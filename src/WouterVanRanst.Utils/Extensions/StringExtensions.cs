﻿using System.Text.RegularExpressions;

namespace WouterVanRanst.Utils.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Trim the given value from the end of the string
    /// </summary>
    /// <param name="inputText"></param>
    /// <param name="value"></param>
    /// <param name="comparisonType"></param>
    /// <returns></returns>
    public static string TrimEnd(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
    {
        // https://stackoverflow.com/questions/4101539/c-sharp-removing-substring-from-end-of-string

        if (!string.IsNullOrEmpty(value))
        {
            while (!string.IsNullOrEmpty(inputText) && inputText.EndsWith(value, comparisonType))
            {
                inputText = inputText.Substring(0, inputText.Length - value.Length);
            }
        }

        return inputText;
    }


    public static string Left(this string str, int length)
    {
        // https://stackoverflow.com/a/3566842/1582323

        if (string.IsNullOrEmpty(str))
            return str;

        return str[..Math.Min(str.Length, length)];
    }

    public static string RemovePrefix(this string s, string prefix)
    {
        if (s.StartsWith(prefix))
            return s[prefix.Length..];

        return s;
    }

    /// <summary>
    /// Joins an array of strings
    /// </summary>
    /// <param name="strings">Array of strings</param>
    /// <param name="separator">Optionally the separator. If not specified: Environment.NewLine</param>
    /// <returns></returns>
    public static string Join(this IEnumerable<string> strings, string separator = null)
    {
        separator ??= Environment.NewLine;

        return string.Join(separator, strings);
    }

    public static string ToKebabCase(this string value)
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
}