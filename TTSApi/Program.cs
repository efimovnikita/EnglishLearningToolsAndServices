using System.Reflection;
using CliWrap;
using CliWrap.Buffered;
using TTSApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(); 
builder.Services.AddCors();

builder.WebHost.UseUrls("http://*:5000");

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/TextToSpeech", async (TextRequest request, IWebHostEnvironment _, ILogger<Program> logger) =>
{
    logger.LogInformation("Received a new TextToSpeech request");
    
   if (String.IsNullOrEmpty(request.Text))
   {
       logger.LogWarning("Received request with empty text");
       return Results.BadRequest("Text is required.");
   }

   string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
   string outputFilePath = Path.Combine(location, "output.wav");

   // Sanitize the input text
   string sanitizedText = System.Net.WebUtility.HtmlEncode(request.Text);
   logger.LogInformation("Text has been successfully sanitized");

   try
   {
       Command cmd = Cli.Wrap("/bin/bash")
           .WithWorkingDirectory(Path.Combine(location, "piper"))
           .WithArguments(
               $"-c \"echo '{sanitizedText}' | ./piper -m en-us-amy-low.onnx --output_file {outputFilePath}\"");
       await cmd.ExecuteBufferedAsync();
       logger.LogInformation("Successfully generated audio at {Path}", outputFilePath);
   }
   catch (Exception exception)
   {
       // Log the error for debug purposes
       logger.LogError(exception, "An error occurred while generating the audio");
       return Results.Problem("An error occurred while generating the audio.");
   }

   DeleteOnCloseStream fileStream;
   try
   {
       // Obtaining an instance of ILogger specific to the "DeleteOnCloseStream" type through dependency injection.
       ILogger<DeleteOnCloseStream> deleteOnCloseStreamLogger =
           app.Services.GetRequiredService<ILogger<DeleteOnCloseStream>>();
       fileStream = new DeleteOnCloseStream(deleteOnCloseStreamLogger, outputFilePath, FileMode.Open);
       logger.LogInformation("Successfully opened the generated audio file");
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