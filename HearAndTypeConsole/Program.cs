using Cocona;
using Cocona.Builder;
using HearAndTypeConsole.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HearAndTypeConsole;

internal class Program
{
    // ReSharper disable once UnusedParameter.Local
    private static void Main(string[] args)
    {
        CoconaAppBuilder builder = CoconaApp.CreateBuilder();

        builder.Services.AddTransient<IAskerService, AskerService>();
        builder.Services.AddTransient<IContinueService, ContinueService>();
        builder.Services.AddTransient<IAudioDownloaderService, AudioDownloaderService>();
        builder.Services.AddTransient<ISplitterService, SplitterService>();
        builder.Services.AddTransient<ILearnService, LearnService>();
        builder.Services.AddTransient<IClearService, ClearService>();

        builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, Constants.ConfigFileName));

        CoconaApp app = builder.Build();

        app.AddCommand("url",
                async (ILearnService service, [Argument(Description = "The YouTube link.")] string url) =>
                await service.GetAudioSplitAndAskUser(url))
            .WithDescription("Start a train session, that based on the audio from the YouTube video.");

        app.AddCommand("continue",
                async (IContinueService service) => await service.ContinueLearning())
            .WithDescription("Start the previous session.");

        app.AddCommand("clear", (IClearService clearService) => clearService.Clear())
            .WithDescription("Clear the temp folder from previously downloaded files.");

        app.Run();
    }
}
