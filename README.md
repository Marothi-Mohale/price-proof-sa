# PriceProof SA

PriceProof SA is a clean-architecture ASP.NET Core 8 and Next.js 14 platform for capturing price discrepancies, analyzing quoted-versus-charged outcomes, extracting receipt data with OCR, generating complaint packs, and surfacing merchant risk patterns over time.

## Repository structure
- `src/PriceProof.Api`: ASP.NET Core Web API host, middleware, controllers, health checks, rate limiting, and structured logging.
- `src/PriceProof.Application`: use cases, DTOs, validators, service contracts, audit orchestration, and application rules.
- `src/PriceProof.Domain`: entities, enums, and domain services for discrepancy analysis, complaint narrative generation, and risk scoring.
- `src/PriceProof.Infrastructure`: EF Core persistence, PostgreSQL integration, OCR providers, PDF generation, file storage, and session token services.
- `tests/PriceProof.UnitTests`: domain and infrastructure-focused unit tests.
- `tests/PriceProof.IntegrationTests`: API flow, upload, OCR, risk, and complaint pack integration coverage.
- `src/frontend`: Next.js 14 frontend with TypeScript, Tailwind CSS, zod validation, and the mobile-first case workflow.

## Architecture
The solution follows a clean-architecture split:
- `Api` depends on `Application` and `Infrastructure`.
- `Application` depends on `Domain` and abstractions, not framework-specific providers.
- `Domain` stays independent from transport, storage, and external services.
- `Infrastructure` implements persistence, OCR, document generation, uploads, and request-context access.

Cross-cutting hardening that now applies throughout the API:
- request correlation IDs via `X-Correlation-ID`
- centralized audit log writing tied to the active request
- ASP.NET Core rate limiting policies for auth, uploads, OCR, and admin routes
- max upload size enforcement plus file type/content type validation
- safe OCR fallback and retry behavior with provider isolation
- structured Serilog request logging and health checks

## Local setup
### Prerequisites
- .NET 8 SDK
- Node.js 20+
- PostgreSQL 16+ or Docker Desktop

### Option 1: Docker
From the repository root:

```powershell
docker compose up --build
```

This starts PostgreSQL and the API. Run the frontend separately if you want the Next.js development server experience.

### Option 2: Host run
1. Start PostgreSQL locally.
2. If you need machine-specific settings, keep them in a local ignored override file under `src/PriceProof.Api`.
3. Run the API:

```powershell
dotnet run --project .\src\PriceProof.Api\PriceProof.Api.csproj
```

4. Run the frontend:

```powershell
Set-Location .\src\frontend
npm.cmd install
npm.cmd run dev
```

## Verification
### Backend
```powershell
dotnet build .\src\PriceProof.Api\PriceProof.Api.csproj
dotnet test .\tests\PriceProof.UnitTests\PriceProof.UnitTests.csproj
dotnet test .\tests\PriceProof.IntegrationTests\PriceProof.IntegrationTests.csproj
```

### Frontend
```powershell
Set-Location .\src\frontend
npm.cmd run build
npm.cmd test -- --runInBand
```

## Main API flow
1. Create a case with `POST /cases`.
2. Upload evidence with `POST /uploads`.
3. Attach quoted-price evidence with `POST /price-captures`.
4. Record the charged amount with `POST /payment-records`.
5. Attach the receipt with `POST /receipt-records`.
6. Run OCR with `POST /receipt-records/{id}/run-ocr`.
7. Analyze the discrepancy with `POST /cases/{id}/analyze`.
8. Generate the complaint pack with `POST /cases/{id}/generate-complaint-pack`.
9. Download the generated pack with `GET /complaint-packs/{id}/download`.

## Threat model
Primary risks considered in this codebase:
- untrusted file uploads attempting oversized payloads, unsupported types, or path traversal
- abuse of auth, OCR, upload, or admin endpoints through request flooding
- accidental leakage of OCR/provider failures or internal exception details to end users
- stale or unverifiable evidence being presented as stronger than it is
- insufficient traceability for dispute evidence and sensitive actions
- stored user text containing unsafe control characters or malformed payload content

Current mitigations:
- request and response correlation IDs
- audit records for sign-in, sign-up, uploads, evidence creation, OCR, analysis, and complaint-pack generation
- file name/category sanitization, allowed file lists, and max upload size checks
- safe OCR fallback and retry handling with provider-specific implementations kept isolated
- conservative discrepancy classification and complaint wording
- admin access enforcement for operational dashboards and risk overview data

## Security review
Use [security-review-checklist.md](/c:/Users/ASUS/Downloads/priceProof/docs/security-review-checklist.md) before releases, production rollouts, or major backend changes.

## Notes
- Machine-specific credentials and secrets should stay outside source control.
- Generated storage, logs, and local override files are ignored by the repository.
