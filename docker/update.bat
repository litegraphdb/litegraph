@ECHO OFF
SETLOCAL

PUSHD "%~dp0"
IF ERRORLEVEL 1 GOTO :Failed

docker compose down
IF ERRORLEVEL 1 GOTO :Failed

docker compose pull
IF ERRORLEVEL 1 GOTO :Failed

docker compose up -d
IF ERRORLEVEL 1 GOTO :Failed

POPD
ECHO ON
@EXIT /B 0

:Failed
SET "EXIT_CODE=%ERRORLEVEL%"
POPD 2>NUL
ECHO ON
@EXIT /B %EXIT_CODE%
