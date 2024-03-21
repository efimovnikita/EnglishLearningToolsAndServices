using Microsoft.AspNetCore.Http.Features;
using Reader.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services for Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.Configure<FormOptions>(options => {
    options.MultipartBodyLengthLimit = 209715200; // Set the file size limit to 200 MB
});

// Configure the server to listen to any host on ports 5000 and 5001
builder.WebHost.UseUrls("http://*:5000", "https://*:5001");

WebApplication app = builder.Build();

// Add middleware for Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enable middleware to serve generated Swagger as a JSON endpoint
    app.UseSwaggerUI(); // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint
}

app.MapGet("/", () => "Hello World!");

app.MapGet("/api/getAudioFromYoutube", async (string url) => 
{
    string fileName = Path.GetRandomFileName() + ".mp3"; // Use a random filename for the MP3 file
    string tempFilePath = Path.Combine(Path.GetTempPath(), fileName); // This is where the file will be temporarily stored
    
    // Get audio from youtube and save to file
    await YoutubeAudioDownloader.GetAudioFromYoutube(url, tempFilePath);

    // Check if the file was correctly created
    if (!File.Exists(tempFilePath))
    {
        return Results.Problem("The audio file could not be created.");
    }
    
    // Stream the file back to the user
    FileStream stream = new(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
    return Results.File(stream, "audio/mpeg", fileName);
});

app.MapGet("/api/getTextFromYoutube", async (string url, string token) => 
{
    TextContentExtractor textContentExtractor = new(token);
    string textContentFromUrl = await textContentExtractor.ExtractTextContentFromUrlAsync(url);
    return Results.Text(String.IsNullOrEmpty(textContentFromUrl) == false ? textContentFromUrl : "");
});

app.MapPost("/api/getTextFromAudio", async (HttpContext httpContext) =>
{
    IFormFile? audioFile = httpContext.Request.Form.Files["audioFile"];
    string? token = httpContext.Request.Form["token"];
    
    if (audioFile == null)
    {
        return Results.Problem("The audio file wasn't found");
    }
    
    if (String.IsNullOrEmpty(token))
    {
        return Results.Problem("The token wasn't found");
    }
    
    using var stream = new MemoryStream();
    await audioFile.CopyToAsync(stream);
    byte[] fileBytes = stream.ToArray();

    string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".mp3");

    try
    {
        await File.WriteAllBytesAsync(tempFilePath, fileBytes);
        
        AudioTranscriberAndTranslator transcriberAndTranslator = new(token);
        string result = await transcriberAndTranslator.TranscribeAudio(tempFilePath);
        
        return Results.Text(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
    finally
    {
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }
}).DisableAntiforgery();

app.UseCors(policyBuilder =>
    policyBuilder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

app.Run();
