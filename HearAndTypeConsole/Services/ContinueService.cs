using System.Text.Json;
using OpenAI_API;
using Spectre.Console;

namespace HearAndTypeConsole.Services;

internal class ContinueService(IAskerService askerService) : IContinueService
{
    public const string CurrentDataJsonName = "current_data.json";

    public async Task<int> ContinueLearning()
    {
        Console.Clear();
            
        string? key = Environment.GetEnvironmentVariable("API_KEY");
        if (String.IsNullOrWhiteSpace(key))
        {
            AnsiConsole.MarkupLine("[red]The 'API_KEY' environment variable doesn't exist.[/]\n");
            return 1;
        }
            
        OpenAIAPI api = new(key);

        Dictionary<FileInfo, string> dictionary = AnsiConsole.Status()
            .Start("Getting the saved data...", _ => DeserializeFromFile(CurrentDataJsonName));
        
        if (dictionary.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Nothing to do.[/]\n");
            return 1;
        }

        await askerService.AskQuestions(dictionary, api);

        return 0;
    }

    private Dictionary<FileInfo, string> DeserializeFromFile(string filename)
    {
        Dictionary<FileInfo, string> dictionary = new();

        try
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            string jsonString = File.ReadAllText(filePath);
            Dictionary<string, string>? simplifiedDictionary =
                JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            if (simplifiedDictionary == null) return dictionary;
            foreach (KeyValuePair<string, string> kvp in simplifiedDictionary)
            {
                dictionary.Add(new FileInfo(kvp.Key), kvp.Value);
            }

            return dictionary;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return dictionary;
        }
    }
}