using System.Text.RegularExpressions;

namespace WouterVanRanst.Utils.Extensions;

public static class StringExtensions
{
    public static string Left(this string str, int length)
    {
        // https://stackoverflow.com/a/3566842/1582323

        if (string.IsNullOrEmpty(str))
            return str;

        return str[..Math.Min(str.Length, length)];
    }

    public static string RemovePrefix(this string s, string prefix, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        if (s.StartsWith(prefix, comparisonType))
            return s[prefix.Length..];

        return s;
    }

    /// <summary>
    /// Trim the given value from the end of the string
    /// </summary>
    /// <param name="inputText"></param>
    /// <param name="value"></param>
    /// <param name="comparisonType"></param>
    /// <returns></returns>
    public static string RemoveSuffix(this string inputText, string value, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        // https://stackoverflow.com/questions/4101539/c-sharp-removing-substring-from-end-of-string

        if (!string.IsNullOrEmpty(value))
        {
            while (!string.IsNullOrEmpty(inputText) && inputText.EndsWith(value, comparisonType))
            {
                inputText = inputText[..^value.Length];
            }
        }

        return inputText;
    }

    /// <summary>
    /// Joins an array of strings
    /// </summary>
    /// <param name="strings">Array of strings</param>
    /// <param name="separator">Optionally the separator. If not specified: Environment.NewLine</param>
    /// <returns></returns>
    public static string Join(this IEnumerable<string> strings, string? separator = null)
    {
        separator ??= Environment.NewLine;

        return string.Join(separator, strings);
    }

    /// <summary>
    /// Joins an array of strings
    /// </summary>
    /// <param name="strings">Array of strings</param>
    /// <param name="separator">The separator</param>
    /// <returns></returns>
    public static string Join(this IEnumerable<string> strings, char separator)
    {
        return string.Join(separator, strings);
    }

    public static string ToKebabCase(this string? value)
    {
        // https://gist.github.com/wsloth/5e9f0e83bdd0c3c9341da7d83ffb8dbb

        // Replace all non-alphanumeric characters with a dash
        value = Regex.Replace(value, @"[^0-9a-zA-Z]", "-");

        // Replace all subsequent dashes with a single dash
        value = Regex.Replace(value, @"[-]{2,}", "-");

        // Remove any trailing dashes
        value = Regex.Replace(value, @"-+$", string.Empty);

        // Remove any dashes in position zero
        if (value.StartsWith("-")) value = value[1..];

        // Lowercase and return
        return value.ToLower();
    }

    public static (string[], string) RemoveLongestCommonPrefix(this string[] values)
    {
        var length = GetLongestCommonPrefixLength(values).StartIndex;

        var prefix = values.First()[..length];
        var array = values.Select(v => v[length..]).ToArray();

        return (array, prefix);
    }

    /// <summary>
    /// Returns the zero-based length of the longest common prefix
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static (int StartIndex, string Prefix) GetLongestCommonPrefixLength(this string[] values)
    {
        if (values.Length == 1)
            return (0, "");

        var i = 0;
        while (values.Select(v => v[..i]).Distinct().Count() == 1)
            i++;

        return (i - 1, values.First()[0..(i - 1)]);
    }
}