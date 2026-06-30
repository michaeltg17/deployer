# Deployer

.NET 10 ASP.NET Core minimal API that receives deployment requests and deploys Docker containers via `docker compose`.

## Architecture

- Receives POST `/` with a JSON body containing project, environment, and tag
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
│   │   └── DeployEndpoint.cs       # POST /: JSON parsing, delegation
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
│   │   ├── IProcessRunner.cs       # interface for process execution
│   │   ├── KeePassEnvService.cs    # keepassxc-cli attachment-export → .env files
│   │   └── ProcessRunner.cs        # real implementation via ProcessStartInfo
│   └── Validation/
│       ├── DeployRequestValidator.cs    # request field validation
│       └── DeployerSettingsValidator.cs # IValidateOptions for settings
└── tests/
    ├── Tests.csproj                # xunit, Moq, Docker.DotNet, Mvc.Testing
    ├── BaseTestClass.cs            # WebApplicationFactory, mock Docker client & IProcessRunner
    ├── Tests.cs                    # 7 mocked tests: validation, missing compose, success path
    ├── RealDockerTestClass.cs      # WebApplicationFactory for real Docker tests
    ├── RealDockerTests.cs          # 2 real Docker tests: deploys actual containers
    ├── DelegatingProcessRunner.cs  # IProcessRunner wrapper: blocks keepassxc-cli, delegates
    ├── test.kdbx                   # KeePassXC 2 binary test database (used by RealDockerTests)
    └── projects/
        └── test-project/
            └── docker-compose.yml  # Deploys ghcr.io/michaeltg17/deployer:${TAG}
```

## Configuration

All config binds from `DeployerSettings` via `builder.Configuration`. Required settings (`ImageRepo`, `KeePassDbPath`, `KeePassDbPassword`) validated at startup via `DeployerSettingsValidator`. `ProjectsDir` is optional, defaults to `/projects`. Application fails to start if any required setting is missing.

Projects are stored under `/projects/<name>/` on disk, each containing a `docker-compose.yml`. Environment secrets (`.env`, `.env.<environment>`) are stored as KeePassXC attachments under `Projects/<name>`.

## Endpoints

| Method | Path      | Description                       |
|--------|-----------|-----------------------------------|
| POST   | `/`       | Triggers deployment               |

`/` expects JSON body: `{ "project": "...", "environment": "...", "tag": "..." }`

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

### Test Files

```
└── tests/
    ├── Tests.csproj                # xunit, Moq, Docker.DotNet, Mvc.Testing
    ├── BaseTestClass.cs            # WebApplicationFactory, mock Docker client & IProcessRunner
    ├── Tests.cs                    # 7 mocked tests: validation, missing compose, success path
    ├── RealDockerTestClass.cs      # WebApplicationFactory for real Docker tests
    ├── RealDockerTests.cs          # 2 real Docker tests: deploys actual containers
    ├── DelegatingProcessRunner.cs  # IProcessRunner wrapper: blocks keepassxc-cli, delegates
    ├── test.kdbx                   # KeePassXC 2 binary test database (used by RealDockerTests)
    └── projects/
        └── test-project/
            └── docker-compose.yml  # Deploys ghcr.io/michaeltg17/deployer:${TAG}
```

### Mocked Tests (Tests.cs)

7 tests using `BaseTestClass` with Moq — never call real Docker or KeePass. Validates request parsing, missing fields, missing compose file, and the success path (compose file created on disk, all deps mocked).

### Real Docker Tests (RealDockerTests.cs)

2 tests using `RealDockerTestClass` — deploy real containers against Docker daemon:

| Test | Description |
|------|-------------|
| `ValidRequest_Latest_Returns200_AndStartsContainer` | Deploys `test-project` with `tag: "latest"`, verifies 200 + `docker inspect --format '{{.Config.Image}}'` confirms `ghcr.io/michaeltg17/deployer:latest` |
| `ValidRequest_CommitTag_Returns200_AndStartsContainer` | Deploys `test-project` with `tag: "21ec91a"`, verifies 200 + correct image tag |

`RealDockerTestClass` points `TestProjectsDir` at `tests/projects/` (the pre-existing test fixture compose files), uses real Docker.DotNet client, and replaces `IProcessRunner` with `DelegatingProcessRunner` (`keepassxc-cli` blocked, `docker` delegated to real `ProcessRunner`). `KeePassEnvService` tolerates failed CLI calls (empty `.env` is acceptable for tests).

### KeePass CLI Mocking

There are two levels of KeePass mocking:

- **BaseTestClass** (mocked tests): Full Moq mock — every `keepassxc-cli` and `docker` call returns `ExitCode = 0`, with `"ENV_VAR=value\n"` as stdout for `keepassxc-cli`
- **RealDockerTests**: `DelegatingProcessRunner` short-circuits `keepassxc-cli` with `ExitCode = 1` and stderr `"keepassxc-cli not found"`, delegates all other commands to real `ProcessRunner`

## Coding Conventions

- **No `Async` suffix** — don't name methods `RunAsync`, do `Run`. The `async` modifier on the method body is sufficient.
- **Models over tuples** — use a proper response class instead of `Task<(int, string, string)>`
