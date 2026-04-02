# Backup And Restore Runbook

## Purpose

This runbook covers database backup and restore for the active PriceProof SA production data plane. It assumes PostgreSQL is the system of record for transactional data, shared binary storage, and data-protection keys.

## Backup policy

- Run a full logical backup at least once every 24 hours.
- Keep rolling point-in-time or base-backup coverage outside the application host.
- Encrypt backup storage and limit operator access.
- Verify restoreability on a schedule, not only backup success.

## Manual backup

PowerShell:

```powershell
./infra/scripts/backup-postgres.ps1 -ConnectionString "<secure-connection-string>" -OutputPath "./backups"
```

Shell:

```bash
./infra/scripts/backup-postgres.sh "<secure-connection-string>" "./backups"
```

## Restore procedure

1. Pause traffic to the API.
2. Confirm which backup snapshot is being restored and why.
3. Restore into an isolated environment first when the incident timeline allows it.
4. Validate key tables: `users`, `cases`, `stored_binary_objects`, `complaint_packs`, and `data_protection_keys`.
5. Only then restore into the primary production database.

PowerShell:

```powershell
./infra/scripts/restore-postgres.ps1 -ConnectionString "<secure-connection-string>" -BackupFile "./backups/priceproof-20260402-120000.dump"
```

Shell:

```bash
./infra/scripts/restore-postgres.sh "<secure-connection-string>" "./backups/priceproof-20260402-120000.dump"
```

## Post-restore checks

- Run the API health checks.
- Verify sign-in and cookie validation still work.
- Generate and download a complaint pack.
- Confirm recent evidence files are downloadable.
- Check OTEL traces and error logs for schema or key-management issues.
