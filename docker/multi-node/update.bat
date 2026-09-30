@ECHO OFF
REM Pull the latest published images and recreate the stack.  Non-destructive: named volumes
REM and bind-mounted data are preserved.  Honors LITEGRAPH_IMAGE_TAG.

SETLOCAL
PUSHD "%~dp0"
IF ERRORLEVEL 1 GOTO :Failed

docker compose pull
IF ERRORLEVEL 1 GOTO :Failed

docker compose down
IF ERRORLEVEL 1 GOTO :Failed

docker compose up -d
IF ERRORLEVEL 1 GOTO :Failed

docker ps -a

POPD
@EXIT /B 0

:Failed
SET "EXIT_CODE=%ERRORLEVEL%"
POPD 2>NUL
@EXIT /B %EXIT_CODE%
