# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiamos toda la solución (incluyendo Api y Shared)
COPY . ./

# 👇 CAMBIO 1: Entramos específicamente a la carpeta de la API antes de publicar
WORKDIR /app/ControlPyme.Api
RUN dotnet publish -c Release -o /app/out

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 👇 CAMBIO 2: Nos aseguramos de jalar el "out" desde la raíz global del build
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ControlPyme.Api.dll"]