param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    [string]$Container = "voltflow-postgres",
    [string]$Database = "voltflow",
    [string]$User = "postgres"
)

if(-not (Test-Path $BackupFile)) { throw "Backup file not found: $BackupFile" }
$remote = "/tmp/voltflow-restore.dump"
docker cp $BackupFile "$Container`:$remote"
docker exec $Container pg_restore -U $User -d $Database --clean --if-exists --no-owner $remote
Write-Output "Restore completed from: $BackupFile"
