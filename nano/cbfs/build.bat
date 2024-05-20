@echo off
dotnet build Nano.Cbfs.sln -c Debug -p:Platform=x86
pause
dotnet build Nano.Cbfs.sln -c Debug -p:Platform=x64
pause
