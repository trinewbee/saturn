set src=E:\code\csc\files\BackupCloud\BackupConsole\bin\Debug\net472
set cpcmd=xcopy /d/y/q
%cpcmd% %src%\*.dll .
%cpcmd% %src%\BackupConsole.exe .
pause