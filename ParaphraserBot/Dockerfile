FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ParaphraserBot/ParaphraserBot.csproj", "ParaphraserBot/"]
RUN dotnet restore "ParaphraserBot/ParaphraserBot.csproj"
COPY . .
WORKDIR "/src/ParaphraserBot"
RUN dotnet build "ParaphraserBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ParaphraserBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ParaphraserBot.dll"]
