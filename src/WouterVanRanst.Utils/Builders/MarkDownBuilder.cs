using System.Data;
using System.Text;

namespace WouterVanRanst.Utils.Builders;

internal static class MarkDownExtensions
{
    public static string ToMarkdownLink(this string text, MkDocsParser.MkDocsLink l) => ToMarkdownLink(text, l.RootedUrl);
    public static string ToMarkdownLink(this string text, string link) => $"[{text}]({link})";
}

internal static class HtmlExtensions
{
    public static string ToHtmlLink(this string text, string link) 
        => $"<a href=\"{link}\">{text}</a>";
    public static string ToHtmlAdmonition(this string summary, string content)
        => $"<details><summary>{summary}</summary><ul>{content}</ul></details>";
    public static string ToHtmlUnorderedList(this IEnumerable<string> items)
        => $"<ul>{string.Join("", items.Select(i => $"<li>{i}</li>"))}</ul>";
}
internal class MarkDownBuilder
{
    private readonly StringBuilder md = new();

    public MarkDownBuilder AddHeading(string header, int level, bool include = true)
    {
        if (!include)
            return this; // skip this header

        md.AppendLine($"{new string('#', level)} {header}");
        md.AppendLine();
        return this;
    }

    public MarkDownBuilder AddParagraph(string text)
    {
        md.AppendLine(text);
        md.AppendLine();

        return this;
    }

    public MarkDownBuilder AddUnorderedList(params string[] items)
    {
        foreach (var item in items)
        {
            md.AppendLine($"- {item}");
        }

        md.AppendLine();

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="verbatim"></param>
    /// <param name="language">See https://pygments.org/docs/lexers/</param>
    /// <returns></returns>
    public MarkDownBuilder AddVerbatim(string verbatim, string? language = null)
    {
        if (language is null)
            md.AppendLine("```");
        else
            md.AppendLine($"``` {language}");

        md.AppendLine(verbatim);
        md.AppendLine("```");
        md.AppendLine();

        return this;
    }


    public MarkDownBuilder AddImage(string path, string? description = null)
    {
        md.AppendLine($"![{description ?? ""}]({path})");
        md.AppendLine();

        return this;
    }

    public MarkDownBuilder AddAdmonition(string type, string text, string? title = null)
    {
        // see https://squidfunk.github.io/mkdocs-material/reference/admonitions/

        if (title is null)
            md.AppendLine($"!!! {type}");
        else
            md.AppendLine($"!!! {type} \"{title}\"");

        md.AppendLine();

        md.AppendLine($"\t{text}"); //todo multiline

        md.AppendLine();

        return this;
    }

    public class MarkdownTab
    {
        public MarkdownTab(string Header)
        {
            this.Header = Header;
        }

        public string Header { get; init; }
        public MarkDownBuilder Content { get; } = new();
    }

    public MarkDownBuilder AddTabbed(params MarkdownTab[] tabs)
    {
        // https://squidfunk.github.io/mkdocs-material/reference/content-tabs/#usage

        if (tabs.Any(mb => mb.Content == this))
            throw new InvalidOperationException("Tabbed content should be a separte MarkdownBuilder from the parent");

        foreach (var t in tabs)
        {
            md.AppendLine($"=== \"{t.Header}\"");
            //md.AppendLine();

            using var stringReader = new StringReader(t.Content.Build());
            foreach (var line in stringReader.ReadLines())
            {
                md.AppendLine($"    {line}");
            }
        }

        return this;
    }


    // TABLES

    public enum SortType
    {
        TEXT,
        NUMBER
    }

    public enum Alignment
    {
        LEFT, CENTER, RIGHT
    }

    public record TableHeader(string Header, SortType SortType = SortType.TEXT, Alignment Alignment = Alignment.LEFT);

    public MarkDownBuilder AddTable<T>(IEnumerable<T> rows, params string[] headers)
    {
        var propertyNames = rows.First().GetType().GetProperties().Select(property => property.Name).ToArray();

        if (headers.Any())
        {
            if (headers.Length != rows.First().GetType().GetProperties().Length)
                throw new ArgumentException("The provided headers do not correspond in length with the provided columns");
        }
        else
            headers = propertyNames;

        md.AppendLine(Header(headers));

        md.AppendLine(Header(Enumerable.Repeat("---", headers.Length).ToArray()));

        foreach (var row in rows)
        {
            // see https://github.com/numbworks/NW.MarkdownTables/blob/master/src/NW.MarkdownTables/MarkdownTabulizer.cs#L143

            var values = propertyNames.Select(pn => row.GetType().GetProperty(pn).GetValue(row).ToString()).ToArray();
            md.AppendLine(Header(values));
        }


        //var tabulizer = new MarkdownTabulizer();
        //var table = tabulizer.ToMarkdownTable(false, NullHandlingStrategies.ReplaceNullsWithNullMarkdownLines, rows);

        //md.AppendLine(table);

        md.AppendLine();

        static string Header(string[] e) => $"|{string.Join("|", e)}|";

        return this;
    }

    //public MarkDownBuilder AddMarkdownTable(DataTable dataTable, params TableHeader[] headers)
    //{
    //    var propertyNames = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();

    //    if (headers.Any())
    //    {
    //        if (headers.Length != dataTable.Columns.Count)
    //            throw new ArgumentException("The provided headers do not correspond in length with the provided columns");
    //    }
    //    else
    //        headers = propertyNames.Select(header => new TableHeader(header, SortType.TEXT, Alignment.LEFT)).ToArray();

    //    // Add table headers
    //    md.Append("|");
    //    foreach (var header in headers)
    //    {
    //        md.Append($" {header.Header} |");
    //    }
    //    md.AppendLine();

    //    // Add table separator line
    //    md.Append("|");
    //    foreach (var header in headers)
    //    {
    //        md.Append(GetAlignmentString(header.Alignment) + "|");
    //    }
    //    md.AppendLine();

    //    // Add table rows
    //    foreach (DataRow row in dataTable.Rows)
    //    {
    //        md.Append("|");
    //        var values = propertyNames.Select(pn => row[pn].ToString()).ToArray();
    //        for (var i = 0; i < values.Length; i++)
    //            md.Append($" {values[i]} |");
    //        md.AppendLine();
    //    }

    //    md.AppendLine();

    //    return this;

    //    static string GetAlignmentString(Alignment alignment) => alignment switch
    //    {
    //        Alignment.CENTER => " :---: ",
    //        Alignment.RIGHT => " ---: ",
    //        _ => " --- ",
    //    };
    //}


    public MarkDownBuilder AddHtmlTable<T>(IEnumerable<T> rows, params TableHeader[] headers)
    {
        var propertyNames = rows.First().GetType().GetProperties().Select(property => property.Name).ToArray();

        if (headers.Any())
        {
            if (headers.Length != rows.First().GetType().GetProperties().Length)
                throw new ArgumentException("The provided headers do not correspond in length with the provided columns");
        }
        else
            headers = propertyNames.Select(header => new TableHeader(header, SortType.TEXT, Alignment.LEFT)).ToArray();

        md.AppendLine("<table>");
        
        md.AppendLine("<thead>");
        md.AppendLine("<tr>");
        foreach (var header in headers)
        {
            md.Append("<th role=\"columnheader\"");
            md.Append(GetSortString(header.SortType));
            md.Append(GetAlignmentString(header.Alignment));
            md.AppendLine($">{header.Header}</th>");
        }
        md.AppendLine("</tr>");
        md.AppendLine("</thead>");

        md.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            md.AppendLine("<tr>");
            var values = propertyNames.Select(pn => row.GetType().GetProperty(pn).GetValue(row).ToString()).ToArray();
            for (var i = 0; i < values.Length; i++)
                md.AppendLine($"<td{GetAlignmentString(headers[i].Alignment)}>{values[i]}</td>");
            md.AppendLine("</tr>");
        }
        md.AppendLine("</tbody>");
        
        md.AppendLine("</table>");

        md.AppendLine();

        return this;
    }

    public MarkDownBuilder AddHtmlTable(DataTable dataTable, params TableHeader[] headers)
    {
        var propertyNames = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();

        if (headers.Any())
        {
            if (headers.Length != dataTable.Columns.Count)
                throw new ArgumentException("The provided headers do not correspond in length with the provided columns");
        }
        else
            headers = propertyNames.Select(header => new TableHeader(header, SortType.TEXT, Alignment.LEFT)).ToArray();

        md.AppendLine("<table>");

        md.AppendLine("<thead>");
        md.AppendLine("<tr>");
        foreach (var header in headers)
        {
            md.Append("<th role=\"columnheader\"");
            md.Append(GetSortString(header.SortType));
            md.Append(GetAlignmentString(header.Alignment));
            md.AppendLine($">{header.Header}</th>");
        }
        md.AppendLine("</tr>");
        md.AppendLine("</thead>");

        md.AppendLine("<tbody>");
        foreach (DataRow row in dataTable.Rows)
        {
            md.AppendLine("<tr>");
            var values = propertyNames.Select(pn => row[pn].ToString()).ToArray();
            for (var i = 0; i < values.Length; i++)
                md.AppendLine($"<td{GetAlignmentString(headers[i].Alignment)}>{values[i]}</td>");
            md.AppendLine("</tr>");
        }
        md.AppendLine("</tbody>");

        md.AppendLine("</table>");

        md.AppendLine();

        return this;
    }

    private static string GetAlignmentString(Alignment alignment) => alignment switch
    {
        Alignment.CENTER => " style=\"text-align:center\"",
        Alignment.RIGHT => " style=\"text-align:right\"",
        _ => ""
    };

    private static string GetSortString(SortType sortType) => sortType switch
    {
        SortType.NUMBER => " data-sort-method='number'",
        _ => ""
    };


    // BUILD

    public string Build()
    {
        return md.ToString();
    }

    public async Task BuildAsync(FileInfo fi)
    {
        // feat: non breaking page writes when using mkdocs serve --dirtyreload
        fi.Directory.CreateIfNotExists();

        var md = Build();

        // the file does NOT exist  OR
        // the file exists and the contents are different
        if (!fi.Exists || (fi.Exists && (fi.CalculateSHA256Hash() != md.CalculateSHA256Hash())))
            await fi.WriteAllTextAsync(Build());
    }
}