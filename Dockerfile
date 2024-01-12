FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["M3U8Proxy/M3U8Proxy.csproj", "M3U8Proxy/"]
RUN dotnet restore "M3U8Proxy/M3U8Proxy.csproj"
COPY . .
WORKDIR "/src/M3U8Proxy"
RUN dotnet build "M3U8Proxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "M3U8Proxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "M3U8Proxy.dll"]
