namespace WouterVanRanst.Utils.Extensions;

public static class DirectoryInfoExtensions
{
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
}