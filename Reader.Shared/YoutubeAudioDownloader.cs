using System.Reflection;
using CliWrap;
using CliWrap.Buffered;

namespace Reader.Shared;

public class YoutubeAudioDownloader
{
    public static async Task GetAudioFromYoutube(string url, string outputPath)
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        string currentDirectory = Path.GetDirectoryName(currentAssembly.Location)!;

        await Cli.Wrap("chmod")
            .WithWorkingDirectory(currentDirectory)
            .WithArguments("u+x \"yt-dlp_linux\"")
            .ExecuteAsync();

        string arguments =
            $"-c \"./yt-dlp_linux -x --audio-format mp3 -o {outputPath} '{url}'\"";

        Command cmd = Cli.Wrap("/bin/bash")
            .WithWorkingDirectory(currentDirectory)
            .WithArguments(arguments);

        await cmd.ExecuteBufferedAsync();
    }
}