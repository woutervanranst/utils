using System.IO.Compression;

namespace WouterVanRanst.Utils.Extensions;

public static class FileSystemExtensions
{
    /// <summary>
    /// Copy to targetDir preserving the original filename
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, DirectoryInfo targetDir)
    {
        return source.CopyTo(Path.Combine(targetDir.FullName, source.Name));
    }

    /// <summary>
    /// Copy to targetDir with the given name
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, DirectoryInfo targetDir, string targetName)
    {
        return source.CopyTo(Path.Combine(targetDir.FullName, targetName));
    }

    /// <summary>
    /// Copy to the targetDir in the same relative path as vs the sourceRoot
    /// eg. CopyTo('dir1\documents\file.txt', 'dir1', 'dir2') results in 'dir2\documents\file.txt')
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, DirectoryInfo sourceRoot, DirectoryInfo targetDir, bool overwrite = false)
    {
        if (!source.IsInDirectoryTree(sourceRoot))
            throw new ArgumentException($"{source.FullName} is not in the source directory {sourceRoot.FullName}");

        var relativeName = Path.GetRelativePath(sourceRoot.FullName, source.FullName);
        var target = new FileInfo(Path.Combine(targetDir.FullName, relativeName));
        target.Directory.Create();

        if (!target.Exists || target.Exists && overwrite)
        {
            source.CopyTo(target.FullName, overwrite);
            target.LastWriteTimeUtc = source.LastWriteTimeUtc; //CopyTo does not do this somehow
        }

        return target;
    }

    /// <summary>
    /// Checks whether fi is in the directory tree under parent, recursively
    /// eg. c:\test\dir1\file1.txt, c:\test\dir1 is true
    /// eg. c:\test\dir1\file1.txt, c:\test\abcd is false
    /// </summary>
    /// <param name="fi"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static bool IsInDirectoryTree(this FileInfo fi, DirectoryInfo parent)
    {
        var dir = fi.Directory;
        while (true)
        {
            if (string.Compare(dir.FullName, parent.FullName, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            dir = dir.Parent;

            if (dir is null)
                return false;
        }
    }

    public static void Rename(this FileInfo source, string targetName)
    {
        source.MoveTo(Path.Combine(source.DirectoryName, targetName));
    }

    public static string GetRelativeName(this FileInfo fi, DirectoryInfo root) => Path.GetRelativePath(root.FullName, fi.FullName);

    public static async Task CompressAsync(this FileInfo fi, bool deleteOriginal)
    {
        await using (var ss = fi.OpenRead())
        {
            await using var ts = File.OpenWrite($"{fi.FullName}.gzip");
            await using var gzs = new GZipStream(ts, CompressionLevel.Optimal);
            await ss.CopyToAsync(gzs);
        }

        if (deleteOriginal)
            fi.Delete();
    }

    /// <summary>
    /// Empty the directory
    /// </summary>
    /// <param name="dir"></param>
    public static void Clear(this DirectoryInfo dir)
    {
        if (dir.Exists) dir.Delete(true);
        dir.Create();
    }

    public static FileInfo[] TryGetFiles(this DirectoryInfo d, string searchPattern)
    {
        try
        {
            return d.GetFiles(searchPattern, SearchOption.AllDirectories);
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<FileInfo>();
        }
        catch (DirectoryNotFoundException)
        {
            return Array.Empty<FileInfo>();
        }
    }

    public static void CreateIfNotExists(this DirectoryInfo di)
    {
        if (!di.Exists)
            di.Create();
    }

    public static string ReadAllText(this FileInfo fi)
    {
        return File.ReadAllText(fi.FullName);
    }

    public static async Task<string> ReadAllTextAsync(this FileInfo fi)
    {
        return await File.ReadAllTextAsync(fi.FullName);
    }

    public static void WriteAllText(this FileInfo fi, string contents)
    {
        File.WriteAllText(fi.FullName, contents);
    }

    public static async Task WriteAllTextAsync(this FileInfo fi, string contents)
    {
        await File.WriteAllTextAsync(fi.FullName, contents);
    }

    public static FileInfo GetFileInfo(this DirectoryInfo di, string relativePath) => new FileInfo(Path.Combine(di.FullName, relativePath));

    public static void CreateDirectoryIfNotExists(this FileInfo fi) => fi.Directory.CreateIfNotExists();

    public static void CopyTo(this DirectoryInfo dir, DirectoryInfo destinationDir, bool recursive)
    {
        // Source: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        var dirs = dir.GetDirectories();

        // Create the destination directory
        destinationDir.Create();

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir.FullName, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (var subDir in dirs)
            {
                var newDestinationDir = new DirectoryInfo(Path.Combine(destinationDir.FullName, subDir.Name));
                CopyTo(subDir, newDestinationDir, true);
            }
        }
    }
}