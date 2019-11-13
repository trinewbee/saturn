set bin=..\..\bin\tool\SyncCode
for %%n in (Nano.Common Nano.Extensive Nano.Xapi Nano.Sockets) do (
  %bin% ..\%%n %%n
)
pause
