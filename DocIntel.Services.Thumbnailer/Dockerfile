FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
VOLUME /config
VOLUME /files
VOLUME /lock

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["./Library", "./Library"]
COPY ["./DocIntel.Core/DocIntel.Core.csproj", "./DocIntel.Core/"]
COPY ["./DocIntel.Integrations/DocIntel.Integrations.csproj", "./DocIntel.Integrations/"]
COPY ["./DocIntel.Services.Thumbnailer/DocIntel.Services.Thumbnailer.csproj", "./DocIntel.Services.Thumbnailer/"]
RUN dotnet restore "./DocIntel.Services.Thumbnailer/DocIntel.Services.Thumbnailer.csproj"
COPY . .
WORKDIR /src/DocIntel.Services.Thumbnailer
RUN dotnet build "DocIntel.Services.Thumbnailer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocIntel.Services.Thumbnailer.csproj" -c Release -o /app/publish

FROM base AS final
RUN apt-get update && apt-get install -y poppler-utils
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocIntel.Services.Thumbnailer.dll"]