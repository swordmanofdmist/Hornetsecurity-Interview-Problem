using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAssignment.Models
{
    public sealed class Rezultat
    {
        public string sourcePath { get; set; } = string.Empty;
        public string destinationPath { get; set; } = string.Empty;
        public long fileSize { get; set; }
        public int chunkSize { get; set; }
        public List<Chunk> chunks { get; set; } = new List<Chunk>();
        public string SourceSHA256 { get; set; } = string.Empty;
        public string DestinationSHA256 { get; set; } = string.Empty;
        public TimeSpan TimeTaken { get; set; }
        public bool HashMatch => string.Equals(SourceSHA256, DestinationSHA256, StringComparison.OrdinalIgnoreCase);
        public bool AllChunksVerified => chunks.TrueForAll(c => c.Verified);
        public bool Success => HashMatch && AllChunksVerified;
    }
}