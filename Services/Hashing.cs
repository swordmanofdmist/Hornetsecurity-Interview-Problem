using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace HomeAssignment.Services
{
    public static class Hashing
    {
        public static string ComputeMD5(ReadOnlySpan<byte> data)
        {
            byte[] hash = MD5.HashData(data);
            return Convert.ToHexString(hash);
        }

        public static async Task<string> ComputeFileSHA256Async(string filePath, CancellationToken ct = default)
        {
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            
            byte[] buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                hasher.AppendData(buffer, 0, bytesRead);
            }
            return Convert.ToHexString(hasher.GetHashAndReset());
        }
    }
}