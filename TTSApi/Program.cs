using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using TTSApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(); 
builder.Services.AddCors();
builder.Services.AddHttpContextAccessor();

builder.WebHost.UseUrls("http://*:5000");

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/TextToSpeech", async (TextRequest request, IWebHostEnvironment _, ILogger<Program> logger, 
    IHttpContextAccessor accessor) =>
{
    IPAddress? ipAddress = accessor.HttpContext!.Connection.RemoteIpAddress;

    logger.LogInformation("Received a new TextToSpeech request from IP: {IP}", ipAddress);
    
   if (String.IsNullOrEmpty(request.Text))
   {
       logger.LogWarning("Received request with empty text from IP: {IP}", ipAddress);
       return Results.BadRequest("Text is required.");
   }

   string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
   string outputFilePath = Path.Combine(location, "output.wav");

   // Sanitize the input text
   string sanitizedText = WebUtility.HtmlEncode(request.Text);
   logger.LogInformation("Text from {IP} has been successfully sanitized", ipAddress);

   try
   {
       Command cmd = Cli.Wrap("/bin/bash")
           .WithWorkingDirectory(Path.Combine(location, "piper"))
           .WithArguments(
               $"-c \"echo '{sanitizedText}' | ./piper -m en-us-amy-low.onnx --output_file {outputFilePath}\"");
       await cmd.ExecuteBufferedAsync();
       logger.LogInformation("Successfully generated audio at {Path} for IP: {IP}", outputFilePath, ipAddress);
   }
   catch (Exception exception)
   {
       // Log the error for debug purposes
       logger.LogError(exception, "An error occurred while generating the audio for IP: {IP}", ipAddress);
       return Results.Problem("An error occurred while generating the audio.");
   }

   DeleteOnCloseStream fileStream;
   try
   {
       // Obtaining an instance of ILogger specific to the "DeleteOnCloseStream" type through dependency injection.
       ILogger<DeleteOnCloseStream> deleteOnCloseStreamLogger =
           app.Services.GetRequiredService<ILogger<DeleteOnCloseStream>>();
       fileStream = new DeleteOnCloseStream(deleteOnCloseStreamLogger, outputFilePath, FileMode.Open);
       logger.LogInformation("Successfully opened the generated audio file for IP: {IP}", ipAddress);
   }
   catch (Exception e)
   {
       // Log the error for debug purposes
       logger.LogError(e, "An error occurred while reading the audio file for {IP}", ipAddress);
       return Results.Problem("An error occurred while reading the audio file.");
   }

   return Results.Stream(fileStream, "audio/wav");
});

app.MapPost("/TextToSpeechOpenAI", async (TextRequestWithToken request, IWebHostEnvironment _, ILogger<Program> logger, 
    IHttpContextAccessor accessor) =>
{
    IPAddress? ipAddress = accessor.HttpContext!.Connection.RemoteIpAddress;

    logger.LogInformation("Received a new TextToSpeech request from IP: {IP}", ipAddress);
    
   if (String.IsNullOrEmpty(request.Text))
   {
       logger.LogWarning("Received request with empty text from IP: {IP}", ipAddress);
       return Results.BadRequest("Text is required.");
   }
   
   if (String.IsNullOrWhiteSpace(request.Token))
   {
       logger.LogWarning("Received request with empty token from IP: {IP}", ipAddress);
       return Results.BadRequest("Token is required.");
   }

   // Sanitize the input text
   string sanitizedText = WebUtility.HtmlEncode(request.Text);
   logger.LogInformation("Text from {IP} has been successfully sanitized", ipAddress);
   
   if (sanitizedText.Length >= 4096)
   {
       logger.LogWarning("Received input that is long than 4096 chars ({Length} chars) IP: {IP}", sanitizedText.Length, ipAddress);
       return Results.BadRequest("Input is more than 4096 chars.");
   }

   string outputFilePath;
   try
   {
       outputFilePath = await CreateSpeechAsync(request.Token, sanitizedText);
   }
   catch (Exception)
   {
       return Results.Problem("An error occurred while receiving the audio.");
   }
   
   if (String.IsNullOrEmpty(outputFilePath))
   {
       logger.LogWarning("Audio file path is empty, IP: {IP}", ipAddress);
       return Results.BadRequest("Audio file path is empty.");
   }

   DeleteOnCloseStream fileStream;
   try
   {
       // Obtaining an instance of ILogger specific to the "DeleteOnCloseStream" type through dependency injection.
       ILogger<DeleteOnCloseStream> deleteOnCloseStreamLogger =
           app.Services.GetRequiredService<ILogger<DeleteOnCloseStream>>();
       fileStream = new DeleteOnCloseStream(deleteOnCloseStreamLogger, outputFilePath, FileMode.Open);
       logger.LogInformation("Successfully opened the generated audio file for IP: {IP}", ipAddress);
   }
   catch (Exception e)
   {
       // Log the error for debug purposes
       logger.LogError(e, "An error occurred while reading the audio file for {IP}", ipAddress);
       return Results.Problem("An error occurred while reading the audio file.");
   }

   return Results.Stream(fileStream, "audio/mp3");
});

async Task<string> CreateSpeechAsync(string token, string input)
{
    try
    {
        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); 

        StringContent content = new("{\"model\": \"tts-1\", \"input\": \"" + input + "\", \"voice\": \"nova\"}", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync("https://api.openai.com/v1/audio/speech", content);

        if (!response.IsSuccessStatusCode)
        {
            return String.Empty;
        }

        byte[] result = await response.Content.ReadAsByteArrayAsync();
        string tempPath = Path.Combine(Path.GetTempPath(), "speech.mp3");
        await File.WriteAllBytesAsync(tempPath, result);
        return tempPath;
    }
    catch
    {
        return String.Empty;
    }
}

app.UseCors(policyBuilder =>
    policyBuilder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

app.Run();