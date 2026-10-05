@echo off
setlocal
cd /d "%~dp0"

set "CSC64=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "CSC32=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "CSC="

if exist "%CSC64%" set "CSC=%CSC64%"
if not defined CSC if exist "%CSC32%" set "CSC=%CSC32%"

if not defined CSC (
  echo.
  echo [ERROR] Windows .NET Framework C# compiler was not found.
  echo.
  pause
  exit /b 1
)

echo.
echo Building PDF Compressor release builder...
"%CSC%" /nologo /target:exe /optimize+ ^
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ^
  /out:"ReleaseBuilder.exe" "build\ReleaseBuilder.cs"

if errorlevel 1 (
  echo.
  echo [ERROR] ReleaseBuilder compilation failed.
  pause
  exit /b 1
)

echo.
echo Building integrated Setup...
"ReleaseBuilder.exe"

if errorlevel 1 (
  echo.
  echo Build failed.
  pause
  exit /b 1
)

echo.
echo Build complete. Opening dist...
pause
explorer.exe "%~dp0dist"
