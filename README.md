# PriceProof SA

PriceProof SA is a production-minded MVP for documenting quoted prices, detecting likely unlawful card surcharges, and generating evidence packs for South African consumer complaints.

## What is implemented
- Clean-architecture ASP.NET Core 8 modular monolith with `Api`, `Application`, `Domain`, and `Infrastructure` layers.
- Next.js 14 + TypeScript + Tailwind mobile-first frontend shell.
- Demo auth/session flow with role support for `User` and `Admin`.
- Case creation, manual and media price capture, manual payment capture, receipt upload, OCR orchestration, discrepancy classification, merchant risk scoring, and PDF complaint pack generation.
- PostgreSQL persistence, secure local/Azure-abstracted file storage, Hangfire background OCR jobs, structured logging, health checks, rate limiting, and audit hashing.
- Backend and frontend test scaffolds covering classification, merchant risk scoring, receipt parsing, and basic UI helpers.

## Architecture notes
- `src/backend/src/PriceProofSA.Domain` keeps the rule-heavy entities and pricing heuristics independent from transport or persistence code.
- `src/backend/src/PriceProofSA.Application` contains the use-case orchestration, validation, DTOs, and service contracts so the API stays thin.
- `src/backend/src/PriceProofSA.Infrastructure` handles EF Core, storage, OCR providers, Hangfire, auth resolution, PDF generation, and health checks.
- `src/frontend` is intentionally client-heavy for the MVP so session storage and evidence uploads stay simple; the API client remains typed and isolated for later server-component migration.
- Complaint packs are generated through a lightweight PDF builder instead of a heavy reporting dependency, which keeps the MVP portable and easy to audit.

## Folder guide
- `docs/implementation-plan.md`
- `docs/project-structure.md`
- `src/backend`
- `src/frontend`
- `infra/docker-compose.yml`
- `infra/env/backend.env.example`
- `infra/env/frontend.env.example`

## Environment variables
### Backend
```env
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=priceproofsa;Username=priceproof;Password=priceproof
Cors__AllowedOrigins__0=http://localhost:3000
Auth__AdminEmails__0=admin@priceproof.local
Storage__Provider=Local
Storage__LocalRootPath=storage
Storage__AzureBlobConnectionString=
Storage__AzureContainerPrefix=priceproof
Ocr__AzureDocumentIntelligence__Endpoint=
Ocr__AzureDocumentIntelligence__ApiKey=
Ocr__AzureDocumentIntelligence__ModelId=prebuilt-read
Ocr__GoogleVision__ApiKey=
```

### Frontend
```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:8081
```

## Exact setup commands
### Option 1: Docker Compose
From `C:\Users\ASUS\Downloads\priceProof`:

```powershell
docker compose -f .\infra\docker-compose.yml up --build
```

Then open `http://localhost:3000`.

### Option 2: Run backend and frontend on the host
1. Start PostgreSQL only:

```powershell
docker compose -f .\infra\docker-compose.yml up -d postgres
```

2. Start the backend:

```powershell
Set-Location .\src\backend
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=priceproofsa;Username=priceproof;Password=priceproof'
$env:Cors__AllowedOrigins__0='http://localhost:3000'
$env:Storage__Provider='Local'
$env:Storage__LocalRootPath='storage'
dotnet run --project .\src\PriceProofSA.Api\PriceProofSA.Api.csproj
```

3. In a second PowerShell window, start the frontend:

```powershell
Set-Location C:\Users\ASUS\Downloads\priceProof\src\frontend
$env:NEXT_PUBLIC_API_BASE_URL='http://localhost:8081'
npm.cmd install
npm.cmd run dev
```

Then open `http://localhost:3000`.

## Demo workflow
1. Sign up with a local email on the landing screen.
2. Create a case with merchant and basket details.
3. Capture the quoted price manually or with pre-payment media evidence.
4. Record the final payment or upload a receipt.
5. Refresh the case if OCR is still processing.
6. Generate and download the complaint pack PDF.

## Local OCR note
If no cloud OCR credentials are configured, the mock OCR provider keeps the flow usable. For quick local demos it can infer a total from file names like `receipt-59.99.jpg`.

## Tests
### Backend
```powershell
Set-Location C:\Users\ASUS\Downloads\priceProof\src\backend
dotnet test .\PriceProofSA.slnx
```

### Frontend
```powershell
Set-Location C:\Users\ASUS\Downloads\priceProof\src\frontend
npm.cmd test
```

## Next hardening steps
- Replace demo auth with a live Clerk or Supabase adapter.
- Add EF Core migrations and a persistent Hangfire backing store.
- Expand OCR extraction confidence handling and receipt-field provenance.
- Add richer admin analytics and moderation tooling for merchant reports.
