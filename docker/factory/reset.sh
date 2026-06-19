#!/bin/bash
#
# reset.sh - Reset LiteGraph docker environment to factory defaults
#
# This script destroys runtime Docker data (PostgreSQL volume, optional
# SQLite state, vector index files, Grafana and Prometheus volumes, logs,
# and backups) and restores the factory-default Docker configuration files.

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
FACTORY_DIR="$SCRIPT_DIR"
REPO_DIR="$(cd "$DOCKER_DIR/.." && pwd)"

echo ""
echo "=========================================================="
echo "  LiteGraph - Reset to Factory Defaults"
echo "=========================================================="
echo ""
echo "WARNING: This is a DESTRUCTIVE action. The following will"
echo "be permanently deleted:"
echo ""
echo "  - LiteGraph PostgreSQL Docker volume"
echo "  - Optional LiteGraph SQLite database runtime state"
echo "  - Optional SQLite sidecar files (.db-wal, .db-shm, .db-journal)"
echo "  - Vector index files under docker/indexes/"
echo "  - Grafana and Prometheus Docker volumes"
echo "  - LiteGraph and LiteGraph MCP logs"
echo "  - LiteGraph and LiteGraph MCP backups"
echo "  - Docker configuration changes to compose.yaml and prometheus.yaml"
echo "  - Docker configuration changes to litegraph.json"
echo "  - Docker configuration changes to litegraph-mcp.json"
echo "  - Grafana provisioning and dashboard asset changes"
echo ""
echo "LiteGraph Docker configuration"
echo "will be restored to factory defaults."
echo ""
read -r -p "Type 'RESET' to confirm: " CONFIRM
echo ""

if [ "$CONFIRM" != "RESET" ]; then
  echo "Aborted. No changes were made."
  exit 1
fi

echo "[1/5] Stopping containers..."
cd "$DOCKER_DIR"
docker compose down 2>/dev/null || true

echo "[2/5] Removing Docker volumes..."
docker volume rm docker_postgresql-data 2>/dev/null || docker volume rm postgresql-data 2>/dev/null || true
docker volume rm docker_prometheus-data 2>/dev/null || docker volume rm prometheus-data 2>/dev/null || true
docker volume rm docker_grafana-storage 2>/dev/null || docker volume rm grafana-storage 2>/dev/null || true
echo "        Removed PostgreSQL, Prometheus, and Grafana volumes"

echo "[3/5] Restoring factory defaults..."
rm -f "$DOCKER_DIR/litegraph.db"
rm -f "$DOCKER_DIR/litegraph.db-shm"
rm -f "$DOCKER_DIR/litegraph.db-wal"
rm -f "$DOCKER_DIR/litegraph.db-journal"
rm -rf "$DOCKER_DIR/indexes"
mkdir -p "$DOCKER_DIR/indexes"
cp "$FACTORY_DIR/litegraph.db" "$DOCKER_DIR/litegraph.db"
cp "$FACTORY_DIR/litegraph.db-shm" "$DOCKER_DIR/litegraph.db-shm" 2>/dev/null || true
cp "$FACTORY_DIR/litegraph.db-wal" "$DOCKER_DIR/litegraph.db-wal" 2>/dev/null || true
cp "$FACTORY_DIR/litegraph.db-journal" "$DOCKER_DIR/litegraph.db-journal" 2>/dev/null || true
if [ -d "$FACTORY_DIR/indexes" ]; then
  cp -R "$FACTORY_DIR/indexes/." "$DOCKER_DIR/indexes/" 2>/dev/null || true
fi
cp "$FACTORY_DIR/compose.yaml" "$DOCKER_DIR/compose.yaml"
cp "$FACTORY_DIR/prometheus.yaml" "$DOCKER_DIR/prometheus.yaml"
cp "$FACTORY_DIR/litegraph.json" "$DOCKER_DIR/litegraph.json"
cp "$FACTORY_DIR/litegraph-mcp.json" "$DOCKER_DIR/litegraph-mcp.json"
mkdir -p "$DOCKER_DIR/grafana/provisioning/datasources" "$DOCKER_DIR/grafana/provisioning/dashboards"
cp "$FACTORY_DIR/grafana/provisioning/datasources/litegraph-prometheus.yml" "$DOCKER_DIR/grafana/provisioning/datasources/litegraph-prometheus.yml"
cp "$FACTORY_DIR/grafana/provisioning/dashboards/litegraph.yml" "$DOCKER_DIR/grafana/provisioning/dashboards/litegraph.yml"
mkdir -p "$REPO_DIR/assets/grafana"
cp "$FACTORY_DIR/assets/grafana/litegraph-observability-dashboard.json" "$REPO_DIR/assets/grafana/litegraph-observability-dashboard.json"
echo "        Restored compose.yaml, Prometheus, Grafana, litegraph.json, and litegraph-mcp.json"

echo "[4/5] Clearing runtime directories..."
rm -rf "$DOCKER_DIR/logs/litegraph" "$DOCKER_DIR/logs/litegraph-mcp"
rm -rf "$DOCKER_DIR/backups/litegraph" "$DOCKER_DIR/backups/litegraph-mcp"
mkdir -p "$DOCKER_DIR/logs/litegraph" "$DOCKER_DIR/logs/litegraph-mcp"
mkdir -p "$DOCKER_DIR/backups/litegraph" "$DOCKER_DIR/backups/litegraph-mcp"
echo "        Cleared logs and backups"

echo "[5/5] Factory reset complete."
echo ""
echo "To start the environment:"
echo "  cd $DOCKER_DIR"
echo "  docker compose up -d"
echo ""
