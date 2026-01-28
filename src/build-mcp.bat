@ECHO OFF
IF "%1" == "" GOTO :Usage
ECHO.
ECHO Building for linux/amd64 and linux/arm64/v8...
docker buildx build -f LiteGraph.McpServer/Dockerfile --builder cloud-jchristn77-jchristn77 --platform linux/amd64,linux/arm64/v8 --tag jchristn77/litegraph-mcp:%1 --tag jchristn77/litegraph-mcp:latest --push .

GOTO :Done

:Usage
ECHO Provide a tag argument for the build.
ECHO Example: dockerbuild.bat v1.2.0

:Done
ECHO Done
@ECHO ON
