using Reader.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services for Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

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

app.UseCors(policyBuilder =>
    policyBuilder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

app.Run();
