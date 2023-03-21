using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouterVanRanst.Utils.Extensions;

public static class TextReaderExtensions
{
    public static IEnumerable<string> ReadLines(this TextReader reader)
    {
        // https://stackoverflow.com/a/28365736/1582323
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}