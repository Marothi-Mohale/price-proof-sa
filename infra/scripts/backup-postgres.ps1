param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$targetFile = Join-Path $OutputPath "priceproof-$timestamp.dump"

if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

pg_dump --format=custom --file $targetFile --dbname $ConnectionString
Write-Output "Created backup: $targetFile"
