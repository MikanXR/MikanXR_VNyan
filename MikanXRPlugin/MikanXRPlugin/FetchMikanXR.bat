@echo off
call SetMikanVars_x64.bat

:: Set path variables
set "TARGET_DIR=%~dp0"

:: Copy over the client DLLs
xcopy /y "%MIKAN_XR_PATH%\MikanCore.dll" "%TARGET_DIR%" || goto failure
xcopy /y "%MIKAN_XR_PATH%\MikanClientCSharp.dll" "%TARGET_DIR%" || goto failure
xcopy /y "%MIKAN_XR_PATH%\SpoutLibrary.dll" "%TARGET_DIR%" || goto failure

echo "Successfully updated MikanXR DLLs from: %MIKAN_XR_PATH%"
pause
EXIT /B 0

:failure
echo "Failed to copy files from MikanXR DLLs from: %MIKAN_XR_PATH%"
pause
EXIT /B 1