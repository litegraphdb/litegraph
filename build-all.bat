@ECHO OFF
SETLOCAL

IF "%~1" == "" GOTO :Usage
IF NOT "%~2" == "" GOTO :Usage

CALL "%~dp0build-server.bat" "%~1"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

CALL "%~dp0build-dashboard.bat" "%~1"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

CALL "%~dp0build-mcp.bat" "%~1"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

GOTO :Done

:Usage
ECHO.
ECHO Provide exactly one image tag argument.
ECHO Example: build-all.bat v6.0.0
EXIT /B 1

:Done
ECHO.
ECHO Done
EXIT /B 0
