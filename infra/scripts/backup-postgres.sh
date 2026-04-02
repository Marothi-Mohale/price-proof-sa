#!/usr/bin/env bash
set -eu

connection_string="${1:?connection string required}"
output_path="${2:?output path required}"
timestamp="$(date +%Y%m%d-%H%M%S)"
mkdir -p "$output_path"
target_file="$output_path/priceproof-$timestamp.dump"

pg_dump --format=custom --file "$target_file" --dbname "$connection_string"
echo "Created backup: $target_file"
