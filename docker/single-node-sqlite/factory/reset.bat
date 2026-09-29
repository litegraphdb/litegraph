@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION
REM ==========================================================================
REM Factory reset: LiteGraph Docker deployment (single node, SQLite)
REM
REM DESTRUCTIVE.  Stops this deployment, deletes its Docker volumes and runtime
REM directories, and restores the configuration files in factory\ .
REM Other deployments and other Compose projects are not touched.
REM ==========================================================================

SET "FACTORY=%~dp0"
SET "DEPLOY=%~dp0..\"

ECHO.
ECHO LiteGraph factory reset: single node, SQLite
ECHO.
ECHO This permanently deletes:
ECHO   - the SQLite database and vector index files in data/
ECHO   - Prometheus, Grafana, Loki, and Alloy volumes
ECHO   - logs and backups
ECHO and restores factory configuration:
ECHO   - compose.yaml
ECHO   - litegraph.json
ECHO   - litegraph-mcp.json
ECHO   - prometheus.yaml
ECHO.
SET /P "CONFIRM=Type RESET to continue: "
IF NOT "%CONFIRM%"=="RESET" (
    ECHO Aborted; nothing was changed.
    EXIT /B 1
)

PUSHD "%DEPLOY%"
ECHO [1/3] Stopping the deployment and removing its volumes...
docker compose --profile switchboard --profile tools down -v
ECHO [2/3] Removing runtime directories...
IF EXIST "data" RMDIR /S /Q "data"
IF EXIST "logs" RMDIR /S /Q "logs"
IF EXIST "backups" RMDIR /S /Q "backups"
ECHO [3/3] Restoring factory configuration...
COPY /Y "%FACTORY%compose.yaml" "compose.yaml" >NUL
COPY /Y "%FACTORY%litegraph.json" "litegraph.json" >NUL
COPY /Y "%FACTORY%litegraph-mcp.json" "litegraph-mcp.json" >NUL
COPY /Y "%FACTORY%prometheus.yaml" "prometheus.yaml" >NUL
POPD

ECHO.
ECHO Factory reset complete.  Start again with: docker compose up -d
EXIT /B 0
