namespace WouterVanRanst.Utils.Extensions;

public static class DirectoryInfoExtensions
{
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
    /// Empty the directory recursively
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

    ///// <summary>
    ///// Create the directory for a file path if it doesn't exist.
    ///// </summary>
    //public static void CreateDirectoryIfNotExists(this string filePath)
    //{
    //    var directoryPath = Path.GetDirectoryName(filePath);
    //    if (directoryPath is not null)
    //        Directory.CreateDirectory(directoryPath);
    //}

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