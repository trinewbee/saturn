call build.bat Release

set cpcmd=xcopy /d/y/q
set tag=..\..\..\shared\cs

echo Deploy components for PC
for %%n in (
  Nano.Common Nano.Extensive Nano.Forms Nano.Mysql Nano.Rsa Nano.Xapi Nano.Cloud
  Nano.Sockets Nano.Test Nano.Win32
) do (
  %cpcmd% %%n\bin\Release\%%n.dll %tag%\pc
  %cpcmd% %%n\bin\Release\%%n.pdb %tag%\pc
)

echo Deploy components for Android
for %%n in (Nano.Common Nano.Extensive Nano.Xapi Nano.Sockets) do (
  %cpcmd% android\%%n\bin\Release\%%n.dll %tag%\android
  %cpcmd% android\%%n\bin\Release\%%n.pdb %tag%\android
)

echo Deploy CBFS binaries
for %%n in (x86 x64) do (
  %cpcmd% cbfs\Nano.Cbfs\bin\%%n\Release\Nano.Cbfs.dll %tag%\pc\cbfs\net40-%%n
  %cpcmd% cbfs\Nano.Cbfs\bin\%%n\Release\Nano.Cbfs.pdb %tag%\pc\cbfs\net40-%%n
)

pause
