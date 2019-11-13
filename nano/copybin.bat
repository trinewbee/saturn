@echo off
set cpcmd=xcopy /d/y/q
set tag=..\..\shared\cs

echo Deploy components for PC
for %%n in (
  Nano.Common Nano.Extensive
  Nano.Sockets Nano.Test
) do (
  for %%p in (net40 net45 net472 netcoreapp2.2 netcoreapp3.0) do (
    %cpcmd% %%n\bin\Release\%%p\%%n.dll %tag%\%%p\
    %cpcmd% %%n\bin\Release\%%p\%%n.pdb %tag%\%%p\
  )
)

echo Deploy components for PC (Net 40 only)
for %%n in (Nano.Forms Nano.Mysql Nano.Obsolete Nano.Rsa Nano.Win32 Nano.Xapi) do (
    %cpcmd% %%n\bin\Release\%%n.dll %tag%\net40\
    %cpcmd% %%n\bin\Release\%%n.pdb %tag%\net40\
)

echo Deploy components for Android
for %%n in (Nano.Common Nano.Extensive Nano.Xapi Nano.Sockets) do (
  %cpcmd% android\%%n\bin\Release\%%n.dll %tag%\android
  %cpcmd% android\%%n\bin\Release\%%n.pdb %tag%\android
)

pause