using System.Text.Json;
using OpenAI_API;
using OpenAI_API.Audio;
using Reader.Shared;
using Spectre.Console;

namespace HearAndTypeConsole.Services;

internal class LearnService(
    IAudioDownloaderService audioDownloader,
    ISplitterService splitterService,
    IAskerService askerService)
    : ILearnService
{
    public async Task<int> GetAudioSplitAndAskUser(string url)
    {
        Console.Clear();
            
        string? key = Environment.GetEnvironmentVariable("API_KEY");
        if (String.IsNullOrWhiteSpace(key))
        {
            AnsiConsole.MarkupLine("[red]The 'API_KEY' environment variable doesn't exist.[/]\n");
            return 1;
        }

        string? endpointUrl = Environment.GetEnvironmentVariable("ENDPOINT");
        if (String.IsNullOrWhiteSpace(endpointUrl))
        {
            AnsiConsole.MarkupLine("[red]The 'ENDPOINT' environment variable doesn't exist.[/]\n");
            return 1;
        }
            
        if (TextContentExtractor.IsUrlFromYouTube(url) == false)
        {
            AnsiConsole.MarkupLine("[red]Only Youtube link are allowed...[/]\n");
            return 1;
        }

        (bool downloadStatus, string? filePath) = await AnsiConsole.Status()
            .StartAsync("Extracting the audio from the link...", 
                async _ => await audioDownloader.DownloadAudio(url, endpointUrl));

        if (downloadStatus == false)
        {
            AnsiConsole.MarkupLine("[red]Failed to download the audio file.[/]\n");
            return 1;
        }

        if (File.Exists(filePath) == false)
        {
            AnsiConsole.MarkupLine("[red]The audio file doesn't exist.[/]\n");
            return 1;
        }
            
        OpenAIAPI api = new(key);
            
        AudioResultVerbose? result = await AnsiConsole.Status().StartAsync("Getting the transcription...",
            async _ => await GetDetailedTranscription(api, filePath));
            
        if (result == null)
        {
            AnsiConsole.MarkupLine("[red]The transcription result is empty.[/]\n");
            return 1;
        }
        
        if (result.language.Equals("english", StringComparison.OrdinalIgnoreCase) == false)
        {
            AnsiConsole.MarkupLine("[red]The app is working only with the English language.[/]\n");
            return 1;
        }
            
        List<AudioResultVerbose.Segment> segments = result.segments;
            
        if (segments.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]The number of text segments is zero.[/]\n");
            return 1;
        }
            
        Dictionary<FileInfo,string> dictionary = await AnsiConsole.Status()
            .StartAsync("Splitting the audio file...", 
                async _ => await splitterService.SplitAudio(segments, filePath));

        AnsiConsole.Status().Start("Saving the result...", _ =>
        {
            SerializeToFile(dictionary, ContinueService.CurrentDataJsonName);
        });

        await askerService.AskQuestions(dictionary, api);

        return 0;
    }

    private void SerializeToFile(Dictionary<FileInfo, string> dictionary, string filename)
    {
        try
        {
            // Since FileInfo is not easily serializable, we convert it to a serializable form
            Dictionary<string, string> simplifiedDictionary = new();
            foreach (KeyValuePair<FileInfo, string> kvp in dictionary)
            {
                simplifiedDictionary.Add(kvp.Key.FullName, kvp.Value);
            }

            string jsonString =
                JsonSerializer.Serialize(simplifiedDictionary, new JsonSerializerOptions {WriteIndented = true});
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            File.WriteAllText(filePath, jsonString);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }

    private static async Task<AudioResultVerbose?> GetDetailedTranscription(OpenAIAPI api, string? filePath)
    {
        try
        {
            return await api.Transcriptions.GetWithDetailsAsync(filePath);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return null;
        }
    }
}