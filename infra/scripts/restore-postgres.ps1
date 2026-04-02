param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,
    [Parameter(Mandatory = $true)]
    [string]$BackupFile
)

if (!(Test-Path $BackupFile)) {
    throw "Backup file '$BackupFile' was not found."
}

pg_restore --clean --if-exists --no-owner --dbname $ConnectionString $BackupFile
Write-Output "Restore completed from: $BackupFile"
