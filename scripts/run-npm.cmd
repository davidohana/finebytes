@echo off
setlocal

if exist "%ProgramFiles%\nodejs\" (
  set "PATH=%ProgramFiles%\nodejs;%PATH%"
)

if exist "%LocalAppData%\Programs\nodejs\" (
  set "PATH=%LocalAppData%\Programs\nodejs;%PATH%"
)

call npm %*
exit /b %ERRORLEVEL%
