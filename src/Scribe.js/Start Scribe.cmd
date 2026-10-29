@echo off
cd /d "%~dp0"
set "PATH=C:\Program Files\nodejs;%PATH%"
if exist "dist\Scribe.js-win32-x64\Scribe.js.exe" (
  start "" "dist\Scribe.js-win32-x64\Scribe.js.exe"
) else (
  if exist "node_modules\electron\dist\electron.exe" (
    start "" "node_modules\electron\dist\electron.exe" .
  ) else (
    echo Run npm ci in this folder first.
    pause
  )
)
