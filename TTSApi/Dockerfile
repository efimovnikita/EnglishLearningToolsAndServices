# Use the .NET 6.0 SDK (Software Development Kit) as base image to build our application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

# Set the working directory
WORKDIR /app

# Copy csproj file(s) and restore any dependencies
# Adjust the project file names if needed
COPY *.csproj ./
RUN dotnet restore

# Copy all the files and build our application
COPY . ./
RUN dotnet publish -c Release -o out

COPY piper.tar.gz ./
RUN mkdir -p piper && tar -xzvf piper.tar.gz -C out && rm piper.tar.gz

COPY voice.tar.gz ./
RUN tar -xzvf voice.tar.gz -C out/piper && rm voice.tar.gz

# Generate the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 80

# Define the entrypoint, replace 'TTSApi.dll' with your own .dll file
ENTRYPOINT ["dotnet", "TTSApi.dll"]