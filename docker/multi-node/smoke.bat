@ECHO OFF
SETLOCAL
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0smoke.ps1" %*
@EXIT /B %ERRORLEVEL%
