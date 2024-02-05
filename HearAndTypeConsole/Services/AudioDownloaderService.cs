using Spectre.Console;

namespace HearAndTypeConsole.Services;

internal class AudioDownloaderService : IAudioDownloaderService
{
    public async Task<(bool, string)> DownloadAudio(string url, string? endpointUrl)
    {
        try
        {
            HttpClientHandler httpClientHandler = new()
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using HttpClient httpClient = new(httpClientHandler);
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            HttpResponseMessage audioResponse =
                await httpClient.GetAsync($"{endpointUrl}/api/getAudioFromYoutube?url={url}");

            if (!audioResponse.IsSuccessStatusCode)
            {
                return (false, "");
            }

            string fileName = Path.GetRandomFileName() + ".mp3";
            string audioFilePath = Path.Combine(Path.GetTempPath(), fileName);

            await using FileStream fileStream = new(audioFilePath, FileMode.Create, FileAccess.Write);
            await audioResponse.Content.CopyToAsync(fileStream);
                    
            return (true, audioFilePath);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return (false, "");
        }
    }
}