#!/bin/sh
set -eu

data_dir="/var/lib/postgresql/data"

if [ "$(id -u)" = "0" ]; then
  mkdir -p "$data_dir"
  chown -R postgres:postgres "$data_dir"
  exec su-exec postgres /usr/local/bin/init-priceproof-db.sh "$@"
fi

if [ ! -s "$data_dir/PG_VERSION" ]; then
  initdb -D "$data_dir" --username=postgres --auth=trust
  if ! grep -q "host all all all trust" "$data_dir/pg_hba.conf"; then
    printf "\nhost all all all trust\n" >> "$data_dir/pg_hba.conf"
  fi
  pg_ctl -D "$data_dir" -o "-c listen_addresses=localhost" -w start

  psql --username=postgres --dbname=postgres <<'SQL'
CREATE ROLE priceproof WITH LOGIN PASSWORD 'priceproof';
CREATE DATABASE priceproof OWNER priceproof;
SQL

  pg_ctl -D "$data_dir" -m fast -w stop
fi

if ! grep -q "host all all all trust" "$data_dir/pg_hba.conf"; then
  printf "\nhost all all all trust\n" >> "$data_dir/pg_hba.conf"
fi

exec postgres -D "$data_dir" -c listen_addresses='*'
