using OpenAI_API.Audio;

namespace HearAndTypeConsole.Services;

internal interface ISplitterService
{
    Task<Dictionary<FileInfo, string>> SplitAudio(List<AudioResultVerbose.Segment> segments, string audioFilePath);
}