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

        builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));

        CoconaApp app = builder.Build();

        app.AddCommand("url",
            async (ILearnService service, [Argument] string url) => await service.GetAudioSplitAndAskUser(url));
        app.AddCommand("continue",
            async (IContinueService service) => await service.ContinueLearning());
        app.AddCommand("clear", () => { });

        app.Run();
    }
}
