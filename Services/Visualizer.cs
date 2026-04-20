using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeAssignment.Models;

namespace HomeAssignment.Services
{
    public sealed class Visualizer
    {
        private readonly object _lock = new();
        private int _completedChunks;

        public void StartofTransfer(string fileName, long fileSize, int totalChunks, int chunkSize, int concurrency)
        {
            lock (_lock)
            {
                Console.WriteLine($"  File:         {fileName}");
                Console.WriteLine($"  Size:         {FormatSize(fileSize)} ({fileSize:N0} bytes)");
                Console.WriteLine($"  Chunk size:   {FormatSize(chunkSize)}");
                Console.WriteLine($"  Total chunks: {totalChunks:N0}");
                Console.WriteLine($"  Concurrency:  {concurrency}"); 
            }
        }

        public void ChunkDone(long totalBytesTransferred, long fileSize, int totalChunks)
        {
            int completed = Interlocked.Increment(ref _completedChunks);
            double percent = (double)totalBytesTransferred / fileSize * 100;

            lock (_lock)
            {
                Console.Write($"\r  Progress: {completed,6} / {totalChunks} chunks   " + $"{FormatSize(totalBytesTransferred),10} / {FormatSize(fileSize)}   " + $"[{percent,5:F1}%]");
            }
        }

        public void ReportRetry(int chunkIndex, long position, int attempt, string sourceHash, string destHash)
        {
            lock (_lock)
            {
                Console.WriteLine($"Retrying chunk {chunkIndex} at position {position}. Attempt {attempt}. Source hash: {sourceHash}, Destination hash: {destHash}.");
            }
        }
        public void ReportResult(Rezultat result)
        {
            lock (_lock)

            {
                foreach (var chunk in result.chunks.OrderBy(c => c.Position))
                {
                string status = chunk.Verified ? "Verified" : "Failed";
                string retries = chunk.Attempts > 1 ? $" (Attempts: {chunk.Attempts})" : string.Empty;
                Console.WriteLine(
                    $"{chunk.Index + 1}) position = {chunk.Position}, hash = {chunk.Hash}, status = {status}{retries}");
                }

                Console.WriteLine($"Source SHA256: {result.SourceSHA256}");
                Console.WriteLine($"Destination SHA256: {result.DestinationSHA256}");
                Console.WriteLine($"  Match: {(result.HashMatch ? "YES" : "NO")}");
                Console.WriteLine($"Duration: {result.TimeTaken.TotalSeconds:F2} seconds");

                if(result.TimeTaken.TotalSeconds > 0)
                {
                    long throughput = (long)(result.fileSize / result.TimeTaken.TotalSeconds);
                    Console.WriteLine($"  Throughput:  {FormatSize(throughput)}/s");
                }

                Console.WriteLine(result.Success ? "Transfer successful" : "Transfer with errors");
            }
        }

        private static string FormatSize(long bytes)
    {
        string[] units = new[] { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int i = 0;
        while (size >= 1024 && i < units.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:F2} {units[i]}";
    }
    }
}