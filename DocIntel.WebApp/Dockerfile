FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
VOLUME /config
VOLUME /files
VOLUME /lock
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["./Library", "./Library"]
COPY ["./DocIntel.Core/DocIntel.Core.csproj", "./DocIntel.Core/"]
COPY ["./DocIntel.Integrations/DocIntel.Integrations.csproj", "./DocIntel.Integrations/"]
COPY ["./DocIntel.WebApp/DocIntel.WebApp.csproj", "./DocIntel.WebApp/"]
COPY ["./DocIntel.AdminConsole/DocIntel.AdminConsole.csproj", "./DocIntel.AdminConsole/"]
COPY ["./DocIntel.WebApp/package.json", "./DocIntel.WebApp/"]
RUN apt-get update && apt-get install curl build-essential autoconf libpng-dev python2 -y --no-install-recommends
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash -
RUN apt-get update && apt-get install -y nodejs
RUN npm install --global yarn
WORKDIR /src/DocIntel.WebApp/
RUN npm install
WORKDIR /src
RUN dotnet restore "./DocIntel.WebApp/DocIntel.WebApp.csproj"
RUN dotnet restore "./DocIntel.AdminConsole/DocIntel.AdminConsole.csproj"
COPY . .
WORKDIR /src/DocIntel.WebApp
RUN dotnet build "DocIntel.WebApp.csproj" -c Release -o /app/build
WORKDIR /src/DocIntel.AdminConsole
RUN dotnet build "DocIntel.AdminConsole.csproj" -c Release -o /cli/build

FROM build AS publish
WORKDIR /src/DocIntel.WebApp
RUN dotnet publish "DocIntel.WebApp.csproj" -c Release -o /app/publish
WORKDIR /src/DocIntel.AdminConsole
RUN dotnet publish "DocIntel.AdminConsole.csproj" -c Release -o /cli/publish

FROM base AS final
WORKDIR /cli
COPY --from=publish /cli/publish .
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80/
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocIntel.WebApp.dll"]