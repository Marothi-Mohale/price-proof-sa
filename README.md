# PriceProof SA

PriceProof SA is a clean-architecture ASP.NET Core 8 and Next.js 14 application for documenting price discrepancies, analyzing quoted-versus-charged amounts, and generating complaint-ready evidence packs.

## What is in the repo
- ASP.NET Core 8 Web API with `Api`, `Application`, `Domain`, and `Infrastructure` layers.
- Next.js 14 frontend with TypeScript, Tailwind CSS, zod validation, and a mobile-first case workflow.
- PostgreSQL persistence, OCR orchestration, discrepancy analysis, complaint-pack generation, structured logging, health checks, and test projects.

## Key user flows
- Sign in or sign up with the lightweight local session flow.
- Create a case and attach quoted-price evidence.
- Record the final charge and upload receipt evidence.
- Run OCR and discrepancy analysis.
- Generate and download a complaint pack PDF.

## Local run
### Docker
From the repository root:

```powershell
docker compose up --build
```

### Host run
1. Start PostgreSQL locally.
2. Run the API from the repository root:

```powershell
dotnet run --project .\src\PriceProof.Api\PriceProof.Api.csproj
```

3. Run the frontend:

```powershell
Set-Location .\src\frontend
npm.cmd install
npm.cmd run dev
```

## Verification
### Backend
```powershell
dotnet build .\src\PriceProof.Api\PriceProof.Api.csproj
dotnet test .\PriceProof.sln
```

### Frontend
```powershell
Set-Location .\src\frontend
npm.cmd run build
npm.cmd test -- --runInBand
```

## Repository note
Operational secrets and machine-specific overrides should remain local and out of source control.
