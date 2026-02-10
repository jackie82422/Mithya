FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Mithya.Api/Mithya.Api.csproj", "src/Mithya.Api/"]
COPY ["src/Mithya.Core/Mithya.Core.csproj", "src/Mithya.Core/"]
COPY ["src/Mithya.Infrastructure/Mithya.Infrastructure.csproj", "src/Mithya.Infrastructure/"]
RUN dotnet restore "src/Mithya.Api/Mithya.Api.csproj"
COPY . .
WORKDIR "/src/src/Mithya.Api"
RUN dotnet build "Mithya.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Mithya.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mithya.Api.dll"]
