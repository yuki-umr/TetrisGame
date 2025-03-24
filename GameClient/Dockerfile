FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GameClient/GameClient.csproj", "GameClient/"]
RUN dotnet restore "GameClient/GameClient.csproj"
COPY . .
WORKDIR "/src/GameClient"
RUN dotnet build "GameClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameClient.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameClient.dll"]
