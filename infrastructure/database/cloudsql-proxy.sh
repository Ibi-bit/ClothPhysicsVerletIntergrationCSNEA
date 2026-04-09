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

required_vars=(PROJECT_ID REGION INSTANCE_NAME)
for v in "${required_vars[@]}"; do
  if [[ -z "${!v:-}" ]]; then
    echo "Missing required variable: $v"
    exit 1
  fi
done

LOCAL_PORT="${LOCAL_PORT:-5432}"
INSTANCE_CONNECTION_NAME="${INSTANCE_CONNECTION_NAME:-${PROJECT_ID}:${REGION}:${INSTANCE_NAME}}"

echo "Starting Cloud SQL Auth Proxy for ${INSTANCE_CONNECTION_NAME} on 127.0.0.1:${LOCAL_PORT}"

auth_proxy_args=(
  "--address=0.0.0.0"
  "--port=${LOCAL_PORT}"
  "${INSTANCE_CONNECTION_NAME}"
)

if command -v cloud-sql-proxy >/dev/null 2>&1; then
  exec cloud-sql-proxy "${auth_proxy_args[@]}"
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Neither cloud-sql-proxy nor docker was found. Install one of them first."
  exit 1
fi

ADC_FILE="${GOOGLE_APPLICATION_CREDENTIALS:-$HOME/.config/gcloud/application_default_credentials.json}"
if [[ ! -f "$ADC_FILE" ]]; then
  echo "No Application Default Credentials file found at: $ADC_FILE"
  echo "Run: gcloud auth application-default login"
  exit 1
fi

exec docker run --rm -p "${LOCAL_PORT}:5432" \
  -e GOOGLE_APPLICATION_CREDENTIALS=/config/adc.json \
  -v "$ADC_FILE:/config/adc.json:ro" \
  gcr.io/cloud-sql-connectors/cloud-sql-proxy:2.14.3 \
  "${auth_proxy_args[@]}"
