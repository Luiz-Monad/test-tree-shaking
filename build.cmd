@echo off
cls

IF NOT EXIST .fake/build.fsx (
  paket install
)

paket restore
if errorlevel 1 (
  exit /b %errorlevel%
)

fake build %*