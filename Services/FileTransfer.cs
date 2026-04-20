using System.Diagnostics;
using HomeAssignment.Models;

namespace HomeAssignment.Services
{
    public sealed class FileTransfer
    {
        private const int MaxRetries = 3;

        private readonly string _sourcePath;
        private readonly string _destinationInput;
        private readonly int _chunkSize;
        private readonly int _maxConcurrency;
        private readonly Visualizer _reporter;

        public FileTransfer(string sourcePath, string destinationPath, int chunkSize, int maxConcurrency)
        {
            _sourcePath = Path.GetFullPath(sourcePath);
            _destinationInput = destinationPath;
            _chunkSize = Math.Max(1024, chunkSize);
            _maxConcurrency = Math.Max(1, maxConcurrency);
            _reporter = new Visualizer();
        }

        public async Task<Rezultat> TransferAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            var sourceInfo = new FileInfo(_sourcePath);
            if (!sourceInfo.Exists)
                throw new FileNotFoundException("Source file not found.", _sourcePath);

            long fileSize = sourceInfo.Length;
            int totalChunks = fileSize == 0 ? 0 : (int)Math.Ceiling((double)fileSize / _chunkSize);

            string destinationPath = ResolveDestinationPath(_destinationInput, sourceInfo.Name);
            string? destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destDir))
                Directory.CreateDirectory(destDir);

            _reporter.StartofTransfer(sourceInfo.Name, fileSize, totalChunks, _chunkSize, _maxConcurrency);

            
            using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.SetLength(fileSize);
            }

            var chunks = new Chunk[totalChunks];
            long bytesTransferred = 0;

            using var semaphore = new SemaphoreSlim(_maxConcurrency);
            var tasks = new List<Task>(totalChunks);

            for (int i = 0; i < totalChunks; i++)
            {
                int chunkIndex = i;
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        long position = (long)chunkIndex * _chunkSize;
                        int size = (int)Math.Min(_chunkSize, fileSize - position);

                        chunks[chunkIndex] = await TransferChunkAsync(
                            chunkIndex, position, size, destinationPath, ct);

                        long transferred = Interlocked.Add(ref bytesTransferred, size);
                        _reporter.ChunkDone(transferred, fileSize, totalChunks);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(tasks);

            string sourceSha256 = Hashing.ComputeFileSHA256(_sourcePath, ct);
            string destinationSha256 = Hashing.ComputeFileSHA256(destinationPath, ct);

            stopwatch.Stop();

            var result = new Rezultat
            {
                sourcePath = _sourcePath,
                destinationPath = destinationPath,
                fileSize = fileSize,
                chunkSize = _chunkSize,
                chunks = chunks.OrderBy(c => c.Position).ToList(),
                SourceSHA256 = sourceSha256,
                DestinationSHA256 = destinationSha256,
                TimeTaken = stopwatch.Elapsed
            };

            _reporter.ReportResult(result);
            return result;
        }

        private async Task<Chunk> TransferChunkAsync(
            int index,
            long position,
            int size,
            string destinationPath,
            CancellationToken ct)
        {
            byte[] buffer = new byte[size];
            byte[] verifyBuffer = new byte[size];

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                
                using (var source = new FileStream(
                    _sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.RandomAccess))
                {
                    source.Seek(position, SeekOrigin.Begin);
                    await ReadExactlyAsync(source, buffer, size, ct);
                }

                string sourceHash = Hashing.ComputeMD5(buffer, size);

                
                using (var destWrite = new FileStream(
                    destinationPath,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.RandomAccess))
                {
                    destWrite.Seek(position, SeekOrigin.Begin);
                    await destWrite.WriteAsync(buffer.AsMemory(0, size), ct);
                    await destWrite.FlushAsync(ct);
                }

                
                using (var destRead = new FileStream(
                    destinationPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.RandomAccess))
                {
                    destRead.Seek(position, SeekOrigin.Begin);
                    await ReadExactlyAsync(destRead, verifyBuffer, size, ct);
                }

                string destHash = Hashing.ComputeMD5(verifyBuffer, size);

                if (string.Equals(sourceHash, destHash, StringComparison.OrdinalIgnoreCase))
                {
                    return new Chunk
                    {
                        Index = index,
                        Position = position,
                        Size = size,
                        Hash = sourceHash,
                        Verified = true,
                        Attempts = attempt
                    };
                }

                _reporter.ReportRetry(index, position, attempt, sourceHash, destHash);
            }

            
            return new Chunk
            {
                Index = index,
                Position = position,
                Size = size,
                Hash = Hashing.ComputeMD5(buffer, size),
                Verified = false,
                Attempts = MaxRetries
            };
        }

        private static async Task ReadExactlyAsync(
            FileStream stream,
            byte[] buffer,
            int size,
            CancellationToken ct)
        {
            int read = 0;
            while (read < size)
            {
                int n = await stream.ReadAsync(buffer.AsMemory(read, size - read), ct);
                if (n == 0)
                    throw new EndOfStreamException("Unexpected end of stream while reading chunk.");
                read += n;
            }
        }

        private static string ResolveDestinationPath(string destinationInput, string sourceFileName)
        {
            
            bool looksLikeFolder =
                destinationInput.EndsWith(Path.DirectorySeparatorChar) ||
                destinationInput.EndsWith(Path.AltDirectorySeparatorChar) ||
                !Path.HasExtension(destinationInput);

            if (looksLikeFolder)
            {
                string folder = Path.GetFullPath(destinationInput);
                return Path.Combine(folder, sourceFileName);
            }            
            return Path.GetFullPath(destinationInput);
        }
    }
}