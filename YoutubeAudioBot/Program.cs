using Reader.Tools;
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

            if (TextContentExtractor.IsUrlFromYouTube(url) == false)
            {
                await client.SendTextMessageAsync(chatId,
                    "Only Youtube link are allowed...",
                    replyToMessageId: messageId,
                    cancellationToken: token);
                return;
            }

            using HttpClient httpClient = new();
            HttpResponseMessage response = await httpClient.GetAsync($"{_endpointUrl}/api/getAudioFromYoutube?url={url}", token);

            if (response.IsSuccessStatusCode)
            {
                string fileName = Path.GetRandomFileName() + ".mp3"; 
                string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

                await using FileStream fileStream = new(tempFilePath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fileStream, token);

                Console.WriteLine($"File saved to {tempFilePath}");

                await client.SendAudioAsync(chatId,
                    InputFile.FromStream(File.OpenRead(tempFilePath)),
                    replyToMessageId: messageId,
                    cancellationToken: token);
                
                // After done, delete the file.
                File.Delete(tempFilePath);

                Console.WriteLine("File deleted successfully");
            }
            else
            {
                Console.WriteLine($"Failed to download file: {response.ReasonPhrase}");
            }
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
}