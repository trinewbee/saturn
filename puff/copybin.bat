set cpcmd=xcopy /d/y/q
set tag=..\..\shared\cs

for %%n in (PuffServer) do (
  for %%p in (net48 net6.0 net8.0) do (
    %cpcmd% %%n\bin\Release\%%p\%%n.dll %tag%\%%p\
    %cpcmd% %%n\bin\Release\%%p\%%n.pdb %tag%\%%p\
  )
)

for %%n in (PuffNetCore) do (
  for %%p in (net6.0 net8.0) do (
    %cpcmd% %%n\bin\Release\%%p\%%n.dll %tag%\%%p\
    %cpcmd% %%n\bin\Release\%%p\%%n.pdb %tag%\%%p\
  )
)

pause