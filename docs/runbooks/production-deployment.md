# Production Deployment Runbook

## Purpose

This runbook covers the production deployment path for PriceProof SA using the repository workflow and the hardened container runtime.

## Deployment model

- CI validation runs through `.github/workflows/ci.yml`.
- Production deployment runs through `.github/workflows/deploy-production.yml`.
- The deployment workflow builds immutable API and frontend images, prepares the secure deployment bundle, copies it to the target host, and applies the Docker Compose stack.
- Production secrets are supplied out of band through GitHub environment secrets and mounted secret files on the host.

## Required secure inputs

- database connection and application secrets in `priceproof.api.json`
- PostgreSQL password secret file
- SSH private key for the deployment host
- deployment host and target path

Do not place these values in tracked repository files.

## Standard deployment procedure

1. Confirm `main` is green in CI.
2. Confirm the production secret files and GitHub environment secrets are up to date.
3. Trigger `.github/workflows/deploy-production.yml`.
4. Watch the workflow through image build, bundle copy, and remote `docker compose` apply.
5. After deployment, verify:
   - `/health/live`
   - `/health/ready`
   - sign-in
   - case creation
   - complaint-pack generation

## Rollback approach

1. Identify the last known good image revision.
2. Re-run deployment using that revision's images or restore the prior deployment bundle.
3. Recheck health, authentication, evidence download, and complaint-pack generation.
4. If rollback is related to data corruption, stop here and follow the backup and restore runbook before restoring traffic.

## Post-deployment checks

- Confirm OpenTelemetry traces are arriving at the collector.
- Confirm Prometheus scrape targets remain healthy.
- Review correlated API logs for migration, secret-loading, or cookie-protection errors.
- Verify new uploads and complaint packs are readable from shared storage.
