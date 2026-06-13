@ECHO OFF
SETLOCAL EnableExtensions

IF "%~1" == "" GOTO :Usage

SET "NUGET_API_KEY=%~1"
SET "ROOT=%~dp0"
SET "OUTPUT=%ROOT%artifacts\nuget"
SET "NUGET_SOURCE=https://api.nuget.org/v3/index.json"
SET "PACKAGE_VERSION=6.0.3"

ECHO.
ECHO Packing LiteGraph %PACKAGE_VERSION% packages...

IF EXIST "%OUTPUT%" RMDIR /S /Q "%OUTPUT%"
MKDIR "%OUTPUT%"
IF ERRORLEVEL 1 GOTO :Error

dotnet pack "%ROOT%src\LiteGraph\LiteGraph.csproj" -c Release -o "%OUTPUT%" /p:PackageVersion=%PACKAGE_VERSION% /p:GeneratePackageOnBuild=false
IF ERRORLEVEL 1 GOTO :Error

dotnet pack "%ROOT%sdk\csharp\src\LiteGraph.Sdk\LiteGraph.Sdk.csproj" -c Release -o "%OUTPUT%" /p:PackageVersion=%PACKAGE_VERSION% /p:GeneratePackageOnBuild=false
IF ERRORLEVEL 1 GOTO :Error

IF NOT EXIST "%OUTPUT%\LiteGraph.%PACKAGE_VERSION%.nupkg" (
  ECHO Missing package: "%OUTPUT%\LiteGraph.%PACKAGE_VERSION%.nupkg"
  GOTO :Error
)

IF NOT EXIST "%OUTPUT%\LiteGraph.Sdk.%PACKAGE_VERSION%.nupkg" (
  ECHO Missing package: "%OUTPUT%\LiteGraph.Sdk.%PACKAGE_VERSION%.nupkg"
  GOTO :Error
)

ECHO.
ECHO Publishing packages to NuGet...

dotnet nuget push "%OUTPUT%\LiteGraph.%PACKAGE_VERSION%.nupkg" --api-key "%NUGET_API_KEY%" --source "%NUGET_SOURCE%" --skip-duplicate
IF ERRORLEVEL 1 GOTO :Error

dotnet nuget push "%OUTPUT%\LiteGraph.Sdk.%PACKAGE_VERSION%.nupkg" --api-key "%NUGET_API_KEY%" --source "%NUGET_SOURCE%" --skip-duplicate
IF ERRORLEVEL 1 GOTO :Error

IF EXIST "%OUTPUT%\LiteGraph.%PACKAGE_VERSION%.snupkg" (
  dotnet nuget push "%OUTPUT%\LiteGraph.%PACKAGE_VERSION%.snupkg" --api-key "%NUGET_API_KEY%" --source "%NUGET_SOURCE%" --skip-duplicate
  IF ERRORLEVEL 1 GOTO :Error
)

IF EXIST "%OUTPUT%\LiteGraph.Sdk.%PACKAGE_VERSION%.snupkg" (
  dotnet nuget push "%OUTPUT%\LiteGraph.Sdk.%PACKAGE_VERSION%.snupkg" --api-key "%NUGET_API_KEY%" --source "%NUGET_SOURCE%" --skip-duplicate
  IF ERRORLEVEL 1 GOTO :Error
)

ECHO.
ECHO Published LiteGraph and LiteGraph.Sdk %PACKAGE_VERSION%.
GOTO :Done

:Usage
ECHO.
ECHO Provide a NuGet API key.
ECHO Example: publish-nuget.bat YOUR_NUGET_API_KEY
GOTO :Done

:Error
ECHO.
ECHO NuGet publish failed.
EXIT /B 1

:Done
ECHO.
ENDLOCAL
@ECHO ON
