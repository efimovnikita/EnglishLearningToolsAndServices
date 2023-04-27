FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY bin/Release/net6.0 .

ENTRYPOINT ["./Reader"]