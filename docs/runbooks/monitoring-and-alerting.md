# Monitoring And Alerting Runbook

## Signals to collect

- API request rate, latency, and 5xx responses
- OCR provider failures and timeouts
- complaint-pack generation failures
- database health and migration failures
- lockout, reset, and verification events from audit logs

## Telemetry path

- The API emits traces and metrics through OpenTelemetry OTLP.
- The repository includes an OTEL collector config in `infra/monitoring/otel-collector-config.yaml`.
- Prometheus can scrape the collector using `infra/monitoring/prometheus.yml`.
- Alert rules live in `infra/monitoring/alerts/priceproof-alerts.yml`.

## Minimum production alerts

- API or collector unavailable
- sustained 5xx error rate
- elevated p95 latency
- OCR failure spike
- repeated database connection failures

## Incident triage

1. Check `/health/ready` and `/health/live`.
2. Inspect the most recent correlated error logs by `X-Correlation-ID`.
3. Review OTEL traces for the failing route or provider dependency.
4. Confirm database connectivity and pending migrations.
5. For auth incidents, inspect recent `AuditLog` entries for lockout, verification, and password-reset actions.
