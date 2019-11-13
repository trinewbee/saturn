@echo off

if [%1] == [] (set cfg=Debug) else set cfg=%1
echo Building Configure %cfg%

rem ---------- Using MSBuild instead of Devenv ----------

rem set devenv="C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\DevEnv.exe"
rem %devenv% Nano.All.sln /build Release
rem %devenv% cbfs\Nano.Cbfs\Nano.Cbfs.csproj /build "Release|x86"
rem %devenv% cbfs\Nano.Cbfs\Nano.Cbfs.csproj /build "Release|x64"

rem ---------- Using MSBuild instead of Devenv ----------

rem set msb="C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"
rem set msb="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe"
set msb="C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
set msbout=out\gen\msb
if not exist %msbout% mkdir %msbout%

echo Building Nano Basic
%msb% Nano.All.sln /t:Build /p:Configuration=%cfg% > %msbout%\nano.log
findstr "Error" %msbout%\nano.log

for %%n in (Nano.Sockets) do (
  echo Buiding %%n
  %msb% %%n\%%n.sln /t:Build /p:Configuration=%cfg% > %msbout%\%%n.log
  findstr "Error" %msbout%\%%n.log
)

echo Building Nano.Cbfs (x86)
%msb% cbfs\Nano.Cbfs\Nano.Cbfs.csproj /t:Build /p:Configuration=%cfg% /p:Platform=x86 > %msbout%\cbfs-x86.log
findstr "Error" %msbout%\cbfs-x86.log

echo Building Nano.Cbfs (x64)
%msb% cbfs\Nano.Cbfs\Nano.Cbfs.csproj /t:Build /p:Configuration=%cfg% /p:Platform=x64 > %msbout%\cbfs-x64.log
findstr "Error" %msbout%\cbfs-x64.log

rem ---------- Copy binaries ----------

if not exist out\debug\x86 mkdir out\debug\x86
if not exist out\debug\x64 mkdir out\debug\x64
if not exist out\release\x86 mkdir out\release\x86
if not exist out\release\x64 mkdir out\release\x64

set cpcmd=xcopy /d/y/q
set binout=out\%cfg%

for %%n in (Nano.Common Nano.Extensive Nano.Forms Nano.Mysql Nano.Rsa Nano.Xapi Nano.Sockets) do (
  %cpcmd% %%n\bin\%cfg%\%%n.dll %binout%
)

for %%n in (x86 x64) do (
  %cpcmd% cbfs\Nano.Cbfs\bin\%%n\%cfg%\Nano.Cbfs.dll %binout%\%%n
)
