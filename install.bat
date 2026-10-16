@echo off
setlocal
:: Installs the plugin into a VNyan install: the assembly into Items\Assemblies and the shader
:: bundle into Items\Assemblies\MikanXR. The VNyan folder comes from the first argument or from
:: VNYAN_DIR; the bundle comes from the second argument or Build\mikanxr_shaders.bundle.
set "VNYAN=%~1"
if "%VNYAN%"=="" set "VNYAN=%VNYAN_DIR%"
if "%VNYAN%"=="" set "VNYAN=D:\tools\vnyan"
set "BUNDLE=%~2"
if "%BUNDLE%"=="" set "BUNDLE=%~dp0Build\mikanxr_shaders.bundle"
set "DLL=%~dp0bin\Release\netstandard2.1\MikanXR_VNyan.dll"
if not exist "%DLL%" (
  echo Build the plugin first: dotnet build MikanXR_VNyan.csproj -c Release
  exit /b 1
)
if not exist "%VNYAN%\Items\Assemblies" (
  echo No VNyan install at "%VNYAN%"
  exit /b 1
)
copy /y "%DLL%" "%VNYAN%\Items\Assemblies\" >nul || exit /b 1
if not exist "%VNYAN%\Items\Assemblies\MikanXR" mkdir "%VNYAN%\Items\Assemblies\MikanXR"
if exist "%BUNDLE%" (
  copy /y "%BUNDLE%" "%VNYAN%\Items\Assemblies\MikanXR\" >nul || exit /b 1
) else (
  echo No shader bundle at "%BUNDLE%": build it with the Unity menu MikanXR / Build Shader Bundle
)
echo Installed MikanXR_VNyan into "%VNYAN%\Items\Assemblies"
exit /b 0
