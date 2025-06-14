using System.IO.Compression;

namespace WouterVanRanst.Utils.Extensions;

public static class FileInfoExtensions
{
    // --- FILE COPY OPERATIONS ---

    /// <summary>
    /// Copy file to the target directory preserving the original filename.
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, DirectoryInfo targetDir)
    {
        targetDir.Create();
        return source.CopyTo(Path.Combine(targetDir.FullName, source.Name));
    }

    /// <summary>
    /// Copy file to the target directory with the given name.
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, DirectoryInfo targetDir, string targetName)
    {
        var destFileName = Path.Combine(targetDir.FullName, targetName);
        Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
        return source.CopyTo(destFileName);
    }

    /// <summary>
    /// Copy file to the target directory with the given name.
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, string targetDir, string targetName)
    {
        var destFileName = Path.Combine(targetDir, targetName);
        Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
        return source.CopyTo(destFileName);
    }

    /// <summary>
    /// Copy file to the target directory in the same relative path as vs the source root.
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, DirectoryInfo sourceRoot, DirectoryInfo targetDir, bool overwrite = false)
    {
        if (!source.IsInDirectoryTree(sourceRoot))
            throw new ArgumentException($"{source.FullName} is not in the source directory {sourceRoot.FullName}");

        var relativeName = Path.GetRelativePath(sourceRoot.FullName, source.FullName);
        var target       = new FileInfo(Path.Combine(targetDir.FullName, relativeName));
        target.Directory.Create();

        if (!target.Exists || overwrite)
        {
            source.CopyTo(target.FullName, overwrite);
            target.LastWriteTimeUtc = source.LastWriteTimeUtc;
        }

        return target;
    }

    // --- FILE/DIRECTORY UTILITIES ---

    /// <summary>
    /// Check if a file is in the directory tree under the parent directory.
    /// </summary>
    public static bool IsInDirectoryTree(this FileInfo fi, DirectoryInfo parent)
    {
        var dir = fi.Directory;
        while (dir is not null)
        {
            if (string.Compare(dir.FullName, parent.FullName, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            dir = dir.Parent;
        }

        return false;
    }

    /// <summary>
    /// Rename a file.
    /// </summary>
    public static void Rename(this FileInfo source, string targetName)
        => source.MoveTo(Path.Combine(source.DirectoryName, targetName));

    /// <summary>
    /// Get the relative path of a file from a root directory.
    /// </summary>
    public static string GetRelativeName(this FileInfo fi, DirectoryInfo root) 
        => Path.GetRelativePath(root.FullName, fi.FullName);

    /// <summary>
    /// Compress a file
    /// </summary>
    public static async Task CompressAsync(this FileInfo fi, bool deleteOriginal)
    {
        await using var ss  = fi.OpenRead();
        await using var ts  = File.OpenWrite($"{fi.FullName}.gzip");
        await using var gzs = new GZipStream(ts, CompressionLevel.Optimal);
        await ss.CopyToAsync(gzs);

        if (deleteOriginal)
            fi.Delete();
    }

    /// <summary>
    /// Create the directory for a file if it doesn't exist.
    /// </summary>
    public static FileInfo CreateDirectoryIfNotExists(this FileInfo fi)
    {
        fi.Directory.CreateIfNotExists();
        return fi;
    }

    // --- FILE READ/WRITE OPERATIONS ---

    /// <summary>
    /// Read all text from a file.
    /// </summary>
    public static string ReadAllText(this FileInfo fi)
        => File.ReadAllText(fi.FullName);

    /// <summary>
    /// Read all text from a file asynchronously.
    /// </summary>
    public static async Task<string> ReadAllTextAsync(this FileInfo fi)
        => await File.ReadAllTextAsync(fi.FullName);

    /// <summary>
    /// Read all lines from a file asynchronously.
    /// </summary>
    public static async Task<string[]> ReadAllLinesAsync(this FileInfo fi)
        => await File.ReadAllLinesAsync(fi.FullName);

    /// <summary>
    /// Write all text to a file.
    /// </summary>
    public static void WriteAllText(this FileInfo fi, string contents)
        => File.WriteAllText(fi.FullName, contents);

    /// <summary>
    /// Write all text to a file asynchronously.
    /// </summary>
    public static async Task WriteAllTextAsync(this FileInfo fi, string contents)
        => await File.WriteAllTextAsync(fi.FullName, contents);
}