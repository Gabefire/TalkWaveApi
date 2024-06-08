# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ENV DOTNET_EnableWriteXorExecute=0
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ./TalkWaveApi.WebSocket.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://*:80
COPY --from=build /app ./
EXPOSE 80

ENTRYPOINT ["dotnet", "TalkWaveApi.dll"]