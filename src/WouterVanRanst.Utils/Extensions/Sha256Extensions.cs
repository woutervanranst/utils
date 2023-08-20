using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WouterVanRanst.Utils.Extensions
{
    public static class SHA256Extensions
    {
        /// <summary>
        /// WARNING BYTE ARRAYS ARE VALUE TYPES -- TWO ARRAYS WITH THE SAME CONTENT ARE NOT EQUAL
        /// </summary>
        public static byte[] CalculateSHA256Hash(this FileInfo fi)
        {
            using var fileStream = fi.OpenRead();
            return CalculateSHA256Hash(fileStream);
        }

        /// <summary>
        /// WARNING BYTE ARRAYS ARE VALUE TYPES -- TWO ARRAYS WITH THE SAME CONTENT ARE NOT EQUAL
        /// </summary>
        public static byte[] CalculateSHA256Hash(this string content)
        {
            using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return CalculateSHA256Hash(contentStream);
        }

        /// <summary>
        /// WARNING BYTE ARRAYS ARE VALUE TYPES -- TWO ARRAYS WITH THE SAME CONTENT ARE NOT EQUAL
        /// </summary>
        public static byte[] CalculateSHA256Hash(this Stream stream)
        {
            return SHA256.HashData(stream);
        }

        /// <summary>
        /// WARNING BYTE ARRAYS ARE VALUE TYPES -- TWO ARRAYS WITH THE SAME CONTENT ARE NOT EQUAL
        /// </summary>
        public static async Task<byte[]> CalculateSHA256HashAsync(this Stream stream)
        {
            return await SHA256.HashDataAsync(stream);
        }

        //public static string CalculateSHA256Hash(this FileInfo fi)
        //{
        //    using var fileStream = fi.OpenRead();
        //    return CalculateSHA256Hash(fileStream);
        //}

        //public static string CalculateSHA256Hash(this string content)
        //{
        //    using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        //    return CalculateSHA256Hash(contentStream);
        //}

        //public static string CalculateSHA256Hash(this Stream stream)
        //{
        //    return SHA256.HashData(stream).BytesToString();
        //}

        //public static async Task<string> CalculateSHA256HashAsync(this Stream stream)
        //{
        //    return (await SHA256.HashDataAsync(stream)).BytesToString();
        //}

        public static string BytesToHexString(this byte[] bytes) => Convert.ToHexString(bytes).ToLower();
        public static byte[] HexStringToBytes(this string hex) => Convert.FromHexString(hex);
        public static byte[] StringToBytes(this string s) => Encoding.UTF8.GetBytes(s); // todo this should not be here


        //public static string BytesToString(this byte[] ba) => Convert.ToHexString(ba).ToLower();
    }
}
