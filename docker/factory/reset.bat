@echo off
setlocal enabledelayedexpansion

REM ==========================================================================
REM reset.bat - Reset LiteGraph docker environment to factory defaults
REM
REM This script destroys runtime Docker data (PostgreSQL volume, optional
REM SQLite state, vector index files, Grafana and Prometheus volumes, logs,
REM and backups) and restores the factory-default Docker configuration files.
REM ==========================================================================

set "SCRIPT_DIR=%~dp0"
set "DOCKER_DIR=%SCRIPT_DIR%..\\"
set "FACTORY_DIR=%SCRIPT_DIR%"
set "REPO_DIR=%DOCKER_DIR%..\\"

echo.
echo ==========================================================
echo   LiteGraph - Reset to Factory Defaults
echo ==========================================================
echo.
echo WARNING: This is a DESTRUCTIVE action. The following will
echo be permanently deleted:
echo.
echo   - LiteGraph PostgreSQL Docker volume
echo   - Optional LiteGraph SQLite database runtime state
echo   - Optional SQLite sidecar files ^(.db-wal, .db-shm, .db-journal^)
echo   - Vector index files under docker\indexes\
echo   - Grafana and Prometheus Docker volumes
echo   - LiteGraph and LiteGraph MCP logs
echo   - LiteGraph and LiteGraph MCP backups
echo   - Docker configuration changes to compose.yaml and prometheus.yml
echo   - Docker configuration changes to litegraph.json
echo   - Docker configuration changes to litegraph-mcp.json
echo   - Grafana provisioning and dashboard asset changes
echo.
echo LiteGraph Docker configuration
echo will be restored to factory defaults.
echo.
set /p "CONFIRM=Type 'RESET' to confirm: "
echo.

if not "%CONFIRM%"=="RESET" (
    echo Aborted. No changes were made.
    exit /b 1
)

echo [1/5] Stopping containers...
pushd "%DOCKER_DIR%"
docker compose down 2>nul
popd

echo [2/5] Removing Docker volumes...
docker volume rm docker_postgresql-data 2>nul
if errorlevel 1 docker volume rm postgresql-data 2>nul
docker volume rm docker_prometheus-data 2>nul
if errorlevel 1 docker volume rm prometheus-data 2>nul
docker volume rm docker_grafana-storage 2>nul
if errorlevel 1 docker volume rm grafana-storage 2>nul
echo         Removed PostgreSQL, Prometheus, and Grafana volumes

echo [3/5] Restoring factory defaults...
del /q "%DOCKER_DIR%litegraph.db" 2>nul
del /q "%DOCKER_DIR%litegraph.db-shm" 2>nul
del /q "%DOCKER_DIR%litegraph.db-wal" 2>nul
del /q "%DOCKER_DIR%litegraph.db-journal" 2>nul
rd /s /q "%DOCKER_DIR%indexes" 2>nul
mkdir "%DOCKER_DIR%indexes" 2>nul
copy /y "%FACTORY_DIR%litegraph.db" "%DOCKER_DIR%litegraph.db" >nul
copy /y "%FACTORY_DIR%litegraph.db-shm" "%DOCKER_DIR%litegraph.db-shm" >nul 2>nul
copy /y "%FACTORY_DIR%litegraph.db-wal" "%DOCKER_DIR%litegraph.db-wal" >nul 2>nul
copy /y "%FACTORY_DIR%litegraph.db-journal" "%DOCKER_DIR%litegraph.db-journal" >nul 2>nul
if exist "%FACTORY_DIR%indexes" xcopy "%FACTORY_DIR%indexes\*" "%DOCKER_DIR%indexes\" /e /i /y >nul 2>nul
copy /y "%FACTORY_DIR%compose.yaml" "%DOCKER_DIR%compose.yaml" >nul
copy /y "%FACTORY_DIR%prometheus.yml" "%DOCKER_DIR%prometheus.yml" >nul
copy /y "%FACTORY_DIR%litegraph.json" "%DOCKER_DIR%litegraph.json" >nul
copy /y "%FACTORY_DIR%litegraph-mcp.json" "%DOCKER_DIR%litegraph-mcp.json" >nul
mkdir "%DOCKER_DIR%grafana\\provisioning\\datasources" 2>nul
mkdir "%DOCKER_DIR%grafana\\provisioning\\dashboards" 2>nul
copy /y "%FACTORY_DIR%grafana\\provisioning\\datasources\\litegraph-prometheus.yml" "%DOCKER_DIR%grafana\\provisioning\\datasources\\litegraph-prometheus.yml" >nul
copy /y "%FACTORY_DIR%grafana\\provisioning\\dashboards\\litegraph.yml" "%DOCKER_DIR%grafana\\provisioning\\dashboards\\litegraph.yml" >nul
mkdir "%REPO_DIR%assets\\grafana" 2>nul
copy /y "%FACTORY_DIR%assets\\grafana\\litegraph-observability-dashboard.json" "%REPO_DIR%assets\\grafana\\litegraph-observability-dashboard.json" >nul
echo         Restored compose.yaml, Prometheus, Grafana, litegraph.json, and litegraph-mcp.json

echo [4/5] Clearing runtime directories...
rd /s /q "%DOCKER_DIR%logs\\litegraph" 2>nul
rd /s /q "%DOCKER_DIR%logs\\litegraph-mcp" 2>nul
rd /s /q "%DOCKER_DIR%backups\\litegraph" 2>nul
rd /s /q "%DOCKER_DIR%backups\\litegraph-mcp" 2>nul
mkdir "%DOCKER_DIR%logs\\litegraph" 2>nul
mkdir "%DOCKER_DIR%logs\\litegraph-mcp" 2>nul
mkdir "%DOCKER_DIR%backups\\litegraph" 2>nul
mkdir "%DOCKER_DIR%backups\\litegraph-mcp" 2>nul
echo         Cleared logs and backups

echo [5/5] Factory reset complete.
echo.
echo To start the environment:
echo   cd %DOCKER_DIR%
echo   docker compose up -d
echo.

endlocal
