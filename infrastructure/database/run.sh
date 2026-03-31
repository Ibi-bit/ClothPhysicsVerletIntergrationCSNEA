#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker CLI is not installed. Install Docker Desktop first."
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker daemon is not running. Start Docker Desktop, then try again."
  exit 1
fi

echo "Starting database service..."
used_existing_container=false
if docker ps -a --format '{{.Names}}' | grep -qx 'cloth-physics-db'; then
  docker start cloth-physics-db >/dev/null 2>&1 || true
  used_existing_container=true
else
  docker compose up -d
fi

echo "Current service status:"
if [ "$used_existing_container" = true ]; then
  docker ps -a --filter name='^/cloth-physics-db$' --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
else
  docker compose ps
fi
