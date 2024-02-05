using CliWrap;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using Spectre.Console;

namespace HearAndTypeConsole.Services;

internal class AskerService : IAskerService
{
    public async Task AskQuestions(Dictionary<FileInfo, string> dictionary, OpenAIAPI api)
    {
        foreach ((FileInfo? audioPath, string? text) in dictionary)
        {
            await TurnRepetitionLoop(audioPath, text, api);
        }
    }

    private async Task TurnRepetitionLoop(FileInfo audioPath, string text, OpenAIAPI api)
    {
        while (true) // Loop to allow repetition
        {
            await PlayAudio(audioPath);
            Console.WriteLine("Type what you heard ('r' to repeat, 'q' to quit):");

            string? textFromUser = Console.ReadLine();
        
            // Check if user wants to repeat
            if (textFromUser?.ToLower() == "r")
            {
                continue; // skips the rest of the current loop iteration and starts a new one
            }
                    
            // Check if user wants to quit
            if (textFromUser?.ToLower() == "q")
            {
                Environment.Exit(0);  // Exit with code 0
            }
                    
            string textFromAudio = text.Replace(",", "").Replace(".", "").ToLower().Trim();

            // Check if user input is empty or null, then continue to the next loop iteration
            if (String.IsNullOrEmpty(textFromUser))
            {
                AnsiConsole.MarkupLine($"[yellow]{textFromAudio}[/]\n");
                break; // Breaks out of the while(true) loop to continue with the next dictionary entry
            }

            // Normalize the text for comparison
            textFromUser = textFromUser.Replace(",", "").Replace(".", "").ToLower().Trim();
                    
            Conversation? chat = api.Chat.CreateConversation();
            chat.Model = Model.GPT4_Turbo;
            chat.RequestParameters.Temperature = 0;

            chat.AppendSystemMessage("""
                                     You are an English teacher who helps people understand spoken language.
                                     Your task is to evaluate how accurately a person understands spoken text.
                                     If the user makes a mistake, you will provide a comment with a brief explanation.
                                     If the user answers correctly, you will say only 'Right!' and nothing more.
                                     You should respond directly to the user, starting with something like, 'Your text contains a couple of errors...,' etc.
                                     Each response must start with the original text from the audio.
                                     Ignore all punctuation errors.
                                     """);

            chat.AppendUserInput($"""
                                  This is the original text from the audio:
                                  '{textFromAudio}'

                                  This is the text from the user:
                                  '{textFromUser}'

                                  Please check the text from user.
                                  """);

            string response = await chat.GetResponseFromChatbotAsync();

            AnsiConsole.MarkupLine(response.Equals("right!", StringComparison.OrdinalIgnoreCase) == false
                ? $"[yellow]{textFromAudio}\n{response}[/]\n"
                : $"[green]{response}[/]\n");

            break; // This will break out of the inner while loop and proceed to the next foreach iteration
        }
    }

    private async Task PlayAudio(FileInfo audioPath)
    {
        // Define the command to execute with CliWrap
        string arguments = $"\"{audioPath.FullName}\" -nodisp -autoexit";
        Command cmd = Cli.Wrap("ffplay")
            .WithArguments(arguments) // Add the file path
            .WithValidation(CommandResultValidation.None); // Skip the validation to handle non-zero exit codes

        // Execute the command and asynchronously wait for completion
        await cmd.ExecuteAsync();
    }
}