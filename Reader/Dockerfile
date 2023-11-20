FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy as build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
EXPOSE 443

RUN dotnet dev-certs https --clean
RUN dotnet dev-certs https --verbose

RUN apt-get update \
   && apt-get install -y ffmpeg \
   && rm -rf /var/lib/apt/lists/* 

ENTRYPOINT ["./Reader"]