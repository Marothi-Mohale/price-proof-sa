# Security Review Checklist

Use this checklist before production releases, incident-response follow-ups, or any change that touches uploads, OCR, complaint packs, auth, or admin reporting.

## Authentication and session handling
- Confirm sign-in and sign-up flows still validate and sanitize all user input.
- Confirm session token generation and validation still reject tampered tokens.
- Confirm admin-only routes require verified admin access.
- Confirm no privileged behavior is controlled only by client-side state.

## Authorization
- Review any new controller or endpoint for missing access checks.
- Confirm admin reporting and risk overview routes are still protected.
- Confirm case and complaint-pack actions do not expose unrelated records by identifier guessing.

## Request hardening
- Confirm `X-Correlation-ID` is returned on every request path, including failures.
- Confirm audit logs capture important state-changing user actions.
- Confirm rate limiting still covers auth, uploads, OCR, and admin routes.
- Confirm problem responses do not leak provider secrets, connection strings, or stack traces outside safe environments.

## File uploads
- Confirm max upload size is enforced both operationally and in tests.
- Confirm only supported content types and extensions are accepted.
- Confirm file name and category sanitization still prevent path traversal or unsafe characters.
- Confirm download paths are resolved safely beneath the storage root.

## OCR providers and external calls
- Confirm OCR providers remain isolated behind `IOcrProvider`.
- Confirm retries only target transient failures and do not spin indefinitely.
- Confirm provider failures return safe user-facing messages.
- Confirm any new OCR metadata stored in the database is bounded and sanitized.

## Data handling
- Confirm user-entered text is trimmed, sanitized, and length-bounded before persistence.
- Confirm audit payloads and OCR payload metadata remain size-bounded.
- Confirm complaint summaries stay factual, neutral, and evidence-based.
- Confirm weak evidence is described as weak instead of overstated.

## Persistence and migrations
- Confirm EF migrations match the active model.
- Confirm new columns that store sensitive or high-volume payloads have explicit size limits.
- Confirm soft-delete and query-filter behavior still match the intended data lifecycle.

## Logging and observability
- Confirm Serilog output includes correlation context.
- Confirm health checks remain lightweight and safe to expose internally.
- Confirm operational logs do not include secrets, raw credentials, or unnecessary PII.

## Testing gates
- Run unit tests for discrepancy detection, risk scoring, and OCR normalization.
- Run integration tests for the main case flow, uploads, OCR, complaint packs, and risk endpoints.
- Confirm at least one test covers correlation ID propagation into audit logs.
- Confirm at least one test covers rejected uploads and oversized payloads.

## Operational review
- Review local override files and confirm they are ignored by git.
- Review Docker and compose changes for unintended port or storage exposure.
- Review dependency updates for OCR, PDF, auth, and database packages.
- Confirm backup, retention, and incident-response expectations are understood before deployment.
