FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
VOLUME /config
VOLUME /files
VOLUME /lock

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["./Library", "./Library"]
COPY ["./DocIntel.Core/DocIntel.Core.csproj", "./DocIntel.Core/"]
COPY ["./DocIntel.Services.Cron/DocIntel.Services.Cron.csproj", "./DocIntel.Services.Cron/"]
RUN dotnet restore "./DocIntel.Services.Cron/DocIntel.Services.Cron.csproj"
COPY . .
WORKDIR /src/DocIntel.Services.Cron
RUN dotnet build "DocIntel.Services.Cron.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocIntel.Services.Cron.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocIntel.Services.Cron.dll"]