using System.IO.Compression;

namespace WouterVanRanst.Utils.Extensions;

public static class FileSystemExtensions
{
    // --- FILE COPY OPERATIONS ---

    /// <summary>
    /// Copy file to the target directory preserving the original filename.
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, DirectoryInfo targetDir) 
        => source.CopyTo(Path.Combine(targetDir.FullName, source.Name));

    /// <summary>
    /// Copy file to the target directory with the given name.
    /// </summary>
    public static FileInfo CopyTo(this FileInfo source, DirectoryInfo targetDir, string targetName) 
        => source.CopyTo(Path.Combine(targetDir.FullName, targetName));

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

    // --- DIRECTORY OPERATIONS ---

    /// <summary>
    /// Copy directory to the destination directory.
    /// </summary>
    public static void CopyTo(this DirectoryInfo dir, DirectoryInfo destinationDir, bool recursive)
    {
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        var dirs = dir.GetDirectories();

        destinationDir.Create();

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir.FullName, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        if (recursive)
        {
            foreach (var subDir in dirs)
            {
                var newDestinationDir = new DirectoryInfo(Path.Combine(destinationDir.FullName, subDir.Name));
                CopyTo(subDir, newDestinationDir, true);
            }
        }
    }

    /// <summary>
    /// Empty the directory.
    /// </summary>
    public static void Clear(this DirectoryInfo dir)
    {
        if (dir is { Exists: true })
        {
            foreach (var file in dir.GetFiles())
            {
                file.Delete();
            }

            foreach (var subDirectory in dir.GetDirectories())
            {
                subDirectory.Delete(true);
            }
        }
    }

    /// <summary>
    /// Create the directory if it doesn't exist.
    /// </summary>
    public static DirectoryInfo CreateIfNotExists(this DirectoryInfo di)
    {
        if (!di.Exists)
            di.Create();

        return di;
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
    /// Compress a file asynchronously.
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
    /// Get files in a directory, handling exceptions.
    /// </summary>
    public static FileInfo[] TryGetFiles(this DirectoryInfo d, string searchPattern)
    {
        try
        {
            return d.GetFiles(searchPattern, SearchOption.AllDirectories);
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }

    /// <summary>
    /// Create the directory for a file path if it doesn't exist.
    /// </summary>
    public static void CreateDirectoryIfNotExists(this string filePath)
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        if (directoryPath is not null)
            Directory.CreateDirectory(directoryPath);
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

    // --- DIRECTORY INFO UTILITIES ---

    /// <summary>
    /// Get a FileInfo for a relative path in a directory.
    /// </summary>
    public static FileInfo GetFileInfo(this DirectoryInfo di, string relativePath) 
        => new(Path.Combine(di.FullName, relativePath));

    /// <summary>
    /// Get a sub-directory of a directory.
    /// </summary>
    public static DirectoryInfo GetSubDirectory(this DirectoryInfo di, string childFolder) 
        => new(Path.Combine(di.FullName, childFolder));

    /// <summary>
    /// Get the relative path from one directory to another.
    /// </summary>
    public static string GetRelativePath(this DirectoryInfo relativeTo, DirectoryInfo path) 
        => Path.GetRelativePath(relativeTo.FullName, path.FullName);

    /// <summary>
    /// Get the full path of a file in a directory.
    /// </summary>
    public static string GetFileFullName(this DirectoryInfo directoryInfo, string fileName)
        => Path.Combine(directoryInfo.FullName, fileName);
}