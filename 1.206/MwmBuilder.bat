@echo off
REM -------------------------------------------------------------------------
REM mwmbuilder.bat
REM Usage: mwmbuilder.bat ^<mwmbuilder_exe^> ^<source_folder^> ^<subtypeId^> ^
REM                     ^<output_folder^> ^<materials_path^> [clean]
REM -------------------------------------------------------------------------

if "%~5"=="" (
  echo.
  echo Usage: %~nx0 ^<mwmbuilder_exe^> ^<source_folder^> ^<subtypeId^> ^
  echo        ^<output_folder^> ^<materials_path^> [clean]
  echo.
  goto :EOF
)

set "MWMBUILDER=%~1"
set "SRC=%~2"
set "SUBTYPE=%~3"
set "OUT=%~4"
set "MATS=%~5"
set "CLEANFLAG=%~6"
set "LOGFILE=%SRC%\%SUBTYPE%.mwm.log"

echo.
echo === Running MWMBuilder for %SUBTYPE% ===
"%MWMBUILDER%" /f /s:"%SRC%" /m:%SUBTYPE%*.fbx /o:"%OUT%" /x:"%MATS%" > "%LOGFILE%" 2>&1
set "RESULT=%ERRORLEVEL%"

if /I "%CLEANFLAG%"=="clean" (
  echo --- Cleaning up loose files ---
  del /Q "%SRC%\%SUBTYPE%_BS*.fbx" "%SRC%\%SUBTYPE%_LOD*.fbx" "%SRC%\%SUBTYPE%.*.fbx" >nul 2>&1
  del /Q "%SRC%\%SUBTYPE%_BS*.xml" "%SRC%\%SUBTYPE%_LOD*.xml" "%SRC%\%SUBTYPE%.*.xml" >nul 2>&1
  del /Q "%SRC%\%SUBTYPE%_BS*.hkt" "%SRC%\%SUBTYPE%_LOD*.hkt" "%SRC%\%SUBTYPE%.*.hkt" >nul 2>&1
  del /Q "%SRC%\%SUBTYPE%_BS*.log" "%SRC%\%SUBTYPE%_LOD*.log" "%SRC%\%SUBTYPE%.*.log" "%SRC%\%SUBTYPE%.log" >nul 2>&1
)

if %RESULT% equ 0 (
  echo [INFO] Build succeeded for %SUBTYPE% >> "%LOGFILE%"
) else (
  echo [ERROR] Build failed (exit code %RESULT%) for %SUBTYPE% >> "%LOGFILE%"
)

exit /B %RESULT%
