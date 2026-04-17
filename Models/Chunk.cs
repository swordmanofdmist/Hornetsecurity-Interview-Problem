using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAssignment.Models
{
    public sealed class Chunk
    {
        public int Index { get; set; }
        public long Position { get; set; }
        public int Size { get; set; }
        public string Hash { get; set; } = string.Empty;
        public bool Verified { get; set; }
        public int Attempts { get; set; }
    }
}