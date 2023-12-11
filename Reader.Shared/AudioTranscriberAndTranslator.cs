using System.Reflection;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reader.Shared;

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

    private async Task<string> GetText(string fileName, string endpointUrl)
    {
        StringBuilder stringBuilder = new();
        List<string> tempParts = new();

        try
        {
            string? assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Get the duration of the audio file in seconds
            BufferedCommandResult durationProcess = await Cli.Wrap("ffprobe")
                .WithArguments(
                    $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 {fileName}")
                .ExecuteBufferedAsync();

            Double.TryParse(durationProcess.StandardOutput, out double duration);
            
            // Here we calculate how many segments based on fixed chunk duration
            const int chunkDuration = 600; // in seconds. Adjust if needed.
            int totalChunks = (int) Math.Ceiling(duration / chunkDuration);

            for (int i = 0; i < totalChunks; i++)
            {
                if (assemblyLocation == null)
                {
                    continue;
                }

                string partFileName = Path.Combine(assemblyLocation, $"output{i}.mp3");
                tempParts.Add(partFileName);

                await Cli.Wrap("ffmpeg")
                    .WithArguments($"-y -i {fileName} -ss {i * chunkDuration} -t {chunkDuration} {partFileName}")
                    .ExecuteAsync();

                if (!File.Exists(partFileName))
                {
                    continue;
                }

                ByteArrayContent byteArrayContent = new(File.ReadAllBytes(partFileName));
                byteArrayContent.Headers.Remove("Content-Type");
                byteArrayContent.Headers.TryAddWithoutValidation("Content-Type", "application/octet-stream");

                StringContent modelContent = new("whisper-1");

                using MultipartFormDataContent formData = new();
                formData.Add(byteArrayContent, "file", Path.GetFileName(partFileName));
                formData.Add(modelContent, "model");

                HttpResponseMessage response = await _httpClient.PostAsync(endpointUrl, formData);
                response.EnsureSuccessStatusCode();

                string responseString = await response.Content.ReadAsStringAsync();
                JObject? jsonResponse = JsonConvert.DeserializeObject<JObject>(responseString);
                string text = jsonResponse?["text"]?.Value<string>() ?? "";

                stringBuilder.Append(text);
                File.Delete(partFileName);
            }

            return stringBuilder.ToString();
        }
        catch
        {
            tempParts.Where(File.Exists).ToList().ForEach(File.Delete);
            return stringBuilder.ToString();
        }
    }
}
