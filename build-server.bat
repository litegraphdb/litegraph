@ECHO OFF
IF "%1" == "" GOTO :Usage
ECHO.
ECHO Building for linux/amd64 and linux/arm64/v8...
SET "LATEST_TAG="
REM A release tag (vMAJOR.MINOR.PATCH, for example v10.0.0) also moves :latest; any other tag (v10.0.0-rc1, test builds) does not.
ECHO %~1| findstr /R /X "v[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*" >NUL && SET "LATEST_TAG=--tag jchristn77/litegraph:latest"
IF DEFINED LATEST_TAG (ECHO Release tag %~1: also tagging jchristn77/litegraph:latest) ELSE (ECHO Not a release tag: jchristn77/litegraph:latest is left unchanged)
docker buildx build -f src/LiteGraph.Server/Dockerfile --builder cloud-jchristn77-jchristn77 --platform linux/amd64,linux/arm64/v8 --tag jchristn77/litegraph:%~1 %LATEST_TAG% --push src
GOTO :Done

:Usage
ECHO.
ECHO Provide a tag argument.
ECHO Example: build-server.bat v10.0.0

:Done
ECHO.
ECHO Done
