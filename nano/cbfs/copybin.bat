@echo off
set cpcmd=xcopy /d/y/q
set cfg=Debug
set tag=..\..\..\shared\cs

for %%n in (Nano.Cbfs) do (
  for %%p in (net40 net472) do (
    echo %cpcmd% %%n\bin\x86\%cfg%\%%p\%%n.* %tag%\%%p\x86\
    %cpcmd% %%n\bin\x86\%cfg%\%%p\%%n.* %tag%\%%p\x86\
    echo %cpcmd% %%n\bin\x64\%cfg%\%%p\%%n.* %tag%\%%p\x64\
    %cpcmd% %%n\bin\x64\%cfg%\%%p\%%n.* %tag%\%%p\x64\
  )
)

pause