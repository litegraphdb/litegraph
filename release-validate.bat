@ECHO OFF
SETLOCAL EnableExtensions

SET "ROOT=%~dp0"
SET "EXIT_CODE=0"

ECHO.
ECHO Running LiteGraph v7.0 local release validation...

dotnet build "%ROOT%src\LiteGraph.sln" -c Debug
IF ERRORLEVEL 1 GOTO :Error

dotnet list "%ROOT%src\LiteGraph.sln" package --vulnerable --include-transitive
IF ERRORLEVEL 1 GOTO :Error

dotnet build "%ROOT%src\LiteGraph\LiteGraph.csproj" -c Release
IF ERRORLEVEL 1 GOTO :Error

dotnet pack "%ROOT%src\LiteGraph\LiteGraph.csproj" -c Release --no-build -o "%ROOT%artifacts\release-validate\nuget"
IF ERRORLEVEL 1 GOTO :Error

cmd /C "set LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING=& dotnet run --project ""%ROOT%src\Test.Automated\Test.Automated.csproj"" -c Debug --framework net10.0 -- --transaction-concurrency"
IF ERRORLEVEL 1 GOTO :Error

IF NOT "%LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING%" == "" (
  dotnet run --project "%ROOT%src\Test.Automated\Test.Automated.csproj" -c Debug --framework net10.0 -- --transaction-concurrency
  IF ERRORLEVEL 1 GOTO :Error
) ELSE (
  ECHO.
  ECHO Skipping PostgreSQL transaction gate because LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING is not set.
)

PUSHD "%ROOT%sdk\js"
npm test -- --runInBand
IF ERRORLEVEL 1 GOTO :ErrorPop
npm pack --dry-run
IF ERRORLEVEL 1 GOTO :ErrorPop
POPD

PUSHD "%ROOT%sdk\python"
SET "PYTHONPATH=%CD%\src"
py -m pytest
IF ERRORLEVEL 1 GOTO :ErrorPop
py -m pip install --quiet build
IF ERRORLEVEL 1 GOTO :ErrorPop
py -m build --outdir "%ROOT%artifacts\release-validate\python"
IF ERRORLEVEL 1 GOTO :ErrorPop
POPD

PUSHD "%ROOT%dashboard"
npm test -- --runInBand
IF ERRORLEVEL 1 GOTO :ErrorPop
npm run build
IF ERRORLEVEL 1 GOTO :ErrorPop
POPD

ECHO.
ECHO Release validation passed.
GOTO :Finish

:ErrorPop
SET "EXIT_CODE=1"
POPD
GOTO :Finish

:Error
SET "EXIT_CODE=1"

:Finish
ECHO ON
ENDLOCAL & EXIT /B %EXIT_CODE%
