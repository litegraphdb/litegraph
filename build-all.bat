@ECHO OFF
SETLOCAL

IF "%~1" == "" GOTO :Usage
IF NOT "%~2" == "" GOTO :Usage

CALL "%~dp0build-server.bat" "%~1"
SET "EXIT_CODE=%ERRORLEVEL%"
IF NOT "%EXIT_CODE%" == "0" GOTO :Finish

CALL "%~dp0build-dashboard.bat" "%~1"
SET "EXIT_CODE=%ERRORLEVEL%"
IF NOT "%EXIT_CODE%" == "0" GOTO :Finish

CALL "%~dp0build-mcp.bat" "%~1"
SET "EXIT_CODE=%ERRORLEVEL%"
IF NOT "%EXIT_CODE%" == "0" GOTO :Finish

GOTO :Done

:Usage
ECHO.
ECHO Provide exactly one image tag argument.
ECHO Example: build-all.bat v6.0.0
SET "EXIT_CODE=1"
GOTO :Finish

:Done
ECHO.
ECHO Done
SET "EXIT_CODE=0"

:Finish
ECHO ON
@EXIT /B %EXIT_CODE%
