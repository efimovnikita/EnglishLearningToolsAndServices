using Spectre.Console;

namespace HearAndTypeConsole.Services;

internal class ClearService : IClearService
{
    public void Clear()
    {
        RemoveDataFile();
        RemoveTempDirectories();
    }

    private static void RemoveTempDirectories()
    {
        try
        {
            string[] directories = Directory.GetDirectories(Path.GetTempPath(), $"{Constants.DirectoryNamePrefix}*", SearchOption.TopDirectoryOnly);
                
            foreach (string directory in directories)
            {
                Directory.Delete(directory, true);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }

    private static void RemoveDataFile()
    {
        try
        {
            string dataPath = Path.Combine(AppContext.BaseDirectory, Constants.CurrentDataFileName);
            if (File.Exists(dataPath))
            {
                File.Delete(dataPath);
            }
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }
    }
}