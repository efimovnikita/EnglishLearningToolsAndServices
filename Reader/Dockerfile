FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY bin/Release/net6.0/publish .

RUN dotnet dev-certs https --clean
RUN dotnet dev-certs https --verbose

ENTRYPOINT ["./Reader"]