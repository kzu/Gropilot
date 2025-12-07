using System.Text.RegularExpressions;

public static partial class MarkdownConverter
{
    public static string ConvertLinks(string text)
    {
        var counter = 1;
        List<string> references = [];

        var result = LinkExpr().Replace(text, m =>
        {
            var linkText = m.Groups[1].Value;
            var url = m.Groups[2].Value;
            var replacement = $"[{counter}]";
            references.Add($"[{counter}]: {url}");
            counter++;
            return replacement;
        });

        result = CleanExpr().Replace(result, m => $"[{m.Groups[1].Value}]");
        result = CompactExpr().Replace(result, m => $" [{m.Groups[1].Value}]\n");

        if (references.Count > 0)
            result += "\n\n" + string.Join("\n", references);

        return result;
    }

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex LinkExpr();

    [GeneratedRegex(@"\(\s*\[(\d+)\]\s*\)", RegexOptions.Compiled)]
    private static partial Regex CleanExpr();

    [GeneratedRegex(@"\r?\n\[(\d+)\]\r?\n", RegexOptions.Compiled)]
    private static partial Regex CompactExpr();
}