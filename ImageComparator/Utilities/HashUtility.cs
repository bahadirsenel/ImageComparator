using System;
using System.IO;
using System.Security.Cryptography;

namespace ImageComparator.Utilities
{
    /// <summary>
    /// Utility class for hash calculations.
    /// </summary>
    public static class HashUtility
    {
        /// <summary>
        /// Calculates the Hamming distance between two hash arrays.
        /// </summary>
        /// <param name="hash1">First hash array.</param>
        /// <param name="hash2">Second hash array.</param>
        /// <param name="index1">Index in first hash array.</param>
        /// <param name="index2">Index in second hash array.</param>
        /// <param name="length">Length of hash to compare.</param>
        /// <returns>Hamming distance (number of differing bits).</returns>
        public static int CalculateHammingDistance(int[,] hash1, int[,] hash2, int index1, int index2, int length)
        {
            int distance = 0;

            for (int i = 0; i < length; i++)
            {
                if (hash1[index1, i] != hash2[index2, i])
                {
                    distance++;
                }
            }

            return distance;
        }

        /// <summary>
        /// Calculates SHA256 hash of a file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <returns>SHA256 hash as a hex string.</returns>
        public static string CalculateSHA256(string filePath)
        {
            using (var sha = new SHA256Managed())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        /// <summary>
        /// Compares two SHA256 hashes for equality.
        /// </summary>
        /// <param name="hash1">First hash.</param>
        /// <param name="hash2">Second hash.</param>
        /// <returns>True if hashes are equal, false otherwise.</returns>
        public static bool CompareSHA256(string hash1, string hash2)
        {
            return string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
