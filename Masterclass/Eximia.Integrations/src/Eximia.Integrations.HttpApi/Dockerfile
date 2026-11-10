FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Eximia.Integrations.HttpApi/Eximia.Integrations.HttpApi.csproj", "Eximia.Integrations.HttpApi/"]
RUN dotnet restore "Eximia.Integrations.HttpApi/Eximia.Integrations.HttpApi.csproj"
COPY . .
WORKDIR "/src/Eximia.Integrations.HttpApi"
RUN dotnet build "Eximia.Integrations.HttpApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Eximia.Integrations.HttpApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Eximia.Integrations.HttpApi.dll"]
