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
COPY ["./DocIntel.Services.ContinuousIndexing/DocIntel.Services.ContinuousIndexing.csproj", "./DocIntel.Services.ContinuousIndexing/"]
RUN dotnet restore "./DocIntel.Services.ContinuousIndexing/DocIntel.Services.ContinuousIndexing.csproj"
COPY . .
WORKDIR /src/DocIntel.Services.ContinuousIndexing
RUN dotnet build "DocIntel.Services.ContinuousIndexing.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocIntel.Services.ContinuousIndexing.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocIntel.Services.ContinuousIndexing.dll"]