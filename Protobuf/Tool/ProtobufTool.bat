@echo off
REM 声明采用UTF-8编码
chcp 65001

::proto目录
set protoFile=D:\project\yuki-uitest\Protobuf\ProtobufFile
::导出代码目录
set protocsFileName=D:\project\yuki-uitest\Assets\Runtime\App\Protobuf
::protoc.exe
set protocEXE=D:\project\yuki-uitest\Protobuf\Protoc\protoc.exe

echo proto目录:   		%protoFile%
echo 导出代码目录:		%protocsFileName%
echo protoc插件目录:  	%protocEXE%
echo.
echo -------------------------------------文件完整性检测开始--------------------------------------------
echo.
if not exist %protocEXE% (
    echo "protoc.exe丢失"
    pause
    exit /b
) 

if not exist %protoFile% (
    echo "proto文件夹不存在"
    pause
    exit /b
) 

if not exist %protocsFileName% (
    md %protocsFileName%
    echo "导出文件夹已创建"
)
echo 文件检测正常
echo.
echo -------------------------------------文件完整性检测结束--------------------------------------------
echo.
echo -------------------------------------Proto代码导出开始---------------------------------------------
echo.

:: 遍历proto目录及其子目录中的所有.proto文件
for /r "%protoFile%" %%i in (*.proto) do (
    REM 获取文件的相对路径
    setlocal enabledelayedexpansion
    set file=%%i
    set relPath=!file:%protoFile%=!
    
    REM 目标文件夹路径
    set targetDir=%protocsFileName%!relPath!\..\

    REM 创建目标文件夹
    if not exist "!targetDir!" (
        md "!targetDir!"
        echo 创建文件夹：!targetDir!
    )

    REM 执行protoc导出
    %protocEXE% --proto_path=%protoFile% --csharp_out=!targetDir! "%%i"
    
    if errorlevel 1 (
        echo 导出失败：%%i
    ) else (
        echo 已完成导出Proto文件：%%i
    )

    endlocal
)

echo.
echo -------------------------------------Proto代码导出结束---------------------------------------------
echo 导出目录：%protocsFileName%
pause
