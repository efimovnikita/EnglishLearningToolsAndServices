using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reader.Tools;

public class AudioTranscriber
{
    private readonly HttpClient _httpClient;

    public AudioTranscriber(string openAiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiKey}");
    }

    public async Task<string> TranscribeAudio(string fileName)
    {
        ByteArrayContent byteArrayContent = new(File.ReadAllBytes(fileName));
        byteArrayContent.Headers.Remove("Content-Type");
        byteArrayContent.Headers.TryAddWithoutValidation("Content-Type", "application/octet-stream");
        
        StringContent modelContent = new("whisper-1");

        using MultipartFormDataContent formData = new();
        formData.Add(byteArrayContent, "file", Path.GetFileName(fileName));
        formData.Add(modelContent, "model");

        HttpResponseMessage response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", formData);
        response.EnsureSuccessStatusCode();
        string responseString = await response.Content.ReadAsStringAsync();

        JObject? jsonResponse = JsonConvert.DeserializeObject<JObject>(responseString);
        if (jsonResponse == null)
        {
            throw new Exception("Error when trying to get transcript from audio");
        }

        string text = jsonResponse["text"]!.Value<string>() ?? "";

        return text;
    }
}
