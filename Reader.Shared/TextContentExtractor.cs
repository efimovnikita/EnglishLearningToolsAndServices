using System.Reflection;
using System.Text;
using HtmlAgilityPack;

namespace Reader.Shared;

public class TextContentExtractor
{
    private readonly HttpClient _httpClient;
    private readonly string _openAiKey;

    public TextContentExtractor(string openAiKey)
    {
        _openAiKey = openAiKey ?? throw new ArgumentNullException(nameof(openAiKey));
        _httpClient = new HttpClient();
    }

    public async Task<string> ExtractTextContentFromUrlAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }

        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request to {url} failed with status code {response.StatusCode}.");
        }

        string htmlContent = await response.Content.ReadAsStringAsync();
        HtmlDocument htmlDocument = new();
        htmlDocument.LoadHtml(htmlContent);

        HtmlNode? bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
        if (bodyNode == null)
        {
            throw new InvalidOperationException("The web page doesn't have a body tag.");
        }

        if (IsUrlFromTheGuardian(url))
        {
            return ExtractTextFromGuardian(bodyNode);
        }
        
        if (IsUrlFromBbc(url))
        {
            return ExtractTextFromBbc(bodyNode);
        }
        
        if (IsUrlFromWashingtonPost(url))
        {
            return ExtractTextFromWashingtonPost(bodyNode);
        }
         
        if (IsUrlFromIsw(url))
        {
            return ExtractTextFromIsw(bodyNode);
        }
        
        if (IsUrlFromYouTube(url))
        {
            if (String.IsNullOrWhiteSpace(_openAiKey))
            {
                throw new Exception("You must provide OpenAI API key (https://platform.openai.com/account/api-keys). Open settings and add the key.");
            }
            return await ExtractTextFromYouTube(url);
        }

        return ExtractTextFromNode(bodyNode);
    }

    private static bool IsUrlFromTheGuardian(string url)
    {
        Uri uri = new(url);
        return uri.Host.Contains("theguardian.com");
    }
    
    private static bool IsUrlFromBbc(string url)
    {
        Uri uri = new(url);
        return uri.Host.Contains("bbc.com");
    }

    public static bool IsUrlFromYouTube(string url)
    {
        Uri uri = new(url);
        return uri.Host.Contains("youtu.be") || uri.Host.Contains("youtube.com");
    }
    
    private static bool IsUrlFromWashingtonPost(string url)
    {
        Uri uri = new(url);
        return uri.Host.Contains("washingtonpost.com");
    }
    
    private static bool IsUrlFromIsw(string url)
    {
        Uri uri = new(url);
        return uri.Host.Contains("understandingwar.org");
    }
    
    private static string ExtractTextFromIsw(HtmlNode node)
    {
        StringBuilder extractedText = new();

        HtmlNodeCollection? nodes = node.SelectNodes("//*[@id='block-system-main']/div/div/div[3]/div/div");
        if (nodes == null)
        {
            return "";
        }

        if (nodes.Count == 0)
        {
            return "";
        }

        List<HtmlNode> children = nodes[0].ChildNodes.Where(htmlNode => htmlNode.Name != "div").ToList();

        foreach (HtmlNode n in children.SelectMany(htmlNode => htmlNode.ChildNodes.Where(node1 => node1.Name != "span").ToList()))
        {
            extractedText.AppendLine(ExtractTextFromNode(n));
        }
        
        return extractedText.ToString().Trim();
    }
    
    private static string ExtractTextFromWashingtonPost(HtmlNode node)
    {
        StringBuilder extractedText = new();

        HtmlNodeCollection? headlineNodes = node.SelectNodes("//h1[@data-qa='headline']");
        HtmlNodeCollection? bodyNodes = node.SelectNodes("//div[@data-qa='article-body']");

        if (headlineNodes != null)
        {
            foreach (HtmlNode? headlineNode in headlineNodes)
            {
                extractedText.AppendLine(ExtractTextFromNode(headlineNode));
            }
        }

        if (bodyNodes != null)
        {
            foreach (HtmlNode? n in bodyNodes)
            {
                extractedText.AppendLine(ExtractTextFromNode(n));
            }
        }

        return extractedText.ToString().Trim();
    }
    
    private async Task<string> ExtractTextFromYouTube(string url)
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        string audioFilePath = $"{new FileInfo(currentAssembly.Location).Directory!.FullName}/{Guid.NewGuid()}.mp3";

        try
        {
            await YoutubeAudioDownloader.GetAudioFromYoutube(url, audioFilePath);

            AudioTranscriberAndTranslator transcriberAndTranslator = new(_openAiKey);
            string result = await transcriberAndTranslator.TranscribeAudio(audioFilePath);

            DeleteTempAudio(audioFilePath);

            return result;
        }
        finally
        {
            DeleteTempAudio(audioFilePath);
        }
    }

    private static void DeleteTempAudio(string audioFilePath)
    {
        if (File.Exists(audioFilePath))
        {
            File.Delete(audioFilePath);
        }
    }

    private static string ExtractTextFromGuardian(HtmlNode node)
    {
        StringBuilder extractedText = new();

        HtmlNodeCollection? headlineNodes = node.SelectNodes("//div[@data-gu-name='headline']");
        HtmlNodeCollection? standfirstNodes = node.SelectNodes("//div[@data-gu-name='standfirst']");
        HtmlNodeCollection? bodyNodes = node.SelectNodes("//div[@data-gu-name='body']");

        if (headlineNodes != null)
        {
            foreach (HtmlNode? headlineNode in headlineNodes)
            {
                extractedText.AppendLine(ExtractTextFromNode(headlineNode));
            }
        }

        if (standfirstNodes != null)
        {
            foreach (HtmlNode? standfirstNode in standfirstNodes)
            {
                extractedText.AppendLine(ExtractTextFromNode(standfirstNode));
            }
        }
        
        if (bodyNodes != null)
        {
            foreach (HtmlNode? n in bodyNodes)
            {
                extractedText.AppendLine(ExtractTextFromNode(n));
            }
        }

        return extractedText.ToString().Trim();
    }
    
    private static string ExtractTextFromBbc(HtmlNode node)
    {
        StringBuilder extractedText = new();

        HtmlNodeCollection? mainNodes = node.SelectNodes("//*[@id='main-content']");

        if (mainNodes != null)
        {
            foreach (HtmlNode? htmlNode in mainNodes)
            {
                extractedText.AppendLine(ExtractTextFromNode(htmlNode));
            }
        }

        return extractedText.ToString().Trim();
    }

    private static string ExtractTextFromNode(HtmlNode? node)
    {
        if (node == null)
        {
            return string.Empty;
        }

        if (node.NodeType == HtmlNodeType.Text)
        {
            return node.InnerText.Trim();
        }

        // Skip script nodes and style nodes
        if (node.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
            node.Name.Equals("style", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        // Remove inline CSS
        if (node.HasAttributes && node.Attributes.Contains("style"))
        {
            node.Attributes.Remove("style");
        }

        StringBuilder extractedText = new();

        foreach (HtmlNode? childNode in node.ChildNodes)
        {
            extractedText.AppendLine(ExtractTextFromNode(childNode));
        }

        return extractedText.ToString().Trim();
    }
}
