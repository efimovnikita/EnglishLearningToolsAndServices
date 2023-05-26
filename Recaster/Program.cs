using System.Reflection;
using CliWrap;
using CliWrap.Buffered;
using OpenAI_API;
using OpenAI_API.Chat;
using Reader.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using TiktokenSharp;
using File = System.IO.File;

namespace Recaster;

internal class Program
{
    private const int Limit = 2500;
    private static List<int>? _allowedUsersList;
    private static string? _openAiApiKey;
    private static TikToken? _tikToken;
    private static string? _pythonExecutablePath;

    public static void Main(string[] args)
    {
        string? telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_API_KEY");
        if (telegramBotApiKey == null)
        {
            Console.WriteLine("Telegram bot api key didn't set");
            return;
        }
        _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (_openAiApiKey == null)
        {
            Console.WriteLine("OpenAI api key didn't set");
            return;
        }

        string? allowedUsers = Environment.GetEnvironmentVariable("ALLOWED_USERS");
        if (allowedUsers == null)
        {
            Console.WriteLine("List of allowed users didn't set");
            return;
        }

        _pythonExecutablePath = Environment.GetEnvironmentVariable("PYTHON_EXECUTABLE_PATH");
        if (_pythonExecutablePath == null)
        {
            Console.WriteLine("Python executable path didn't set");
            return;
        }

        _allowedUsersList = allowedUsers.Split(' ').Select(Int32.Parse).ToList();

        TelegramBotClient client = new(telegramBotApiKey);
        client.StartReceiving(UpdateHandler, ErrorHandler);

        _tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");

        Console.ReadLine();    
    }

    private static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
    {
        Message? message = update.Message;
        if (message == null)
        {
            return;
        }

        int messageId = message.MessageId;
        long chatId = message.Chat.Id;
        
        string? text = message.Text;
        if (text == null)
        {
            return;
        }
        
        try
        {
            if (_allowedUsersList != null && _allowedUsersList.Contains((int) chatId) == false)
            {
                await client.SendTextMessageAsync(chatId,
                    "You don't have privileges to use this bot.",
                    replyToMessageId: messageId,
                    cancellationToken: token);
                return;
            }
        
            // message is URL?
            UrlExtractor urlExtractor = new();
            string? url = urlExtractor.ExtractUrl(text);
            if (url == null)
            {
                await client.SendTextMessageAsync(chatId,
                    "URL didn't found.",
                    replyToMessageId: messageId,
                    cancellationToken: token);
                return;
            }

            TextContentExtractor contentExtractor = new(_openAiApiKey!);
            string content = await contentExtractor.ExtractTextContentFromUrlAsync(url);

            List<int> encode = _tikToken!.Encode(content);
            if (encode.Count > Limit)
            {
                await client.SendTextMessageAsync(chatId,
                    "Content is too long.",
                    replyToMessageId: messageId,
                    cancellationToken: token);
                return;
            }

            const string jsonExample = "{\n" +
                                       "  \"transcript\": [\n" +
                                       "    {\n" +
                                       "      \"speaker\": \"Speaker 1\",\n" +
                                       "      \"speech\": \"Something...\"\n" +
                                       "    },\n" +
                                       "    {\n" +
                                       "      \"speaker\": \"Speaker 2\",\n" +
                                       "      \"speech\": \"Something...\"\n" +
                                       "    }\n" +
                                       "  ]\n" +
                                       "}";
            
            OpenAIAPI api = new(_openAiApiKey);
            Conversation? chat = api.Chat.CreateConversation();
            chat.AppendUserInput("Turn this text into a transcript from short audio podcast. Summarize this text. This text should be appropriate for non native English listener (A2 - B1 level). Summarized version should be narrated by two persons. Use sentences with no more than 210 chars long. Output should be in JSON format strictly.");

            chat.AppendUserInput($"Example of the output format:\n{jsonExample}");
            chat.AppendUserInput($"Source text:\n{content}");
            string response = await chat.GetResponseFromChatbotAsync();
            
            chat = api.Chat.CreateConversation();
            chat.AppendUserInput("I have text in json format. This is an imaginable podcast about daily news. Add greetings (for podcast listeners) in a front of this json dialog and add say goodbye sentences (for podcast listeners) at the end of dialog. Preserve strict json format as output. Return ONLY json and nothing more!!!");
            chat.AppendUserInput($"Example of the output format:\n{jsonExample}");
            chat.AppendUserInput($"Source json:\n{response}");
            response = await chat.GetResponseFromChatbotAsync();

            // Write the JSON string to a file
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string path = Path.Combine(new FileInfo(currentAssembly.Location).Directory!.FullName, "bark", "transcript.json");
            await File.WriteAllTextAsync(path, response, token);

            string audioFilePath = Path.Combine(new FileInfo(currentAssembly.Location).Directory!.FullName, "bark", "bark_generation.wav");
            string arguments = $"-c \"{_pythonExecutablePath} bark.py --json_file transcript.json\"";

            string barkDir = Path.Combine(new FileInfo(currentAssembly.Location).Directory!.FullName, "bark");
            Command cmd = Cli.Wrap("/bin/bash")
                .WithWorkingDirectory(barkDir)
                .WithEnvironmentVariables(builder =>
                {
                    builder.Set("SUNO_USE_SMALL_MODELS", "TRUE");
                    builder.Build();
                })
                .WithArguments(arguments);
            await cmd.ExecuteBufferedAsync();

            if (File.Exists(audioFilePath) == false)
            {
                await client.SendTextMessageAsync(chatId,
                    "Something went wrong with audio",
                    replyToMessageId: messageId,
                    cancellationToken: token);
            }
            
            await client.SendAudioAsync(chatId,
            InputFile.FromStream(File.OpenRead(audioFilePath)),
                replyToMessageId: messageId,
                cancellationToken: token);
        }
        catch (Exception)
        {
            await client.SendTextMessageAsync(chatId,
                "Error. Something went wrong",
                replyToMessageId: messageId,
                cancellationToken: token);
        }
    }

    private static Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Error. Something went wrong:\n{exception}");
        return Task.CompletedTask;
    }
}