FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build-env

WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

COPY piper.tar.gz ./
RUN mkdir -p piper && tar -xzvf piper.tar.gz -C out && rm piper.tar.gz

COPY voice.tar.gz ./
RUN tar -xzvf voice.tar.gz -C out/piper && rm voice.tar.gz

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy

WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 80

ENTRYPOINT ["dotnet", "TTSApi.dll"]