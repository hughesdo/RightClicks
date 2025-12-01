@echo off
REM Wrapper that runs install.bat and captures all output to a log file
REM This lets us review what happened during installation

set LOGFILE=%~dp0install-log.txt

echo Installation started at %date% %time% > "%LOGFILE%"
echo. >> "%LOGFILE%"

REM Run install.bat and capture all output
call "%~dp0install.bat" >> "%LOGFILE%" 2>&1

echo. >> "%LOGFILE%"
echo Installation finished at %date% %time% >> "%LOGFILE%"

echo.
echo ========================================
echo Log saved to: %LOGFILE%
echo ========================================
pause

