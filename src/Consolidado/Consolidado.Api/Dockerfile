FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY global.json .

# Copia csproj de cada projeto para restore em camada separada
COPY src/BuildingBlocks/FluxoCaixa.Domain.Primitives/FluxoCaixa.Domain.Primitives.csproj \
     src/BuildingBlocks/FluxoCaixa.Domain.Primitives/
COPY src/BuildingBlocks/FluxoCaixa.Application.Abstractions/FluxoCaixa.Application.Abstractions.csproj \
     src/BuildingBlocks/FluxoCaixa.Application.Abstractions/
COPY src/BuildingBlocks/FluxoCaixa.Messaging/FluxoCaixa.Messaging.csproj \
     src/BuildingBlocks/FluxoCaixa.Messaging/
COPY src/Lancamentos/Lancamentos.Domain/Lancamentos.Domain.csproj \
     src/Lancamentos/Lancamentos.Domain/
COPY src/Consolidado/Consolidado.Domain/Consolidado.Domain.csproj \
     src/Consolidado/Consolidado.Domain/
COPY src/Consolidado/Consolidado.Application/Consolidado.Application.csproj \
     src/Consolidado/Consolidado.Application/
COPY src/Consolidado/Consolidado.Infrastructure/Consolidado.Infrastructure.csproj \
     src/Consolidado/Consolidado.Infrastructure/
COPY src/Consolidado/Consolidado.Api/Consolidado.Api.csproj \
     src/Consolidado/Consolidado.Api/

RUN dotnet restore src/Consolidado/Consolidado.Api/Consolidado.Api.csproj

COPY src/ src/

RUN dotnet publish src/Consolidado/Consolidado.Api/Consolidado.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Consolidado.Api.dll"]
