using Markdig;

namespace Client.Helpers;

public static class MarkdownHelper
{
    public static string Parse(string markdown)
    {
        var result = Markdown.ToHtml("This is a text with some *emphasis*");

        return result;

        //if (string.IsNullOrEmpty(markdown))
        //    return "";

        //var parser = MarkdownParserFactory.GetParser(usePragmaLines, forceReload);
        //return parser.Parse(markdown);
    }
}
