@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION
REM ==========================================================================
REM Factory reset: LiteGraph Docker deployment (multi-node cluster)
REM
REM DESTRUCTIVE.  Stops this deployment, deletes its Docker volumes and runtime
REM directories, and restores the configuration files in factory\ .
REM Other deployments and other Compose projects are not touched.
REM ==========================================================================

SET "FACTORY=%~dp0"
SET "DEPLOY=%~dp0..\"

ECHO.
ECHO LiteGraph factory reset: multi-node cluster
ECHO.
ECHO This permanently deletes:
ECHO   - the PostgreSQL volume (LiteGraph and Clutch databases)
ECHO   - Switchboard, Prometheus, Grafana, Loki, and Alloy volumes
ECHO   - logs and backups of every node
ECHO and restores factory configuration:
ECHO   - compose.yaml
ECHO   - litegraph.json
ECHO   - litegraph-mcp.json
ECHO   - prometheus.yaml
ECHO   - nginx/litegraph.conf
ECHO   - nginx/clutch.conf
ECHO   - switchboard/sb.json
ECHO   - postgresql/init/01-litegraph.sh
ECHO   - postgresql/init/02-clutch.sh
ECHO   - clutch/Dockerfile
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
IF EXIST "logs" RMDIR /S /Q "logs"
IF EXIST "backups" RMDIR /S /Q "backups"
ECHO [3/3] Restoring factory configuration...
COPY /Y "%FACTORY%compose.yaml" "compose.yaml" >NUL
COPY /Y "%FACTORY%litegraph.json" "litegraph.json" >NUL
COPY /Y "%FACTORY%litegraph-mcp.json" "litegraph-mcp.json" >NUL
COPY /Y "%FACTORY%prometheus.yaml" "prometheus.yaml" >NUL
COPY /Y "%FACTORY%nginx\litegraph.conf" "nginx\litegraph.conf" >NUL
COPY /Y "%FACTORY%nginx\clutch.conf" "nginx\clutch.conf" >NUL
COPY /Y "%FACTORY%switchboard\sb.json" "switchboard\sb.json" >NUL
COPY /Y "%FACTORY%postgresql\init\01-litegraph.sh" "postgresql\init\01-litegraph.sh" >NUL
COPY /Y "%FACTORY%postgresql\init\02-clutch.sh" "postgresql\init\02-clutch.sh" >NUL
COPY /Y "%FACTORY%clutch\Dockerfile" "clutch\Dockerfile" >NUL
POPD

ECHO.
ECHO Factory reset complete.  Start again with: docker compose up -d
EXIT /B 0
