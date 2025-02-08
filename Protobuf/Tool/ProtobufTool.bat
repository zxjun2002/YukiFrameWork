::google.protobuf导出脚本

@echo off 
REM 声明采用UTF-8编码
chcp 65001

::proto目录
set protoFile=..\ProtobufFile
::导出代码目录
set protocsFileName=..\..\Assets\Runtime\App\Protobuf
::protoc.exe
set protocEXE=..\Protoc\protoc.exe


echo proto目录:   		%protoFile%
echo 导出代码目录:		%protocsFileName%
echo protoc插件目录:  	%protocEXE%
echo.
echo -------------------------------------文件完整性检测开始--------------------------------------------
echo.
if not exist %protoFile% (
	echo "protoc.exe丢失"
	pause
) 

if not exist %protoFile% (
	echo "proto文件夹不存在"
	pause
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
for /f "delims=" %%i in ('dir /b "%protoFile%"') do (
	%protocEXE% --proto_path=%protoFile% %%i --csharp_out=%protocsFileName%
	echo 已完成导出Proto文件：%%i
)
echo.
echo -------------------------------------Proto代码导出结束---------------------------------------------
echo 导出目录：%protocsFileName%
pause