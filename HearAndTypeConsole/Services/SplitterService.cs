using CliWrap;
using OpenAI_API.Audio;

namespace HearAndTypeConsole.Services;

internal class SplitterService : ISplitterService
{
    public async Task<Dictionary<FileInfo, string>> SplitAudio(List<AudioResultVerbose.Segment> segments, string audioFilePath)
    {
        DirectoryInfo tempSubdirectory = Directory.CreateTempSubdirectory("hear_and_type_");

        for (int index = 0; index < segments.Count; index++)
        {
            AudioResultVerbose.Segment segment = segments[index];
            await SplitMp3Async(audioFilePath, tempSubdirectory.FullName, segment.start, segment.end,
                index.ToString());
        }

        List<FileInfo> files = tempSubdirectory.GetFiles("*.mp3")
            .Select(file => new
            {
                FileInfo = file,
                Number = Int32.Parse(Path.GetFileNameWithoutExtension(file.Name))
            })
            .OrderBy(file => file.Number)
            .Select(file => file.FileInfo)
            .ToList();

        Dictionary<FileInfo,string> result = new();
        for (int i = 0; i < files.Count; i++)
        {
            string file = files[i].FullName;
            AudioResultVerbose.Segment segment = segments[i];
            result.Add(new FileInfo(Path.Combine(Path.GetTempPath(), file)), segment.text);
        }
        
        return result;
    }

    private async Task SplitMp3Async(string inputFilePath, string outputDirectory, double startTimeInSeconds,
        double endTimeInSeconds, string outputFilePattern)
    {
        // Ensure the output directory exists
        Directory.CreateDirectory(outputDirectory);

        // Format the start and end times with rounding
        string formattedStartTime = FormatTimeForMp3Splt(startTimeInSeconds);
        string formattedEndTime = FormatTimeForMp3Splt(endTimeInSeconds);

        // Prepare arguments for the command
        string arguments =
            $"-o \"{outputFilePattern}\" \"{inputFilePath}\" {formattedStartTime} {formattedEndTime} -d {outputDirectory}";

        // Execute the mp3splt command using CliWrap
        Command cmd = Cli.Wrap("mp3splt")
            .WithArguments(arguments)
            .WithWorkingDirectory(outputDirectory);

        // Execute the command asynchronously
        await cmd.ExecuteAsync();
    }

    private string FormatTimeForMp3Splt(double timeInSeconds)
    {
        // Convert the time to a TimeSpan object
        TimeSpan timeSpan = TimeSpan.FromSeconds(timeInSeconds);
    
        // Round to the nearest centisecond (0.01 of a second)
        int centiseconds = (int)Math.Round(timeSpan.Milliseconds / 10.0);

        // Add the centiseconds back to the TimeSpan if there was rounding up to a new second
        if (centiseconds == 100)
        {
            timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
            centiseconds = 0;
        }

        // Return the formatted string
        return $"{(int)timeSpan.TotalMinutes}.{timeSpan:ss}.{centiseconds:00}";
    }
}