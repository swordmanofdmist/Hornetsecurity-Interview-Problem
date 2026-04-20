using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace HomeAssignment.Services
{
    public static class Hashing
    {
        public static string ComputeMD5(byte[] data, int length)
    {
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(data, 0, length);
        return Convert.ToHexString(hash);
    }
    public static string ComputeFileSHA256(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
    }
}