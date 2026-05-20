@ECHO OFF
IF "%1" == "" GOTO :Usage
SET "PUSHED="
PUSHD "%~dp0..\.."
SET "PUSHED=1"
ECHO.
ECHO Building for linux/amd64 and linux/arm64/v8...
docker buildx build -f src/LiteGraph.McpServer/Dockerfile --platform linux/amd64,linux/arm64/v8 --tag jchristn77/litegraph-mcp:%1 --tag jchristn77/litegraph-mcp:latest --push .
POPD
SET "PUSHED="

GOTO :Done

:Usage
ECHO Provide a tag argument for the build.
ECHO Example: dockerbuild.bat v6.0.1

:Done
IF DEFINED PUSHED POPD
ECHO Done
@ECHO ON
