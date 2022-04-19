FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
VOLUME /config
VOLUME /files
VOLUME /lock

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["./Library", "./Library"]
COPY ["./DocIntel.Core/DocIntel.Core.csproj", "./DocIntel.Core/"]
COPY ["./DocIntel.Integrations/DocIntel.Integrations.csproj", "./DocIntel.Integrations/"]
COPY ["./DocIntel.Services.DocumentIndexer/DocIntel.Services.DocumentIndexer.csproj", "./DocIntel.Services.DocumentIndexer/"]
RUN dotnet restore "./DocIntel.Services.DocumentIndexer/DocIntel.Services.DocumentIndexer.csproj"
COPY . .
WORKDIR /src/DocIntel.Services.DocumentIndexer
RUN dotnet build "DocIntel.Services.DocumentIndexer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocIntel.Services.DocumentIndexer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocIntel.Services.DocumentIndexer.dll"]