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
                Console.WriteLine($"Starting transfer of '{fileName}' ({fileSize} bytes) in {totalChunks} chunks of {chunkSize} bytes with concurrency level {concurrency}.");
            }
        }
    }
}