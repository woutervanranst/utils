using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouterVanRanst.Utils.Extensions
{
    public static class FileExtensions
    {
        /// <summary>
        /// Return the length of a file in bytes
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static long Length(this string path)
        {
            // Return the length of a file, without using FileInfo and with minimal allocations
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return fileStream.Length;
        }
    }
}
