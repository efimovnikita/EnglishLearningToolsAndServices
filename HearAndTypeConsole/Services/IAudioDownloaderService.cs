namespace HearAndTypeConsole.Services;

internal interface IAudioDownloaderService
{
    Task<(bool, string)> DownloadAudio(string url, string? endpointUrl);
}