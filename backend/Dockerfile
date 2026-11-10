# -------------------------------------------------------------
# 1. Build Stage (.NET 9 SDK)
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia e restaura dependências primeiro (otimização de cache de camadas)
COPY Finis.Api/Finis.Api.csproj Finis.Api/
RUN dotnet restore Finis.Api/Finis.Api.csproj

# Copia todo o código-fonte e publica em modo Release
COPY . .
WORKDIR /src/Finis.Api
RUN dotnet publish Finis.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# -------------------------------------------------------------
# 2. Runtime Stage (.NET 9 ASP.NET Core)
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Finis.Api.dll"]
