using OpenAI_API;

namespace HearAndTypeConsole.Services;

internal interface IAskerService
{
    Task AskQuestions(Dictionary<FileInfo, string> dictionary, OpenAIAPI api);
}