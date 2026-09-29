#!/bin/bash
#
# Factory reset: LiteGraph Docker deployment (multi-node cluster)
#
# DESTRUCTIVE.  Stops this deployment, deletes its Docker volumes and runtime directories,
# and restores the configuration files in factory/.  Other deployments and other Compose
# projects are not touched.
#

set -euo pipefail

FACTORY="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY="$(cd "$FACTORY/.." && pwd)"

echo
echo "LiteGraph factory reset: multi-node cluster"
echo
echo "This permanently deletes:"
echo "  - the PostgreSQL volume (LiteGraph and Clutch databases)"
echo "  - Switchboard, Prometheus, Grafana, Loki, and Alloy volumes"
echo "  - logs and backups of every node"
echo "and restores factory configuration:"
echo "  - compose.yaml"
echo "  - litegraph.json"
echo "  - litegraph-mcp.json"
echo "  - prometheus.yaml"
echo "  - nginx/litegraph.conf"
echo "  - nginx/clutch.conf"
echo "  - switchboard/sb.json"
echo "  - postgresql/init/01-litegraph.sh"
echo "  - postgresql/init/02-clutch.sh"
echo "  - clutch/Dockerfile"
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
rm -rf "./logs"
rm -rf "./backups"
echo "[3/3] Restoring factory configuration..."
cp "$FACTORY/compose.yaml" "./compose.yaml"
cp "$FACTORY/litegraph.json" "./litegraph.json"
cp "$FACTORY/litegraph-mcp.json" "./litegraph-mcp.json"
cp "$FACTORY/prometheus.yaml" "./prometheus.yaml"
cp "$FACTORY/nginx/litegraph.conf" "./nginx/litegraph.conf"
cp "$FACTORY/nginx/clutch.conf" "./nginx/clutch.conf"
cp "$FACTORY/switchboard/sb.json" "./switchboard/sb.json"
cp "$FACTORY/postgresql/init/01-litegraph.sh" "./postgresql/init/01-litegraph.sh"
cp "$FACTORY/postgresql/init/02-clutch.sh" "./postgresql/init/02-clutch.sh"
cp "$FACTORY/clutch/Dockerfile" "./clutch/Dockerfile"

echo
echo "Factory reset complete.  Start again with: docker compose up -d"
