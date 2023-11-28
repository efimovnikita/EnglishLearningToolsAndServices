using FFMpegCore;
using Reader.Shared;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace YoutubeAudioBot;

internal class Program
{
    private static List<int>? _allowedUsersList;
    private static string? _endpointUrl;

    public static void Main(string[] args)
    {
        string? telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_API_KEY");
        if (telegramBotApiKey == null)
        {
            Console.WriteLine("Telegram bot api key didn't set");
            return;
        }

        string? allowedUsers = Environment.GetEnvironmentVariable("ALLOWED_USERS");
        if (allowedUsers == null)
        {
            Console.WriteLine("List of allowed users didn't set");
            return;
        }
        _allowedUsersList = allowedUsers.Split(' ').Select(Int32.Parse).ToList();

        _endpointUrl = Environment.GetEnvironmentVariable("ENDPOINT_URL");
        if (String.IsNullOrEmpty(_endpointUrl))
        {
            Console.WriteLine("Api endpoint url didn't set");
            return;
        }

        TelegramBotClient client = new(telegramBotApiKey);
        client.StartReceiving(UpdateHandler, ErrorHandler);

        Console.WriteLine("Listening...");

        Console.ReadLine();
    }
    
    private static Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Error. Something went wrong:\n{exception}");
        return Task.CompletedTask;
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
        
        Console.WriteLine($"------\nNew request received from '{chatId}'");
        
        string? text = message.Text;
        if (text == null)
        {
            return;
        }
        
        try
        {
            if (IsUserAllowed(chatId))
            {
                await client.SendTextMessageAsync(chatId,
                    "You don't have privileges to use this bot.",
                    replyToMessageId: messageId,
                    cancellationToken: token);
                return;
            }
            
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

            Console.WriteLine($"Url: {url}");

            if (TextContentExtractor.IsUrlFromYouTube(url) == false)
            {
                await client.SendTextMessageAsync(chatId,
                    "Only Youtube link are allowed...",
                    replyToMessageId: messageId,
                    cancellationToken: token);
                return;
            }

            HttpClientHandler httpClientHandler = new()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            
            using HttpClient httpClient = new(httpClientHandler);
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            Console.WriteLine($"Making the request to '{_endpointUrl}'");
            
            HttpResponseMessage response = await httpClient.GetAsync($"{_endpointUrl}/api/getAudioFromYoutube?url={url}", token);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to download file: {response.ReasonPhrase}");
                return;
            }

            Console.WriteLine($"Processing response from '{_endpointUrl}'");

            string fileName = Path.GetRandomFileName() + ".mp3";
            string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

            await using FileStream fileStream = new(tempFilePath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fileStream, token);

            Console.WriteLine($"File saved to {tempFilePath}");

            FileInfo fileInfo = new(tempFilePath);
            long sizeInBytes = fileInfo.Length;

            double sizeInMegabytes = (double) sizeInBytes / (1024 * 1024);

            Console.WriteLine("The size of the file is: {0} MB", sizeInMegabytes);

            if (sizeInMegabytes < 45)
            {
                Console.WriteLine($"Sending voice message '{tempFilePath}'");

                await client.SendVoiceAsync(chatId,
                    InputFile.FromStream(File.OpenRead(tempFilePath)),
                    replyToMessageId: messageId,
                    cancellationToken: token);

                Console.WriteLine($"Request from '{chatId}' successfully processed");

                return;
            }

            Console.WriteLine("Files size more than 45 MB. Chunking...");

            List<string> chunkPaths =
                await SplitMp3IntoChunks(tempFilePath, Path.GetTempPath(), 3600);

            await SendChunks(client, token, chunkPaths, chatId, messageId);

            File.Delete(tempFilePath);

            Console.WriteLine("Original audio file deleted successfully");
            Console.WriteLine($"Request from '{chatId}' successfully processed");
        }
        catch (Exception e)
        {
            await client.SendTextMessageAsync(chatId,
                "Error. Something went wrong. See logs...",
                replyToMessageId: messageId,
                cancellationToken: token);
            Console.WriteLine($"Error. Something went wrong:\n{e}");
        }
    }

    private static async Task SendChunks(ITelegramBotClient client, CancellationToken token, List<string> chunkPaths, long chatId,
        int messageId)
    {
        Console.WriteLine("Sending chunks...");
        
        foreach (string chunkPath in chunkPaths)
        {
            FileInfo info = new(chunkPath);
            long length = info.Length;
            long mbs = length / (1024 * 1024);
            Console.WriteLine($"Chunk size: {mbs} MB");

            if (mbs >= 45)
            {
                Console.WriteLine("Chunk is still more than 45 MB. Skipping...");
                continue;
            }

            Console.WriteLine($"Sending voice chunk '{chunkPath}'");

            await client.SendVoiceAsync(chatId,
                InputFile.FromStream(File.OpenRead(chunkPath)),
                replyToMessageId: messageId,
                cancellationToken: token);

            File.Delete(chunkPath);
            Console.WriteLine($"Chunk '{chunkPath}' deleted successfully");
        }
    }

    private static bool IsUserAllowed(long chatId)
    {
        return _allowedUsersList != null && _allowedUsersList.Contains((int) chatId) == false;
    }

    private static async Task<List<string>> SplitMp3IntoChunks(string inputFilePath, string outputDirectory, int chunkDurationInSeconds)
    {
        List<string> chunkPaths = new();

        if (!File.Exists(inputFilePath))
        {
            Console.WriteLine("The input file does not exist.");
            return chunkPaths;
        }

        IMediaAnalysis mediaInfo = await FFProbe.AnalyseAsync(inputFilePath);
        TimeSpan totalDuration = mediaInfo.Duration;
        TimeSpan chunkDuration = TimeSpan.FromSeconds(chunkDurationInSeconds);
        
        const int bitrate = 32;
        Console.WriteLine($"Current bitrate is: {bitrate}");

        int counter = 1;

        for (TimeSpan start = TimeSpan.Zero; start < totalDuration; start += chunkDuration)
        {
            string outputFilePath = Path.Combine(outputDirectory, $"{Guid.NewGuid()}{counter:D3}.mp3");
            TimeSpan st = start;
            FFMpegArgumentProcessor options = FFMpegArguments
                .FromFileInput(new FileInfo(inputFilePath), opt => opt.Seek(st))
                .OutputToFile(outputFilePath, false, opt =>
                {
                    opt
                        .WithAudioCodec("libmp3lame")
                        .WithAudioBitrate(bitrate)
                        .WithDuration(chunkDuration);
                });
            
            Console.WriteLine($"Process file: {outputFilePath}");
            
            await options.ProcessAsynchronously();
            chunkPaths.Add(outputFilePath);
            counter++;
        }

        return chunkPaths;
    }
}