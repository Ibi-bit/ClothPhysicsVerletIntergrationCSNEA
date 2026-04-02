Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Set-Location $PSScriptRoot

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host 'Docker CLI is not installed. Install Docker Desktop first.'
    exit 1
}

try {
    docker info | Out-Null
}
catch {
    Write-Host 'Docker daemon is not running. Start Docker Desktop, then try again.'
    exit 1
}

Write-Host 'Recreating database container and volume...'
docker compose down --volumes --remove-orphans

Write-Host 'Starting database service...'
docker compose up -d

Write-Host 'Waiting for PostgreSQL to become ready...'
for ($attempt = 1; $attempt -le 60; $attempt++) {
    try {
        docker exec cloth-physics-db pg_isready -U dev -d stick_simulation | Out-Null
        Write-Host 'PostgreSQL is ready.'
        Write-Host 'Setup complete.'
        exit 0
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

Write-Host 'Timed out waiting for PostgreSQL readiness.'
exit 1