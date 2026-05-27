# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copiamos todas las carpetas del repositorio al contenedor
COPY . .

# 2. Compilamos la API apuntando a su carpeta correspondiente
# Al estar en la raíz, preservamos el mapa para encontrar ../ControlPyme.Shared
RUN dotnet publish ControlPyme.Api/ControlPyme.Api.csproj -c Release -o /app/out

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ControlPyme.Api.dll"]