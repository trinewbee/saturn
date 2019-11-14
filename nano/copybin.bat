@echo off
set cpcmd=xcopy /d/y/q
set tag=..\..\shared\cs

rem Deploy components for PC
for %%n in (
  Nano.Common Nano.Extensive
  Nano.Sockets Nano.Test
) do (
  echo Project %%n
  for %%p in (net40 net45 net472 netcoreapp2.2 netcoreapp3.0) do (
    %cpcmd% %%n\bin\Release\%%p\%%n.dll %tag%\%%p\
    %cpcmd% %%n\bin\Release\%%p\%%n.pdb %tag%\%%p\
  )
)

rem Deploy components for PC / Forms
for %%n in (Nano.Forms) do (
  echo Project %%n
  for %%p in (net40 net45 net472 netcoreapp3.0) do (
    %cpcmd% %%n\bin\Release\%%p\%%n.dll %tag%\%%p\
    %cpcmd% %%n\bin\Release\%%p\%%n.pdb %tag%\%%p\
  )
)

rem Deploy components for PC (Net 40 only)
for %%n in (Nano.Mysql Nano.Obsolete Nano.Rsa Nano.Win32 Nano.Xapi) do (
  echo Project %%n
  %cpcmd% %%n\bin\Release\%%n.dll %tag%\net40\
  %cpcmd% %%n\bin\Release\%%n.pdb %tag%\net40\
)

rem Deploy components for Android
for %%n in (Nano.Common Nano.Extensive Nano.Xapi Nano.Sockets) do (
  echo Project %%n for Android
  %cpcmd% android\%%n\bin\Release\%%n.dll %tag%\android
  %cpcmd% android\%%n\bin\Release\%%n.pdb %tag%\android
)

pause