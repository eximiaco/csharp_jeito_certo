FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

# Copy csproj and restore as distinct layers
COPY **/*.csproj ./


RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done

COPY . .

RUN dotnet restore Eximia.Integration.BackgroundAgent/

# Copy everything else and build
RUN dotnet publish -c Release -o /app --no-restore Eximia.Integration.BackgroundAgent/Eximia.Integration.BackgroundAgent.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runner

COPY --from=build /app/ .
ENTRYPOINT ["dotnet", "Eximia.Integration.BackgroundAgent.dll"]