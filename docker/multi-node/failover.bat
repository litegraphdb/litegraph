@ECHO OFF
SETLOCAL
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0failover.ps1" %*
@EXIT /B %ERRORLEVEL%
