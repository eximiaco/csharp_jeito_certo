FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Eximia.Integrations.EventDispatcher/Eximia.Integrations.EventDispatcher.csproj", "Eximia.Integrations.EventDispatcher/"]
RUN dotnet restore "Eximia.Integrations.EventDispatcher/Eximia.Integrations.EventDispatcher.csproj"
COPY . .
WORKDIR "/src/Eximia.Integrations.EventDispatcher"
RUN dotnet build "Eximia.Integrations.EventDispatcher.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Eximia.Integrations.EventDispatcher.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Eximia.Integrations.EventDispatcher.dll"]
