#!/bin/bash
#
# First-start initialization for the single-node PostgreSQL deployment.
#
# The PostgreSQL image runs this once, when the data directory is empty. It creates the
# pgvector extension in the LiteGraph database so the application never needs extension
# privileges. On an existing volume this script does not run; LiteGraph's own startup
# migration creates the extension instead (the single-node role is the database owner).
#
# Safe to re-run by hand: every statement is idempotent.
#

set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
	CREATE EXTENSION IF NOT EXISTS vector;
EOSQL

echo "litegraph init: pgvector extension ready in database $POSTGRES_DB"
