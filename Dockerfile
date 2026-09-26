# Despliegue en Render como Web Service usando Docker.
# Imágenes oficiales de .NET 10: SDK para compilar, ASP.NET Runtime para ejecutar.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar primero para aprovechar la caché de capas de Docker.
COPY ["GestionCreditos.csproj", "./"]
RUN dotnet restore "GestionCreditos.csproj"

COPY . .
RUN dotnet publish "GestionCreditos.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render inyecta el puerto en la variable PORT. Se expande en tiempo de
# ejecución mediante shell (no asumir expansión dentro de una variable ENV).
EXPOSE 8080
CMD ["sh", "-c", "dotnet GestionCreditos.dll --urls http://0.0.0.0:${PORT:-8080}"]
