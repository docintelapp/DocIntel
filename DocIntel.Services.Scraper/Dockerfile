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
COPY ["./DocIntel.Services.Scraper/DocIntel.Services.Scraper.csproj", "./DocIntel.Services.Scraper/"]
RUN dotnet restore "./DocIntel.Services.Scraper/DocIntel.Services.Scraper.csproj"
COPY . .
WORKDIR /src/DocIntel.Services.Scraper
RUN dotnet build "DocIntel.Services.Scraper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocIntel.Services.Scraper.csproj" -c Release -o /app/publish

FROM base AS final
RUN apt-get update && apt-get install -y gconf-service libgbm1 libasound2 libatk1.0-0 libc6 libcairo2 libcups2 libdbus-1-3 libexpat1 libfontconfig1 libgcc1 libgconf-2-4 libgdk-pixbuf2.0-0 libglib2.0-0 libgtk-3-0 libnspr4 libpango-1.0-0 libpangocairo-1.0-0 libstdc++6 libx11-6 libx11-xcb1 libxcb1 libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 libxrender1 libxss1 libxtst6 ca-certificates fonts-liberation libappindicator1 libnss3 lsb-release xdg-utils wget
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocIntel.Services.Scraper.dll"]