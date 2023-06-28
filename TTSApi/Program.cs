using System.Reflection;
using TTSApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(); 
builder.Services.AddCors();

string? apiKey = Environment.GetEnvironmentVariable("API_KEY");

if (String.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("API_KEY environment variables are not set");
}

builder.Services.AddHttpClient("SaluteSpeechServiceClient")
           .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
   ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});
       
builder.Services.AddSingleton(p =>
{
   var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
   var client = httpClientFactory.CreateClient("SaluteSpeechServiceClient");
   return new SaluteSpeechService(client, apiKey);
});

builder.WebHost.UseUrls("http://*:5000", "https://*:5001");

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/TextToSpeech", async (TextRequest request, IWebHostEnvironment _, ILogger<Program> logger, SaluteSpeechService speechService) =>
{
   if (String.IsNullOrEmpty(request.Text))
   {
       return Results.BadRequest("Text is required.");
   }

   string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
   string outputFilePath = Path.Combine(location, "output.wav");

   // Sanitize the input text
   string sanitizedText = System.Net.WebUtility.HtmlEncode(request.Text);

   try
   {
       await speechService.SynthesizeTextToFileAsync(sanitizedText, outputFilePath);
   }
   catch (Exception e)
   {
       // Log the error for debug purposes
       logger.LogError(e, "An error occurred while generating the audio");
       return Results.Problem("An error occurred while generating the audio.");
   }

   DeleteOnCloseStream fileStream;
   try
   {
       fileStream = new DeleteOnCloseStream(outputFilePath, FileMode.Open);
   }
   catch (Exception e)
   {
       // Log the error for debug purposes
       logger.LogError(e, "An error occurred while reading the audio file");
       return Results.Problem("An error occurred while reading the audio file.");
   }

   return Results.Stream(fileStream, "audio/wav");
});

app.UseCors(policyBuilder =>
    policyBuilder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

app.Run();