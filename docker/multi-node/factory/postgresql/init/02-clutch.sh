#!/bin/bash
#
# First-start initialization for the multi-node deployment: the Clutch role and database.
#
# Clutch shares the PostgreSQL server with LiteGraph but never the LiteGraph database.
# Clutch creates and migrates its own tables on startup.
#
# Inputs (environment of the postgresql service):
#   CLUTCH_DB_PASSWORD   password for the clutch role
#
# Safe to re-run by hand: every statement is idempotent.
#

set -euo pipefail

: "${CLUTCH_DB_PASSWORD:?CLUTCH_DB_PASSWORD must be set on the postgresql service}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
     -v pw="$CLUTCH_DB_PASSWORD" <<-'EOSQL'
	SELECT format('CREATE ROLE clutch LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD %L', :'pw')
	WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'clutch') \gexec
	ALTER ROLE clutch PASSWORD :'pw';
	SELECT 'CREATE DATABASE clutch OWNER clutch'
	WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'clutch') \gexec
	REVOKE ALL ON DATABASE clutch FROM PUBLIC;
	GRANT CONNECT, TEMPORARY ON DATABASE clutch TO clutch;
EOSQL

echo "clutch init: role and database ready"
