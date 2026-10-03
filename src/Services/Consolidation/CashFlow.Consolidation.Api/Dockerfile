# Build multi-stage: imagem final só com o runtime ASP.NET (menor e mais segura).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet restore src/Services/Consolidation/CashFlow.Consolidation.Api/CashFlow.Consolidation.Api.csproj
RUN dotnet publish src/Services/Consolidation/CashFlow.Consolidation.Api/CashFlow.Consolidation.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "CashFlow.Consolidation.Api.dll"]
