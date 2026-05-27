# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiamos todo el contenido del repositorio
COPY . ./

# 👇 SOLUCIÓN: Buscamos cualquier archivo .csproj que represente a la API de forma dinámica
RUN dotnet publish **/ControlPyme.Api.csproj -c Release -o /app/out

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ControlPyme.Api.dll"]