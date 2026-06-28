# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level files and source
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY src/ src/
WORKDIR /src/src

# Restore and publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime (alpine + docker-cli + keepassxc-cli for deployments)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
RUN apk add --no-cache docker-cli keepassxc
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "Api.dll"]