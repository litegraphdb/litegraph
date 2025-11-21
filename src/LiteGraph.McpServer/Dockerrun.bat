@echo off

IF "%1" == "" GOTO :Usage

if not exist litegraph.json (
  echo Configuration file litegraph.json not found.
  exit /b 1
)

REM Items that require persistence
REM   litegraph.json
REM   logs/
REM   temp/
REM   backups/

REM Argument order matters!

docker run ^
  -p 8200:8200 ^
  -p 8201:8201 ^
  -p 8202:8202 ^
  -t ^
  -i ^
  -e "TERM=xterm-256color" ^
  -v .\litegraph.json:/app/litegraph.json ^
  -v .\logs\:/app/logs/ ^
  -v .\temp\:/app/temp/ ^
  -v .\backups\:/app/backups/ ^
  jchristn/litegraph-mcp:%1

GOTO :Done

:Usage
ECHO Provide one argument indicating the tag. 
ECHO Example: dockerrun.bat v1.0.0
:Done
@echo on

