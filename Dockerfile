FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY DocumentStorage.slnx ./
COPY src/ ./src/
COPY tests/ ./tests/

RUN dotnet restore DocumentStorage.slnx
RUN dotnet publish src/DocumentStorage.Api/DocumentStorage.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DocumentStorage.Api.dll"]
