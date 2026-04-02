# PriceProof SA Implementation Plan

## Vision
PriceProof SA is a mobile-first consumer protection app for South African shoppers. The MVP helps users preserve a quoted or displayed price before payment, compare it to the final charged amount, classify suspicious differences, and generate a structured evidence pack suitable for complaints.

## Product Scope
### In scope for the MVP
- Account creation and sign-in through an auth abstraction with a development provider.
- New discrepancy case creation.
- Price capture by manual entry, image upload, and media metadata capture for audio/video evidence.
- Final payment capture by manual amount, receipt upload, and bank notification manual entry.
- OCR pipeline with provider fallback abstraction.
- Discrepancy analysis and mismatch classification.
- Merchant and branch tracking with merchant risk history.
- Complaint pack generation with downloadable PDF evidence bundle.
- Case history and merchant history views.
- Audit logging, rate limiting, health checks, structured logging, and Dockerized local development.

### Out of scope for this MVP
- Full production Clerk or Supabase tenant setup.
- Real merchant QR lock flow beyond a stub endpoint and UI affordance.
- Advanced fraud detection, geofencing, and trust scoring.
- Native mobile applications.

## Architectural Approach
### Backend
- Modular monolith with clean architecture boundaries.
- Layers:
  - `Api`: HTTP endpoints, filters, middleware, auth wiring, rate limiting.
  - `Application`: use cases, validators, orchestration, DTOs, background job requests.
  - `Domain`: aggregates, enums, value objects, domain services, core invariants.
  - `Infrastructure`: EF Core, PostgreSQL mappings, shared binary storage, OCR providers, PDF generation, background jobs, logging adapters.
- CQRS-lite with pragmatic services instead of ceremony-heavy mediator usage.
- EF Core used directly where it simplifies reads; repositories only for write-heavy or polymorphic boundaries.

### Frontend
- Next.js 14 App Router + TypeScript + Tailwind CSS.
- Mobile-first design with a lightweight dashboard shell, typed API client, and resilient upload UX.
- Server components for initial data hydration where practical; client components for capture flows and uploads.

### Data and integrations
- PostgreSQL for transactional data.
- Shared database-backed binary storage for uploads, complaint packs, and multi-instance-safe evidence access.
- Azure Document Intelligence primary OCR provider with Google Vision fallback and mock provider for local runs without cloud credentials.
- Hangfire for background jobs and retryable OCR / complaint-pack workflows.

## Security and Compliance
- POPIA-conscious data handling: minimize PII, separate auth identity from case evidence where practical, redact bank notification content before persistence.
- Secure file upload with extension and content-type validation.
- Anti-tampering audit trail with append-only audit records and hash chaining.
- Role support for `User` and `Admin`.
- Endpoint rate limiting for report submissions and media uploads.

## Delivery Phases
### Phase 1: Foundations
- Scaffold backend solution and frontend app.
- Add shared configuration, Docker compose, PostgreSQL, local blob emulator/fallback, and base docs.
- Establish auth abstraction, health checks, logging, global error handling, and testing projects.

### Phase 2: Case Lifecycle
- Implement merchants, branches, users, discrepancy cases, price captures, payment records, and receipt records.
- Build create-account, sign-in, create-case, and case-history flows.
- Add validators and domain invariants.

### Phase 3: Evidence Capture and OCR
- Add secure uploads and media metadata handling.
- Implement OCR orchestration and retries.
- Parse receipt totals and reconcile them with final charged amounts.

### Phase 4: Analysis and Complaint Pack
- Implement discrepancy analyzer and classification engine.
- Compute merchant risk scores from repeat reports.
- Generate complaint summary and PDF evidence pack.

### Phase 5: Hardening
- Add structured logs, audit logs, admin visibility, error states, empty states, and seed/demo data.
- Expand tests and verify the local Docker workflow.

## Testing Strategy
- Domain and application unit tests for classification, risk scoring, validation, and state transitions.
- API integration tests for happy-path flows and validation failures.
- Frontend component tests for capture forms, history lists, and result views.

## Acceptance Criteria
- A user can sign in with the MVP auth flow and create a case.
- A user can record a quoted/displayed amount before paying.
- A user can submit a final paid amount or upload a receipt.
- The system computes a difference and classifies the mismatch.
- A PDF complaint pack is downloadable for completed cases.
- Merchant history and repeated reports influence a visible merchant risk score.
- The system runs locally with Docker and keeps machine-specific operational configuration outside source control.
