namespace Reader.Models;

internal record TextToSpeechRequest(string Text, string ModelId, VoiceSettings VoiceSettings);