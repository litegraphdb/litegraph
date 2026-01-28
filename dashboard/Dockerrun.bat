@echo off

IF "%1" == "" GOTO :Usage

docker run ^
  -p 3000:3000 ^
  -t ^
  -i ^
  -e "TERM=xterm-256color" ^
  jchristn77/litegraph-ui:%1

GOTO :Done

:Usage
ECHO Provide one argument indicating the tag. 
ECHO Example: dockerrun.bat v1.0.0
:Done
@echo on
