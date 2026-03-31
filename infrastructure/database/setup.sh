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

echo "Recreating database container and volume..."
if docker ps -a --format '{{.Names}}' | grep -qx 'cloth-physics-db'; then
  docker rm -f cloth-physics-db >/dev/null 2>&1 || true
fi
docker compose down --volumes --remove-orphans

echo "Starting database service..."
docker compose up -d

echo "Waiting for PostgreSQL to become ready..."
for _ in {1..60}; do
  if docker exec cloth-physics-db pg_isready -U dev -d stick_simulation >/dev/null 2>&1; then
    echo "PostgreSQL is ready."
    echo "Setup complete."
    exit 0
  fi
  sleep 1
done

echo "Timed out waiting for PostgreSQL readiness."
exit 1
