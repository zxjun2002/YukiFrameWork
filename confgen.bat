@echo off
setlocal enabledelayedexpansion
chcp 65001 >NUL

REM 切到脚本所在目录（项目根：与 Assets 同级）
cd /d "%~dp0"

REM ====== 路径映射（对应你的 ResEditorConfig）======
REM ResEditorConfig.CSV_Path = "/../Configs"  -> 项目根下的 .\Configs
set INPUT=.\Configs

REM ResEditorConfig.ConfData_Path = "/ConfigData/Data/ConfData.cs"
set OUT_CS=.\Assets\ConfigData\Data\ConfData.cs

REM ResEditorConfig.Racast_Path = "/ConfigData/RacastSet/"
set OUT_RACAST=.\Assets\ConfigData\RacastSet

REM ResEditorConfig.ConfsAsset_Path = "Assets/ConfigData/Configs/ConfData.byte"
set OUT_BYTES=.\Assets\ConfigData\Configs\ConfData.byte
set OUT_MANIFEST=.\Assets\ConfigData\Configs\ConfManifest.json
REM ================================================

REM ★ 新增总开关：是否生成 Manifest（0=不生成，1=生成）
set EMIT_MANIFEST=0

echo [ConfGen] 确认/安装 yuki-confgen ...
dotnet tool update -g zxjun2002.confgen.tool >NUL 2>&1
IF ERRORLEVEL 1 dotnet tool install -g zxjun2002.confgen.tool

REM 优先用绝对路径，避免 PATH 未刷新
set TOOL=%USERPROFILE%\.dotnet\tools\yuki-confgen.exe
IF NOT EXIST "%TOOL%" set TOOL=yuki-confgen

echo [ConfGen] 生成代码与 RacastSet ...
"%TOOL%" gen --input "%INPUT%" --out-cs "%OUT_CS%" --out-racast "%OUT_RACAST%"
IF ERRORLEVEL 1 goto :fail

echo [ConfGen] 生成 .byte 与清单 ...
REM ★ 根据开关决定是否写 Manifest，并使用无 csproj 的直建（传入 --confdata）
set NO_MANIFEST_SWITCH=
IF "%EMIT_MANIFEST%"=="0" set NO_MANIFEST_SWITCH=--no-manifest

"%TOOL%" build --input "%INPUT%" --out-bytes "%OUT_BYTES%" --out-manifest "%OUT_MANIFEST%" --confdata "%OUT_CS%" %NO_MANIFEST_SWITCH%
IF ERRORLEVEL 1 goto :fail

echo [ConfGen] 完成。
if /I "%~1"=="--nopause" goto :eof
pause
goto :eof

:fail
echo [ConfGen] 失败，错误码 %ERRORLEVEL%
if /I "%~1"=="--nopause" exit /b %ERRORLEVEL%
pause
exit /b %ERRORLEVEL%