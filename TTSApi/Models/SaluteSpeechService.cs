using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TTSApi.Models;

public class SaluteSpeechService
{
    private readonly HttpClient _httpClient;

    private readonly string _authData;
    private TokenResponse? _tokenResponse;

    public SaluteSpeechService(HttpClient httpClient, string authData)
    {
        _httpClient = httpClient;
        _authData = authData;
    }

    private async Task<TokenResponse?> GetAccessTokenAsync()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");
        request.Headers.Add("Authorization", $"Basic {_authData}");
        request.Headers.Add("RqUID", Guid.NewGuid().ToString());
        request.Content =
            new StringContent("scope=SALUTE_SPEECH_PERS", Encoding.UTF8, "application/x-www-form-urlencoded");

        HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseContent = await response.Content.ReadAsStringAsync();
        TokenResponse? tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

        // Convert the expiration time to seconds
        if (tokenResponse == null)
        {
            return null;
        }

        tokenResponse.ExpiresAt /= 1000;

        return tokenResponse;
    }

    private async Task<string?> GetOrRefreshAccessTokenAsync()
    {
        // If the token is expired or close to expiring, get a new one
        if (_tokenResponse == null || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= _tokenResponse.ExpiresAt - 60)
        {
            _tokenResponse = await GetAccessTokenAsync();
        }

        return _tokenResponse?.AccessToken;
    }

    public async Task SynthesizeTextToFileAsync(string text, string filePath, string voice = "Kin_24000")
    {
        HttpRequestMessage request = new(HttpMethod.Post,
            $"https://smartspeech.sber.ru/rest/v1/text:synthesize?voice={voice}");
        request.Headers.Add("Authorization", $"Bearer {await GetOrRefreshAccessTokenAsync()}");
        request.Content = new StringContent(text, Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/text");

        HttpResponseMessage response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int) response.StatusCode} ({response.StatusCode}).\nContent: {errorContent}");
        }

        response.EnsureSuccessStatusCode();

        await using FileStream fileStream = File.Create(filePath);
        await response.Content.CopyToAsync(fileStream);
    }
}