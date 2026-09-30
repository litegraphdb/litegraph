#!/bin/bash
#
# First-start initialization for the multi-node deployment: the LiteGraph role, database,
# schema, and pgvector extension.
#
# The PostgreSQL image runs this once, as the superuser, when the data directory is empty.
# The extension is created here so the LiteGraph role never needs extension privileges.
# Schema contents (tables, indexes, migrations) are created by LiteGraph itself.
#
# Inputs (environment of the postgresql service):
#   LITEGRAPH_DB_PASSWORD   password for the litegraph role
#   LITEGRAPH_DB_SCHEMA     schema LiteGraph uses (default litegraph)
#
# Safe to re-run by hand: every statement is idempotent.
#

set -euo pipefail

: "${LITEGRAPH_DB_PASSWORD:?LITEGRAPH_DB_PASSWORD must be set on the postgresql service}"
LITEGRAPH_DB_SCHEMA="${LITEGRAPH_DB_SCHEMA:-litegraph}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
     -v pw="$LITEGRAPH_DB_PASSWORD" <<-'EOSQL'
	SELECT format('CREATE ROLE litegraph LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD %L', :'pw')
	WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'litegraph') \gexec
	ALTER ROLE litegraph PASSWORD :'pw';
	SELECT 'CREATE DATABASE litegraph OWNER litegraph'
	WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'litegraph') \gexec
	REVOKE ALL ON DATABASE litegraph FROM PUBLIC;
	GRANT CONNECT, TEMPORARY ON DATABASE litegraph TO litegraph;
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname litegraph \
     -v schema="$LITEGRAPH_DB_SCHEMA" <<-'EOSQL'
	CREATE EXTENSION IF NOT EXISTS vector;
	CREATE SCHEMA IF NOT EXISTS :"schema" AUTHORIZATION litegraph;
	REVOKE CREATE ON SCHEMA public FROM PUBLIC;
EOSQL

echo "litegraph init: role, database, schema '$LITEGRAPH_DB_SCHEMA', and pgvector extension ready"
