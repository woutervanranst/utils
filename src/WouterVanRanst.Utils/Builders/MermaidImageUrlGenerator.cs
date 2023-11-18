using System.IO.Compression;
using System.Text;

namespace WouterVanRanst.Utils.Builders;

public static class MermaidImageUrlGenerator
{
    public static string GenerateImageUrl(string mermaidSource, string format = "svg")
    {
        // See https://github.com/yuzutech/kroki/issues/1584

        var compressedBytes = Deflate(Encoding.UTF8.GetBytes(mermaidSource));
        var encodedOutput = Convert.ToBase64String(compressedBytes).Replace('+', '-').Replace('/', '_');
        return $"https://kroki.io/mermaid/svg/{encodedOutput}";
    }

    public static async Task DownloadImageAsync(string mermaidSource, string targetFile, string format = "svg")
    {
        var url = GenerateImageUrl(mermaidSource, format);
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(targetFile);
        await stream.CopyToAsync(fileStream);
    }

    private static byte[] Deflate(byte[] data, CompressionLevel? level = null)
    {
        byte[] newData;
        using (var memStream = new MemoryStream())
        {
            // write header:
            memStream.WriteByte(0x78);
            switch (level)
            {
                case CompressionLevel.NoCompression:
                case CompressionLevel.Fastest:
                    memStream.WriteByte(0x01);
                    break;
                case CompressionLevel.Optimal:
                    memStream.WriteByte(0xDA);
                    break;
                default:
                    memStream.WriteByte(0x9C);
                    break;
            }

            // write compressed data (with Deflate headers):
            using (var dflStream = level.HasValue
                       ? new DeflateStream(memStream, level.Value)
                       : new DeflateStream(memStream, CompressionMode.Compress
                       )) dflStream.Write(data, 0, data.Length);
            //
            newData = memStream.ToArray();
        }

        // compute Adler-32:
        uint a1 = 1, a2 = 0;
        foreach (byte b in data)
        {
            a1 = (a1 + b) % 65521;
            a2 = (a2 + a1) % 65521;
        }

        // append the checksum-trailer:
        var adlerPos = newData.Length;
        Array.Resize(ref newData, adlerPos + 4);
        newData[adlerPos] = (byte)(a2 >> 8);
        newData[adlerPos + 1] = (byte)a2;
        newData[adlerPos + 2] = (byte)(a1 >> 8);
        newData[adlerPos + 3] = (byte)a1;
        return newData;
    }
}