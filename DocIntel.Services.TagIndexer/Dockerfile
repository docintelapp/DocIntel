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
COPY ["./DocIntel.Services.TagIndexer/DocIntel.Services.TagIndexer.csproj", "./DocIntel.Services.TagIndexer/"]
RUN dotnet restore "./DocIntel.Services.TagIndexer/DocIntel.Services.TagIndexer.csproj"
COPY . .
WORKDIR /src/DocIntel.Services.TagIndexer
RUN dotnet build "DocIntel.Services.TagIndexer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocIntel.Services.TagIndexer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocIntel.Services.TagIndexer.dll"]