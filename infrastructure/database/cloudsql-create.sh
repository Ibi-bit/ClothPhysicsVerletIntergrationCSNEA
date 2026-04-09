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

required_vars=(PROJECT_ID REGION INSTANCE_NAME DATABASE_NAME DB_USER DB_PASSWORD DB_TIER DB_VERSION)
for v in "${required_vars[@]}"; do
  if [[ -z "${!v:-}" ]]; then
    echo "Missing required variable: $v"
    exit 1
  fi
done

DB_EDITION="${DB_EDITION:-ENTERPRISE}"

if ! command -v gcloud >/dev/null 2>&1; then
  echo "gcloud CLI not found. Install Google Cloud SDK first."
  exit 1
fi

echo "Setting project to $PROJECT_ID"
gcloud config set project "$PROJECT_ID" >/dev/null

echo "Ensuring Cloud SQL Admin API is enabled"
gcloud services enable sqladmin.googleapis.com

if gcloud sql instances describe "$INSTANCE_NAME" >/dev/null 2>&1; then
  echo "Instance $INSTANCE_NAME already exists; skipping create."
else
  echo "Creating Cloud SQL instance $INSTANCE_NAME"
  gcloud sql instances create "$INSTANCE_NAME" \
    --database-version="$DB_VERSION" \
    --tier="$DB_TIER" \
    --region="$REGION" \
    --edition="$DB_EDITION"
fi

if gcloud sql databases describe "$DATABASE_NAME" --instance="$INSTANCE_NAME" >/dev/null 2>&1; then
  echo "Database $DATABASE_NAME already exists; skipping create."
else
  echo "Creating database $DATABASE_NAME"
  gcloud sql databases create "$DATABASE_NAME" --instance="$INSTANCE_NAME"
fi

if gcloud sql users list --instance="$INSTANCE_NAME" --format='value(name)' | grep -qx "$DB_USER"; then
  echo "User $DB_USER exists; updating password."
  gcloud sql users set-password "$DB_USER" --instance="$INSTANCE_NAME" --password="$DB_PASSWORD"
else
  echo "Creating user $DB_USER"
  gcloud sql users create "$DB_USER" --instance="$INSTANCE_NAME" --password="$DB_PASSWORD"
fi

echo "Cloud SQL setup complete."
echo "Next: run ./cloudsql-proxy.sh and then execute SQL init files against 127.0.0.1:${LOCAL_PORT:-5432}."
