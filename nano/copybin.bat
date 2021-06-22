@echo off
set cpcmd=xcopy /d/y/q
set tag=..\..\shared\cs

rem Deploy components for PC
for %%n in (
  Nano.Common Nano.Extensive Nano.Obsolete
  Nano.Sockets Nano.Rsa Nano.Test
) do (
  echo Project %%n
  for %%p in (net48 netcoreapp3.1 net5.0) do (
    %cpcmd% %%n\bin\Release\%%p\%%n.dll %tag%\%%p\
    %cpcmd% %%n\bin\Release\%%p\%%n.pdb %tag%\%%p\
  )
)

rem Deploy components for PC / Forms
for %%n in (Nano.Forms) do (
  echo Project %%n
  for %%p in (net48 netcoreapp3.1) do (
    %cpcmd% %%n\bin\Release\%%p\%%n.dll %tag%\%%p\
    %cpcmd% %%n\bin\Release\%%p\%%n.pdb %tag%\%%p\
  )
)
for %%n in (Nano.Forms) do (
  echo Project %%n-windows
  for %%p in (net5.0) do (
    %cpcmd% %%n\bin\Release\%%p-windows\%%n.dll %tag%\%%p\
    %cpcmd% %%n\bin\Release\%%p-windows\%%n.pdb %tag%\%%p\
  )
)

rem Deploy components for PC (Net 48 only)
for %%n in (Nano.Mysql Nano.Win32 Nano.Xapi) do (
  echo Project %%n
  %cpcmd% %%n\bin\Release\net48\%%n.dll %tag%\net48\
  %cpcmd% %%n\bin\Release\net48\%%n.pdb %tag%\net48\
)

rem Deploy components for Android
for %%n in (Nano.Common Nano.Extensive Nano.Xapi Nano.Sockets) do (
  echo Project %%n for Android
  %cpcmd% android\%%n\bin\Release\%%n.dll %tag%\android
  %cpcmd% android\%%n\bin\Release\%%n.pdb %tag%\android
)

pause