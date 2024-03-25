# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ./TalkWaveApi/*.csproj ./TalkWaveApi/
RUN dotnet restore ./TalkWaveApi/TalkWaveApi.csproj

# copy everything else and build app
COPY TalkWaveApi/. ./TalkWaveApi/
WORKDIR /source/TalkWaveApi
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "TalkWaveApi.dll"]