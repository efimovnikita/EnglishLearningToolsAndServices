namespace HearAndTypeConsole.Services;

internal interface ILearnService
{
    Task<int> GetAudioSplitAndAskUser(string url);
}