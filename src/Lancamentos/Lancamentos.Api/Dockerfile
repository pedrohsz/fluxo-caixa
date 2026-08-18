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
COPY src/Lancamentos/Lancamentos.Application/Lancamentos.Application.csproj \
     src/Lancamentos/Lancamentos.Application/
COPY src/Lancamentos/Lancamentos.Infrastructure/Lancamentos.Infrastructure.csproj \
     src/Lancamentos/Lancamentos.Infrastructure/
COPY src/Lancamentos/Lancamentos.Api/Lancamentos.Api.csproj \
     src/Lancamentos/Lancamentos.Api/

RUN dotnet restore src/Lancamentos/Lancamentos.Api/Lancamentos.Api.csproj

COPY src/ src/

RUN dotnet publish src/Lancamentos/Lancamentos.Api/Lancamentos.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lancamentos.Api.dll"]
