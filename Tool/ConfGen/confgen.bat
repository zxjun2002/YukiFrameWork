@echo off
setlocal enabledelayedexpansion
chcp 65001 >NUL

REM ====== 路径映射（对应你的 ResEditorConfig）======
set INPUT=D:\project\yuki-uitest\Configs
set OUT_DIR_GEN=D:\project\yuki-uitest\Assets\ConfigData
set OUT_DIR_BYTES=D:\project\yuki-uitest\Assets\ConfigData\Configs
REM ================================================

REM 新增：控制生成端，默认 C（客户端）。可在命令行传 S/ALL/NONE 覆盖
set "SIDE=C"
if /I "%~1"=="C"    set "SIDE=C"
if /I "%~1"=="S"    set "SIDE=S"
if /I "%~1"=="ALL"  set "SIDE=ALL"
if /I "%~1"=="NONE" set "SIDE=NONE"

echo [ConfGen] 确认/安装 yuki-confgen ...
dotnet tool update -g zxjun2002.confgen.tool >NUL 2>&1
IF ERRORLEVEL 1 dotnet tool install -g zxjun2002.confgen.tool

REM 优先用绝对路径，避免 PATH 未刷新
set TOOL=%USERPROFILE%\.dotnet\tools\yuki-confgen.exe
IF NOT EXIST "%TOOL%" set TOOL=yuki-confgen

echo [ConfGen] 生成代码与 RacastSet (side=%SIDE%) ...
"%TOOL%" gen --input "%INPUT%" --out-dir "%OUT_DIR_GEN%" --side %SIDE%
IF ERRORLEVEL 1 goto :fail

echo [ConfGen] 生成 .bytes ...
"%TOOL%" build --input "%INPUT%" --out-dir "%OUT_DIR_BYTES%" --confdata-dir "%OUT_DIR_GEN%"
IF ERRORLEVEL 1 goto :fail

echo [ConfGen] 完成。
REM 支持把 --nopause 放到第二个参数（第一个参数用来传 SIDE）
if /I "%~1"=="--nopause" goto :eof
if /I "%~2"=="--nopause" goto :eof
pause
goto :eof

:fail
echo [ConfGen] 失败，错误码 %ERRORLEVEL%
if /I "%~1"=="--nopause" exit /b %ERRORLEVEL%
if /I "%~2"=="--nopause" exit /b %ERRORLEVEL%
pause
exit /b %ERRORLEVEL%
