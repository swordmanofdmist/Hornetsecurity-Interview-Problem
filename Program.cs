using HomeAssignment.Services;

namespace HomeAssignment
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Large File Transfer Tool ===");
            Console.WriteLine();

            string sourcePath = ReadRequiredPath(
                "Enter source file path (e.g. c:\\source\\my_large_file.bin): ",
                mustExist: true);

            string destinationPath = ReadRequiredPath(
                "Enter destination path (folder or full file path): ",
                mustExist: false);

            long fileSize = new FileInfo(sourcePath).Length;

            int chunkSizeBytes = GetRecommendedChunkSize(fileSize);
            int maxConcurrency = GetRecommendedConcurrency(fileSize);

            Console.WriteLine();
            Console.WriteLine($"Auto settings:");
            Console.WriteLine($"  Chunk size:   {FormatSize(chunkSizeBytes)}");
            Console.WriteLine($"  Concurrency:  {maxConcurrency}");
            Console.WriteLine();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nCancellation requested. Stopping...");
            };

            try
            {
                var transfer = new FileTransfer(sourcePath, destinationPath, chunkSizeBytes, maxConcurrency);
                var result = await transfer.TransferAsync(cts.Token);

                Console.WriteLine();
                Console.WriteLine(result.Success
                    ? "Transfer finished successfully."
                    : "Transfer finished with verification errors.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Transfer cancelled by user.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transfer failed: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string ReadRequiredPath(string prompt, bool mustExist)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = (Console.ReadLine() ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Path cannot be empty.");
                    continue;
                }

                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(input);
                }
                catch
                {
                    Console.WriteLine("Invalid path format.");
                    continue;
                }

                if (mustExist && !File.Exists(fullPath))
                {
                    Console.WriteLine("Source file does not exist.");
                    continue;
                }

                return fullPath;
            }
        }

        private static int GetRecommendedChunkSize(long fileSizeBytes)
        {
            const int MB = 1024 * 1024;

            if (fileSizeBytes < 512L * MB) return 2 * MB;      
            if (fileSizeBytes < 2L * 1024 * MB) return 4 * MB; 
            if (fileSizeBytes < 8L * 1024 * MB) return 8 * MB; 
            return 16 * MB;                                    
        }

        private static int GetRecommendedConcurrency(long fileSizeBytes)
        {
            int cpu = Environment.ProcessorCount;

            
            int baseConcurrency = Math.Clamp(cpu / 2, 2, 8);

            
            const long OneGb = 1024L * 1024 * 1024;
            if (fileSizeBytes < OneGb)
                return Math.Min(baseConcurrency, 4);

            return baseConcurrency;
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size:F2} {units[unit]}";
        }
    }
}