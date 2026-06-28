@echo off
rem Feature 212: uniform product-root verb wrapper (parity with build.sh). Every verb delegates to
rem the single FAKE entry (dotnet fsi build.fsx -t <Target>). Mirrors the existing fake.cmd style.
setlocal
set "target="
if /I "%~1"=="restore" set "target=Restore"
if /I "%~1"=="build"   set "target=Build"
if /I "%~1"=="test"    set "target=Test"
if /I "%~1"=="run"     set "target=Run"
if /I "%~1"=="verify"  set "target=Verify"
if /I "%~1"=="pack"    set "target=Pack"
if not defined target goto :usage
dotnet fsi build.fsx -t %target%
exit /b %errorlevel%
:usage
if "%~1"=="" (echo build.cmd: missing verb>&2) else (echo build.cmd: unknown verb '%~1'>&2)
echo Usage: build.cmd ^<verb^>>&2
echo Supported verbs: restore build test run verify pack>&2
exit /b 2
