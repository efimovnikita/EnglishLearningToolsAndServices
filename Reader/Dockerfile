# First stage: SDK is used to build the app
FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy as build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# Second stage: Runtime is used to run the app
FROM mcr.microsoft.com/dotnet/aspnet:6.0-jammy
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
EXPOSE 443

# Update and install ffmpeg
RUN apt-get update \
   && apt-get install -y ffmpeg \
   && rm -rf /var/lib/apt/lists/* 

ENTRYPOINT ["./Reader"]