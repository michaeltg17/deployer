# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Api/ Api/
WORKDIR /src/Api
RUN dotnet publish -c Release -o /app --no-restore

# Stage 2: Runtime (alpine + docker-cli for deployments)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
RUN apk add --no-cache docker-cli
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "Api.dll"]
