#!/usr/bin/env bash
set -Eeuo pipefail

: "${TELEMETRY_DB_PASSWORD:?TELEMETRY_DB_PASSWORD is required}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$BACKUP_DIR"

PGPASSWORD="$TELEMETRY_DB_PASSWORD" pg_dump \
  --host "${PGHOST:-localhost}" --port "${PGPORT:-5432}" \
  --username "${PGUSER:-telemetry_app}" --dbname "${PGDATABASE:-telemetry}" \
  --format=custom --file="$BACKUP_DIR/telemetry-$STAMP.dump"

find "$BACKUP_DIR" -type f -name 'telemetry-*.dump' -mtime +14 -delete
echo "backup=$BACKUP_DIR/telemetry-$STAMP.dump"
