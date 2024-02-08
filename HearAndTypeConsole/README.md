# HearAndType: A Console Application for Audio Transcription Practice

HearAndType is a console application designed to help users improve their listening skills and typing accuracy through transcription practice. This tool is ideal for language learners, transcribers, or anyone looking to enhance their audio comprehension abilities.

## Features

- Audio Extraction: Downloads audio directly from YouTube videos.
- Transcription: Leverages the OpenAI API to transcribe audio and provide detailed segments for practice.
- Interactive Practice: Prompts users to type what they hear and provides immediate feedback.
- Repetition: Allows users to replay audio segments for additional practice.
- Session Continuation: Save and resume transcription practice sessions.
- Temporary File Management: Automatically handles the cleanup of temporary files and directories.

## Prerequisites

Before running HearAndType, ensure that you have:

- .NET 6.0 Runtime and SDK installed.
- `ffmpeg` and `mp3splt` command-line utilities installed for audio processing.
- An OpenAI API key.

## Installation

To get started with HearAndType:

1. Clone the repository to your local machine.
2. Navigate to the project directory.
3. Build the application using the .NET command-line interface.

## Configuration

Set up your environment variables:

- `API_KEY`: Your OpenAI API key.
- `ENDPOINT`: Your Audio extraction API endpoint URL.

Alternatively, set these values in the `appsettings.json` configuration file:

```json
{
  "API_KEY": "your-api-key",
  "ENDPOINT": "your-audio-extraction-endpoint-url"
}
```

## Usage

Run the application with the desired command:

- Start a new transcription practice session with a YouTube URL.
  ```sh
  dotnet run -- url <YouTube Link>
  ```
- Continue with a previous transcription practice session.
  ```sh
  dotnet run -- continue
  ```
- Clear temporary files from previous sessions.
  ```sh
  dotnet run -- clear
  ```

### Interactive Commands

While practicing, the following interactive commands are available:

- Typing `r` replays the current audio segment.
- Typing `q` quits the current practice session.

## Contributing

Contributions are welcome! If you have ideas for improvements or want to report bugs, please open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the LICENSE file for more details.

## Acknowledgments

- OpenAI for the powerful GPT-based transcriptions API.
- Developers of `ffmpeg` and `mp3splt` for their audio processing tools.

Enjoy practicing your transcription skills with HearAndType!