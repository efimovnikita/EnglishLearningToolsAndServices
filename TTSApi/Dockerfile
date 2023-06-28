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

# Generate the runtime image
FROM mcr.microsoft.com/dotnet/sdk:6.0

WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 80
EXPOSE 443

RUN dotnet dev-certs https --clean
RUN dotnet dev-certs https --verbose

# Define the entrypoint, replace 'TTSApi.dll' with your own .dll file
ENTRYPOINT ["dotnet", "TTSApi.dll"]