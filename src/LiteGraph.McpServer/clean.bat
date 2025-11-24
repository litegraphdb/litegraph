@echo off

del /s /f /q litegraph.json
del /s /f /q logs\*.*
del /s /f /q temp\*.*
del /s /f /q backups\*.*

rmdir /s /q logs
rmdir /s /q temp
rmdir /s /q backups

if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

@echo on
