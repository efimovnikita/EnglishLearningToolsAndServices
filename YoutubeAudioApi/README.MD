# YouTube audio API

This project is a C# application that allows users to download audio from YouTube videos in MP3 format. It uses the YouTube API to retrieve the video and the FFmpeg library to convert it to MP3. The application is built using the .NET 5 framework and can be run on Windows, Linux, and macOS.

## Installation

To install the application, follow these steps:

1. Clone the repository to your local machine.
2. Open the solution file in IDE.
3. Build the solution.
4. Run the application.

## Docker

To run this application, you will need to have Docker installed on your system. Once Docker is installed, you can build the image using the following command:

```
docker build -t myapp .
```

This will create a Docker image with the tag "myapp". To run the container, use the following command:

```
docker run -p 80:80 -p 443:443 myapp
```

This will start the container and map ports 80 and 443 to the host machine. You can then access the application by navigating to http://localhost:80 or https://localhost:443 in your web browser.

## Usage

This command line app provides an API endpoint for getting audio from a YouTube video.

To use the API endpoint, send a GET request to `/api/getAudioFromYoutube` with the `url` parameter set to the YouTube video URL. The endpoint will return an MP3 file of the audio from the video.

Example usage:

```
GET http://localhost:5000/api/getAudioFromYoutube?url=https://www.youtube.com/watch?v=dQw4w9WgXcQ
```

This will return an MP3 file of the audio from the YouTube video with the URL `https://www.youtube.com/watch?v=dQw4w9WgXcQ`.


## Contributing

Contributions to this project are welcome. To contribute, follow these steps:

1. Fork the repository.
2. Create a new branch.
3. Make your changes.
4. Submit a pull request.

## License

This project is licensed under the MIT License. See the LICENSE file for details.