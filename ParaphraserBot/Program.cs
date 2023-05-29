using CliWrap;
using CliWrap.Buffered;
using NAudio.Wave;
using OpenAI_API;
using OpenAI_API.Chat;
using Reader.Models;
using Reader.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using TiktokenSharp;
using File = System.IO.File;

namespace ParaphraserBot;

internal class Program
{
    private const int Limit = 500;
    private const string SystemMessage = "You are professional English teacher. Your speciality is to paraphrase given text into easy and understandable variation for intermediate English learner.";
    private const string OutputFileName = "merged.wav";
    private static List<int>? _allowedUsersList;
    private static string? _openAiApiKey;
    private static TikToken? _tikToken;
    private static readonly string AssemblyLocation = Directory.GetCurrentDirectory();
    private static readonly string PiperFolderPath = Path.Combine(AssemblyLocation, "piper");

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

            List<Sentence> sentences = TokenizerAndSentenceExtractor.GetSentencesList(content);
            Fraction mainFraction = new(sentences);

            List<Fraction> fractions = mainFraction.SplitIntoSubfractions(Limit);

            List<string> responses = new();
            foreach (Fraction fraction in fractions)
            {
                string paraphrasedResponse = await GetParaphrasedResponse(fraction);
                if (String.IsNullOrEmpty(paraphrasedResponse))
                {
                    continue;
                }
                
                responses.Add(paraphrasedResponse);
                
                await client.SendTextMessageAsync(chatId,
                    paraphrasedResponse,
                    replyToMessageId: messageId,
                    cancellationToken: token);
            }

            List<Sentence> responseSentences = responses.Select(s => s.Replace("'", "")
                    .Replace("\"", ""))
                .SelectMany(TokenizerAndSentenceExtractor.GetSentencesList)
                .ToList();

            string tempAudioFolder = Path.Combine(PiperFolderPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempAudioFolder);

            for (int i = 0; i < responseSentences.Count; i++)
            {
                try
                {
                    string audioPath = Path.Combine(tempAudioFolder, $"{i}.wav");
                    Command cmd = Cli.Wrap("/bin/bash")
                        .WithWorkingDirectory(PiperFolderPath)
                        .WithArguments($"-c \"echo '{responseSentences[i]}' | ./piper -m 'en-us-ryan-high.onnx' --output_file '{audioPath}'\"");
                    await cmd.ExecuteBufferedAsync();
                }
                catch
                {
                    // ignored
                }
            }
            
            WavFinder finder = new(tempAudioFolder);
            
            // Sorting the list by file name (as integer)
            string[] wavFiles = finder.FindWavFiles().OrderBy(path =>
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                return int.Parse(fileName);
            }).ToArray();

            string outputFile = Path.Combine(tempAudioFolder, OutputFileName);
            Combine(outputFile, wavFiles);
            
            await client.SendAudioAsync(chatId,
                InputFile.FromStream(File.OpenRead(outputFile)),
                replyToMessageId: messageId,
                cancellationToken: token);
            
            Directory.Delete(tempAudioFolder, true);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error. Something went wrong:\n{e}");
        }
    }

    private static async Task<string> GetParaphrasedResponse(Fraction fraction)
    {
        OpenAIAPI api = new(_openAiApiKey);
        Conversation? chat = api.Chat.CreateConversation();
        chat.AppendSystemMessage(SystemMessage);
        string request = RequestManager.GetRequestForSentences(_tikToken!, fraction.Sentences);
        chat.AppendUserInput(request);
        string response = String.Empty;
        try
        {
            response = await chat.GetResponseFromChatbotAsync();
        }
        catch
        {
            // ignored
        }

        return response;
    }

    private static Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Error. Something went wrong:\n{exception}");
        return Task.CompletedTask;
    }
    
    public static void Combine(string outputFile, params string[] inputFiles)
    {
        byte[] buffer = new byte[1024];
        WaveFileWriter? waveFileWriter = null;

        try
        {
            foreach (string sourceFile in inputFiles)
            {
                using WaveFileReader reader = new(sourceFile);
                if (waveFileWriter == null)
                {
                    // first time in create new Writer
                    waveFileWriter = new WaveFileWriter(outputFile, reader.WaveFormat);
                }
                else
                {
                    if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat))
                    {
                        throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
                    }
                }

                int read;
                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    waveFileWriter.Write(buffer, 0, read);
                }
            }
        }
        finally
        {
            if (waveFileWriter != null)
            {
                waveFileWriter.Dispose();
            }
        }
    }
}

public class WavFinder
{
    private readonly string _dir;

    public WavFinder(string dir)
    {
        _dir = dir;
    }

    public string[] FindWavFiles()
    {
        return Directory.EnumerateFiles(_dir, "*.wav", SearchOption.AllDirectories).ToArray();
    }
}