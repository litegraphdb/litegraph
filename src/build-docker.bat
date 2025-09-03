@ECHO OFF
IF "%1" == "" GOTO :Usage
ECHO.
ECHO Building for linux/amd64 and linux/arm64/v8...
docker buildx build -f LiteGraph.Server/Dockerfile --builder cloud-viewio-assistant-builder --platform linux/amd64,linux/arm64/v8 --tag jchristn/litegraph:%1 --push .

GOTO :Done

:Usage
ECHO Provide a tag argument for the build.
ECHO Example: dockerbuild.bat v1.2.0

:Done
ECHO Done
@ECHO ON
