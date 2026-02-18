using System.IO;
using System.Text.RegularExpressions;

namespace ePub2.SemanticServices
{
    public static class HtmlTextExtractor
    {
        public static string ReadHtmlFile(string filePath)
        {
            var html = File.ReadAllText(filePath);

            // Remove script and style
            html = Regex.Replace(html, "<script.*?>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<style.*?>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Remove all HTML tags
            var textOnly = Regex.Replace(html, "<.*?>", " ");

            // Collapse spaces
            textOnly = Regex.Replace(textOnly, @"\s+", " ").Trim();

            return textOnly.ToLower();
        }
    }
}
