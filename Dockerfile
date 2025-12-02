# Usa la imagen de SDK de .NET 8 para construir
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el archivo del proyecto y restaura las dependencias
COPY ["ConsumoAPI2.Api.csproj", "./"]
RUN dotnet restore "./ConsumoAPI2.Api.csproj"

# Copia el resto de los archivos y construye
COPY . .
RUN dotnet build "ConsumoAPI2.Api.csproj" -c Release -o /app/build

# Publica la aplicación
FROM build AS publish
WORKDIR /src
RUN dotnet publish "ConsumoAPI2.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://*:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ConsumoAPI2.Api.dll"]
