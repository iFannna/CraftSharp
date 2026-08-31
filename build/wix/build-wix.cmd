@echo off
chcp 65001 >nul
setlocal

echo ========================================
echo  CraftSharp MSI Installer Builder
echo  WiX Toolset v7
echo ========================================

:: 获取项目根目录（build/wix 的上两级目录）
set "PROJECT_ROOT=%~dp0..\.."
cd /d "%PROJECT_ROOT%"

:: 从 csproj 提取版本号
for /f "tokens=3 delims=<>" %%a in ('findstr "<Version>" CraftSharp.csproj ^| findstr /v "AssemblyVersion FileVersion PackageReference"') do (
    set "APP_VERSION=%%a"
    goto :got_version
)
:got_version

if not defined APP_VERSION (
    echo ERROR: Cannot extract version from CraftSharp.csproj
    exit /b 1
)

echo Version: %APP_VERSION%
echo.

:: Step 1: Publish
echo [1/3] Publishing .NET project...
dotnet publish CraftSharp.csproj -c Release -r win-x64 --self-contained -o publish
if %errorlevel% neq 0 (
    echo ERROR: dotnet publish failed
    exit /b 1
)
echo.

:: Step 2: Build zh-CN MSI
echo [2/3] Building zh-CN MSI...
wix build -ext WixToolset.UI.wixext ^
    build\wix\CraftSharp.wxs ^
    -loc build\wix\CraftSharp.zh-CN.wxl ^
    -define Version=%APP_VERSION% ^
    -bindpath PublishDir=%PROJECT_ROOT%\publish ^
    -bindpath ProjectRoot=%PROJECT_ROOT% ^
    -o installer\CraftSharp_%APP_VERSION%_Windows_x64_zh-CN.msi ^
    -pdbtype none

if %errorlevel% neq 0 (
    echo ERROR: zh-CN MSI build failed
    exit /b 1
)
echo.

:: Step 3: Build en-US MSI
echo [3/3] Building en-US MSI...
wix build -ext WixToolset.UI.wixext ^
    build\wix\CraftSharp.wxs ^
    -loc build\wix\CraftSharp.en-US.wxl ^
    -define Version=%APP_VERSION% ^
    -bindpath PublishDir=%PROJECT_ROOT%\publish ^
    -bindpath ProjectRoot=%PROJECT_ROOT% ^
    -o installer\CraftSharp_%APP_VERSION%_Windows_x64_en-US.msi ^
    -pdbtype none

if %errorlevel% neq 0 (
    echo ERROR: en-US MSI build failed
    exit /b 1
)
echo.

echo ========================================
echo  Build complete!
echo  - installer\CraftSharp_%APP_VERSION%_Windows_x64_zh-CN.msi
echo  - installer\CraftSharp_%APP_VERSION%_Windows_x64_en-US.msi
echo ========================================

endlocal
