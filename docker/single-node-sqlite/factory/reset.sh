#!/bin/bash
#
# Factory reset: LiteGraph Docker deployment (single node, SQLite)
#
# DESTRUCTIVE.  Stops this deployment, deletes its Docker volumes and runtime directories,
# and restores the configuration files in factory/.  Other deployments and other Compose
# projects are not touched.
#

set -euo pipefail

FACTORY="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY="$(cd "$FACTORY/.." && pwd)"

echo
echo "LiteGraph factory reset: single node, SQLite"
echo
echo "This permanently deletes:"
echo "  - the SQLite database and vector index files in data/"
echo "  - Prometheus, Grafana, Loki, and Alloy volumes"
echo "  - logs and backups"
echo "and restores factory configuration:"
echo "  - compose.yaml"
echo "  - litegraph.json"
echo "  - litegraph-mcp.json"
echo "  - prometheus.yaml"
echo
read -r -p "Type RESET to continue: " CONFIRM
if [ "$CONFIRM" != "RESET" ]; then
    echo "Aborted; nothing was changed."
    exit 1
fi

cd "$DEPLOY"
echo "[1/3] Stopping the deployment and removing its volumes..."
docker compose --profile switchboard --profile tools down -v
echo "[2/3] Removing runtime directories..."
rm -rf "./data"
rm -rf "./logs"
rm -rf "./backups"
echo "[3/3] Restoring factory configuration..."
cp "$FACTORY/compose.yaml" "./compose.yaml"
cp "$FACTORY/litegraph.json" "./litegraph.json"
cp "$FACTORY/litegraph-mcp.json" "./litegraph-mcp.json"
cp "$FACTORY/prometheus.yaml" "./prometheus.yaml"

echo
echo "Factory reset complete.  Start again with: docker compose up -d"
