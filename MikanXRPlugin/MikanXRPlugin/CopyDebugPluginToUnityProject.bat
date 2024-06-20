@echo off

:: Set path variables
set "SOURCE_DIR=%~dp0\bin\Debug\netstandard2.0"

set "TARGET_DIR=%~dp0\..\..\MikanXR_VNyanSDK\Assets\MikanPlugin\Plugins"

:: Copy over the client DLLs
xcopy /y "%SOURCE_DIR%\MikanXRPlugin.dll" "%TARGET_DIR%" || goto failure
xcopy /y "%SOURCE_DIR%\MikanXRPlugin.pdb" "%TARGET_DIR%" || goto failure

echo "Successfully updated MikanXR DLLs from: %TARGET_DIR%"
pause
EXIT /B 0

:failure
echo "Failed to copy files from MikanXR DLLs from: %TARGET_DIR%"
pause
EXIT /B 1