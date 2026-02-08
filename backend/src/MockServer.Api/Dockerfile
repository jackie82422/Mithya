FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MockServer.Api/MockServer.Api.csproj", "src/MockServer.Api/"]
COPY ["src/MockServer.Core/MockServer.Core.csproj", "src/MockServer.Core/"]
COPY ["src/MockServer.Infrastructure/MockServer.Infrastructure.csproj", "src/MockServer.Infrastructure/"]
RUN dotnet restore "src/MockServer.Api/MockServer.Api.csproj"
COPY . .
WORKDIR "/src/src/MockServer.Api"
RUN dotnet build "MockServer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MockServer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MockServer.Api.dll"]
