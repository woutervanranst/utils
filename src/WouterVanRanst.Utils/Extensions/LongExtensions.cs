namespace WouterVanRanst.Utils.Extensions;

public static class LongExtensions
{
    // https://stackoverflow.com/a/11124118/1582323

    // TODO Consider the use of https://github.com/omar/ByteSize and/or https://github.com/Humanizr/Humanizer#bytesize

    public static string GetBytesReadable(this long i, int precision = 3)
    {
        // Determine the suffix and readable value
        string suffix;
        double readable;

        switch (Math.Abs(i))
        {
            case >= 0x1000000000000000: // Exabyte
                suffix = "EiB";
                readable = i >> 50;
                break;
            case >= 0x4000000000000:  // Petabyte
                suffix = "PiB";
                readable = i >> 40;
                break;
            case >= 0x10000000000: // Terabyte
                suffix = "TiB";
                readable = i >> 30;
                break;
            case >= 0x40000000: // Gigabyte
                suffix = "GiB";
                readable = i >> 20;
                break;
            case >= 0x100000: // Megabyte
                suffix = "MiB";
                readable = i >> 10;
                break;
            case >= 0x400: // Kilobyte
                suffix = "KiB";
                readable = i;
                break;
            default:
                return i.ToString("0 B"); // Byte
        }

        // Divide by 1024 to get fractional value
        readable /= 1024;

        // Return formatted number with suffix
        var format = $"{{0:N{precision}}}";
        return $"{readable.ToString($"N{precision}")} {suffix}";
    }


    //public enum Size
    //{
    //    KB
    //}
    //public static string GetBytesReadable(this long i, Size size)
    //{
    //    if (size == Size.KB)
    //        return $"{i / 1024:N0} {size:g}";

    //    throw new NotImplementedException();
    //}



    /* From chatGPT aug23:
     *
     * if (value is long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int      order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes = bytes / 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }
        return null;
     */
}