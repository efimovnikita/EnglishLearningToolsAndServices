FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy
WORKDIR /app
COPY bin/Release/net6.0/publish .

ENTRYPOINT ["dotnet", "YoutubeAudioBot.dll"]
