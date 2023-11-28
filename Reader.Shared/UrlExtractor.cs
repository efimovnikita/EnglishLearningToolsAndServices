using System.Text.RegularExpressions;

namespace Reader.Shared;

public class UrlExtractor
{
    private readonly Regex _urlRegex;

    public UrlExtractor()
    {
        string pattern = @"^(https?|ftp|file):\/\/[-A-Za-z0-9+&@#\/%?=~_|!:,.;]*[-A-Za-z0-9+&@#\/%=~_|]";
        this._urlRegex = new Regex(pattern, RegexOptions.IgnoreCase);
    }

    public string? ExtractUrl(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        Match match = this._urlRegex.Match(input);

        return match.Success ? match.Value : null;
    }
}
