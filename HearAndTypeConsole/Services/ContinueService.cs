using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenAI_API;
using Spectre.Console;

namespace HearAndTypeConsole.Services;

internal class ContinueService(IAskerService askerService, IConfiguration configuration) : IContinueService
{
    public async Task<int> ContinueLearning()
    {
        Console.Clear();
        
        string? key = String.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.ApiKeyVarName)) == false
            ? Environment.GetEnvironmentVariable(Constants.ApiKeyVarName)
            : configuration.GetValue<string>(Constants.ApiKeyVarName);
        
        if (String.IsNullOrWhiteSpace(key))
        {
            AnsiConsole.MarkupLine("[red]The 'API_KEY' environment variable doesn't exist.[/]\n");
            return 1;
        }
            
        OpenAIAPI api = new(key);
        
        string dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            Constants.CurrentDataFileName);

        if (File.Exists(dataFilePath) == false)
        {
            AnsiConsole.MarkupLine("[red]The data file didn't found[/]\n");
            return 1;
        }

        Dictionary<FileInfo, string> dictionary = AnsiConsole.Status()
            .Start("Getting the saved data...",
                _ => DeserializeFromFile(dataFilePath));
        
        if (dictionary.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Nothing to do.[/]\n");
            return 1;
        }

        bool validationResult = AnsiConsole.Status()
            .Start("Validating the saved data...",
                _ => ValidateDictionary(dictionary));
        
        if (validationResult == false)
        {
            AnsiConsole.MarkupLine("[red]The saved data is invalid.[/]");
            return 1;
        }

        await askerService.AskQuestions(dictionary, api);

        return 0;
    }

    private static bool ValidateDictionary(Dictionary<FileInfo, string> dictionary)
    {
        foreach ((FileInfo fileInfo, string value) in dictionary)
        {
            if (fileInfo.Exists == false)
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(value))
            {
                return false;
            }
        }

        return true;
    }

    private Dictionary<FileInfo, string> DeserializeFromFile(string filePath)
    {
        Dictionary<FileInfo, string> dictionary = new();

        try
        {
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