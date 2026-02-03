@ECHO OFF
IF "%1" == "" GOTO :Usage
ECHO.
ECHO Building for linux/amd64 and linux/arm64/v8...
docker buildx build -f src/LiteGraph.Server/Dockerfile --builder cloud-jchristn77-jchristn77 --platform linux/amd64,linux/arm64/v8 --tag jchristn77/litegraph:%1 --tag jchristn77/litegraph:latest --push src
GOTO :Done

:Usage
ECHO.
ECHO Provide a tag argument.
ECHO Example: build-server.bat v1.0.0

:Done
ECHO.
ECHO Done
@ECHO ON
