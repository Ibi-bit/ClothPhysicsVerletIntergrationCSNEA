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

Write-Host 'Starting database service...'
docker compose up -d

Write-Host 'Current service status:'
docker compose ps