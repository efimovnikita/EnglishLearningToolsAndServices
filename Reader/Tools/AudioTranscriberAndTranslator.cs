using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reader.Tools;

public class AudioTranscriberAndTranslator
{
    private readonly HttpClient _httpClient;

    public AudioTranscriberAndTranslator(string openAiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiKey}");
    }

    public async Task<string> TranscribeAudio(string fileName)
    {
        return await GetText(fileName, "https://api.openai.com/v1/audio/transcriptions");
    }
    
    public async Task<string> TranslateAudio(string fileName)
    {
        return await GetText(fileName, "https://api.openai.com/v1/audio/translations");
    }

    private async Task<string> GetText(string fileName, string endpointUrl)
    {
        ByteArrayContent byteArrayContent = new(File.ReadAllBytes(fileName));
        byteArrayContent.Headers.Remove("Content-Type");
        byteArrayContent.Headers.TryAddWithoutValidation("Content-Type", "application/octet-stream");

        StringContent modelContent = new("whisper-1");

        using MultipartFormDataContent formData = new();
        formData.Add(byteArrayContent, "file", Path.GetFileName(fileName));
        formData.Add(modelContent, "model");

        HttpResponseMessage response =
            await _httpClient.PostAsync(endpointUrl, formData);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync();

        JObject? jsonResponse = JsonConvert.DeserializeObject<JObject>(responseString);
        if (jsonResponse == null)
        {
            throw new Exception("Error when trying to get transcript from audio");
        }

        string text = (jsonResponse["text"] ?? throw new Exception("Error when trying to get transcript from audio"))
            .Value<string>() ?? "";

        return text;
    }
}
