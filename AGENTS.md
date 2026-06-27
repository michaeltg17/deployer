# Deployer

.NET 10 ASP.NET Core minimal API that receives deployment webhooks and deploys Docker containers via `docker compose`.

## Architecture

- Receives POST `/deploy` with Basic Auth + optional HMAC signature verification
- Authenticates against GHCR, pulls the image, then runs `docker compose up -d` on the target environment service
- Configured environments: `dev`, `qa`, `prod`

## Structure

```
├── .gitignore
├── AGENTS.md
├── Dockerfile              # multi-stage: SDK build → Alpine + docker-cli runtime
├── src/Deployer/
│   ├── Deployer.csproj     # net10.0 Web SDK, no external deps
│   ├── Program.cs          # entrypoint, DI, endpoint registration, env validation
│   ├── appsettings.json    # config defaults (port, auth, environments, image repo)
│   ├── Endpoints/
│   │   └── DeployEndpoint.cs  # POST /deploy: auth, HMAC, JSON parsing, delegation
│   ├── Models/
│   │   └── DeployerSettings.cs    # POCO bound from config
│   └── Services/
│       └── DeployService.cs   # GHCR login → pull image → docker compose up → prune
└── tests/                  # empty (no tests yet)
```

## Configuration

All config binds from `DeployerSettings` via `builder.Configuration`. Secrets (`BasicAuthPass`, `GhcrToken`, `ImageRepo`, `DeployBaseDir`) must be provided via environment variables or `.env`. Application exits with code 1 if required config is missing.

## Endpoints

| Method | Path                   | Description                              |
|--------|------------------------|------------------------------------------|
| GET    | `/health`              | Returns `{"status":"ok"}`                |
| POST   | `/deploy`              | Triggers deployment (Basic Auth required) |

`/deploy` expects JSON body: `{ "environment": "dev|qa|prod", "tag": "...", "commit_sha": "...", "commit_message": "..." }`

## Build & Run

```bash
dotnet run --project src/Deployer
# or
docker build -t deployer . && docker run --rm -it -v /var/run/docker.sock:/var/run/docker.sock deployer
```
