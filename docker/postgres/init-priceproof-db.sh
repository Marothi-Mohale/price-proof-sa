#!/bin/sh
set -eu

data_dir="/var/lib/postgresql/data"
db_name="${PRICEPROOF_DB_NAME:?PRICEPROOF_DB_NAME is required}"
db_user="${PRICEPROOF_DB_USER:?PRICEPROOF_DB_USER is required}"
db_password_file="${PRICEPROOF_DB_PASSWORD_FILE:?PRICEPROOF_DB_PASSWORD_FILE is required}"
db_password="$(cat "$db_password_file")"

if [ "$(id -u)" = "0" ]; then
  mkdir -p "$data_dir"
  chown -R postgres:postgres "$data_dir"
  exec su-exec postgres /usr/local/bin/init-priceproof-db.sh "$@"
fi

if [ ! -s "$data_dir/PG_VERSION" ]; then
  initdb -D "$data_dir" --username=postgres --auth-local=trust --auth-host=scram-sha-256
  pg_ctl -D "$data_dir" -o "-c listen_addresses=localhost" -w start

  psql --username=postgres --dbname=postgres \
    --set=db_name="$db_name" \
    --set=db_user="$db_user" \
    --set=db_password="$db_password" <<'SQL'
SELECT format('CREATE ROLE %I WITH LOGIN PASSWORD %L', :'db_user', :'db_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = :'db_user') \gexec

SELECT format('CREATE DATABASE %I OWNER %I', :'db_name', :'db_user')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = :'db_name') \gexec
SQL

  pg_ctl -D "$data_dir" -m fast -w stop
fi

exec postgres -D "$data_dir" -c listen_addresses='*' -c password_encryption='scram-sha-256'
