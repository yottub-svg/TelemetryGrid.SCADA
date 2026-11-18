#!/usr/bin/env bash
set -Eeuo pipefail

: "${BACKUP_FILE:?BACKUP_FILE is required}"
: "${TELEMETRY_DB_PASSWORD:?TELEMETRY_DB_PASSWORD is required}"
PGPASSWORD="$TELEMETRY_DB_PASSWORD" pg_restore \
  --clean --if-exists --no-owner \
  --host "${PGHOST:-localhost}" --port "${PGPORT:-5432}" \
  --username "${PGUSER:-telemetry_app}" --dbname "${PGDATABASE:-telemetry}" \
  "$BACKUP_FILE"
echo "restore=ok file=$BACKUP_FILE"
