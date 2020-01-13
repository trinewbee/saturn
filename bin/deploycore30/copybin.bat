set src=E:\code\csc\files\BackupCloud\BackupConsole\bin\Debug\netcoreapp3.0
set cpcmd=xcopy /d/y/q
%cpcmd% %src%\*.dll .
%cpcmd% %src%\BackupConsole.exe .
%cpcmd% %src%\BackupConsole.runtimeconfig.json .
pause