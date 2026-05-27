# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY . .
COPY ControlPyme.Shared/ControlPyme.Shared.csproj ControlPyme.Shared/


RUN dotnet publish ControlPyme.Api/ControlPyme.Api.csproj -c Release -o /app/out

COPY ControlPyme.Shared/ ControlPyme.Shared/

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ControlPyme.Api.dll"]