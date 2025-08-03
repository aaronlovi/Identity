@echo off
setlocal EnableDelayedExpansion

:: Configuration - modify these values as needed
set GCP_PROJECT=identity-ctx-dev
set GCP_REGION=northamerica-northeast2
set DB_INSTANCE=identity-postgres-dev
set DB_PORT=5432

:MAIN_MENU
cls
echo ===============================================
echo   Identity Service - Development Tools
echo ===============================================
echo.
echo Project: %GCP_PROJECT%
echo Region:  %GCP_REGION%
echo DB:      %DB_INSTANCE%
echo Port:    %DB_PORT%
echo.
echo ===============================================
echo   Available Commands:
echo ===============================================
echo.
echo [1] Start Cloud SQL Proxy
echo [Q] Quit
echo.
set /p choice="Enter your choice: "

if /i "%choice%"=="1" goto START_PROXY
if /i "%choice%"=="q" goto QUIT
if /i "%choice%"=="Q" goto QUIT

echo.
echo Invalid choice. Please try again.
pause
goto MAIN_MENU

:START_PROXY
cls
echo ===============================================
echo   Starting Cloud SQL Proxy
echo ===============================================
echo.
echo Project:  %GCP_PROJECT%
echo Instance: %GCP_PROJECT%:%GCP_REGION%:%DB_INSTANCE%
echo Port:     %DB_PORT%
echo.
echo Starting proxy in new window...
echo.
echo Connection string for HeidiSQL:
echo   Host: localhost
echo   Port: %DB_PORT%
echo   Database: identity_dev
echo.
echo The proxy will run in a separate window.
echo Close that window or press Ctrl+C in it to stop the proxy.
echo.

start "Cloud SQL Proxy" cloud-sql-proxy %GCP_PROJECT%:%GCP_REGION%:%DB_INSTANCE% --port %DB_PORT%

echo Cloud SQL Proxy started in new window.
echo.
pause
goto MAIN_MENU

:QUIT
cls
echo.
echo Goodbye!
echo.
exit /b 0
