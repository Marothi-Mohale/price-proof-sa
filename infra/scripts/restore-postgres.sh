#!/usr/bin/env bash
set -eu

connection_string="${1:?connection string required}"
backup_file="${2:?backup file required}"

if [ ! -f "$backup_file" ]; then
  echo "Backup file '$backup_file' was not found." >&2
  exit 1
fi

pg_restore --clean --if-exists --no-owner --dbname "$connection_string" "$backup_file"
echo "Restore completed from: $backup_file"
