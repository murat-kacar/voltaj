param(
    [string]$OutputDirectory = "./backups",
    [string]$Container = "voltflow-postgres",
    [string]$Database = "voltflow",
    [string]$User = "postgres"
)

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$output = Join-Path $OutputDirectory "voltflow-$timestamp.sql"
docker exec $Container pg_dump -U $User -d $Database --format=custom --file=/tmp/backup.dump
docker cp "$Container`:/tmp/backup.dump" $output
Write-Output "Backup created: $output"
