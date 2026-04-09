#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CALLER_DIR="$(pwd)"

ENV_FILE_INPUT="${1:-cloudsql.env}"
if [[ "$ENV_FILE_INPUT" = /* ]]; then
  ENV_FILE="$ENV_FILE_INPUT"
elif [[ -f "$CALLER_DIR/$ENV_FILE_INPUT" ]]; then
  ENV_FILE="$CALLER_DIR/$ENV_FILE_INPUT"
else
  ENV_FILE="$SCRIPT_DIR/$ENV_FILE_INPUT"
fi

cd "$SCRIPT_DIR"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE. Copy cloudsql.env.example to cloudsql.env and fill values."
  exit 1
fi

# shellcheck disable=SC1090
source "$ENV_FILE"

required_vars=(DATABASE_NAME DB_USER DB_PASSWORD)
for v in "${required_vars[@]}"; do
  if [[ -z "${!v:-}" ]]; then
    echo "Missing required variable: $v"
    exit 1
  fi
done

LOCAL_PORT="${LOCAL_PORT:-5432}"
PGHOST="127.0.0.1"

run_psql() {
  local sql_file="$1"
  local psql_bin=""
  if command -v psql >/dev/null 2>&1; then
    psql_bin="$(command -v psql)"
  elif [[ -x /opt/homebrew/opt/libpq/bin/psql ]]; then
    psql_bin="/opt/homebrew/opt/libpq/bin/psql"
  fi

  if [[ -n "$psql_bin" ]]; then
    PGPASSWORD="$DB_PASSWORD" "$psql_bin" \
      -h "$PGHOST" -p "$LOCAL_PORT" -U "$DB_USER" -d "$DATABASE_NAME" \
      -v ON_ERROR_STOP=1 -f "$sql_file"
    return
  fi

  if ! command -v docker >/dev/null 2>&1; then
    echo "Neither psql nor docker is available to run SQL initialization."
    exit 1
  fi

  docker run --rm -v "$SCRIPT_DIR/sql:/sql:ro" -e PGPASSWORD="$DB_PASSWORD" \
    postgres:16-alpine \
    psql -h host.docker.internal -p "$LOCAL_PORT" -U "$DB_USER" -d "$DATABASE_NAME" -v ON_ERROR_STOP=1 -f "/sql/$(basename "$sql_file")"
}

echo "Applying schema and seed data to $DATABASE_NAME on ${PGHOST}:${LOCAL_PORT}"
run_psql "$SCRIPT_DIR/sql/StructuresDB.sql"
run_psql "$SCRIPT_DIR/sql/SampleData.sql"

echo "Cloud SQL initialization complete."
