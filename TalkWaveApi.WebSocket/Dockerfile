# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ENV DOTNET_EnableWriteXorExecute=0
WORKDIR /source

# copy csproj and restore as distinct layers
COPY TalkWaveApi.WebSocket.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
EXPOSE 5432
EXPOSE 6379

RUN apt-get update \
    && apt-get install -y curl

ENTRYPOINT ["dotnet", "TalkWaveApi.WebSocket.dll"]
