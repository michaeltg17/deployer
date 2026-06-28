# Deployer

.NET 10 ASP.NET Core minimal API that receives deployment requests and deploys Docker containers via `docker compose`.

## Architecture

- Receives POST `/deploy` with a JSON body containing project, environment, and tag
- Extracts `.env` files from a KeePassXC database using `keepassxc-cli`
- Pulls the image from GHCR via Docker.DotNet, then runs `docker compose up -d --force-recreate` in an isolated temp directory
- Central package management via `Directory.Packages.props`

## Structure

```
├── .dockerignore
├── .gitignore
├── AGENTS.md
├── ci-docker-build.sh              # CI docker build script
├── ci-docker.sh                    # CI docker run script
├── ci.sh                           # CI entrypoint script
├── Deployer.slnx
├── Directory.Build.props           # shared props: net10.0, nullable, implicit usings
├── Directory.Packages.props        # central package versions
├── docker-compose.ci.yml           # CI compose config
├── Dockerfile                      # multi-stage: SDK build → Alpine + docker-cli + keepassxc
├── Dockerfile.ci                   # CI runtime image with test dependencies
├── .github/workflows/ci.yml        # GH Actions: test, build, push to GHCR
├── src/                            # Api project
│   ├── Api.csproj                  # net10.0 Web SDK, Docker.DotNet dep
│   ├── appsettings.json            # minimal defaults (logging, allowed hosts)
│   ├── Program.cs                  # entrypoint, DI, endpoint registration
│   ├── Endpoints/
│   │   └── DeployEndpoint.cs       # POST /deploy: JSON parsing, delegation
│   ├── Exceptions/
│   │   ├── DeployerException.cs    # base exception
│   │   └── InvalidDeployRequestException.cs
│   ├── Extensions/
│   │   ├── ExceptionHandlerExtensions.cs  # problem+json error handler
│   │   └── TypeExtensions.cs              # helper for exception names
│   ├── Logging/
│   │   └── ILoggerExtensions.cs    # source-generated log messages
│   ├── Models/
│   │   ├── DeployRequest.cs        # POCO: project, environment, tag
│   │   └── DeployerSettings.cs     # POCO bound from config
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Services/
│   │   ├── DeploymentService.cs    # validate → extract env → pull image → compose up → cleanup
│   │   └── KeePassEnvService.cs    # keepassxc-cli attachment-export → .env files
│   └── Validation/
│       ├── DeployRequestValidator.cs    # request field validation
│       └── DeployerSettingsValidator.cs # IValidateOptions for settings
└── tests/
    ├── Tests.csproj                # xunit, Moq, Mvc.Testing
    ├── BaseTestClass.cs            # WebApplicationFactory, mock Docker client
    └── Tests.cs                    # endpoint tests: auth, validation, compose
```

## Configuration

All config binds from `DeployerSettings` via `builder.Configuration`. Required settings (`GhcrUser`, `GhcrToken`, `ImageRepo`, `KeePassDbPath`, `KeePassDbPassword`) validated at startup via `DeployerSettingsValidator`. Application fails to start if any required setting is missing.

Projects are stored under `/projects/<name>/` on disk, each containing a `docker-compose.yml`. Environment secrets (`.env`, `.env.<environment>`) are stored as KeePassXC attachments under `Projects/<name>`.

## Endpoints

| Method | Path      | Description                       |
|--------|-----------|-----------------------------------|
| POST   | `/deploy` | Triggers deployment               |

`/deploy` expects JSON body: `{ "project": "...", "environment": "...", "tag": "..." }`

Responses use `application/problem+json`. Invalid requests return 400, other errors return 500 with details hidden in production.

## Build & Run

```bash
dotnet run --project src
# or
docker build -t deployer . && docker run --rm -it -v /var/run/docker.sock:/var/run/docker.sock deployer
```

## Tests

```bash
dotnet test tests/Tests.csproj
```
