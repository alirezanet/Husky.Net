dotnet pack -c Release
@echo off
dotnet tool uninstall -g husky
@echo on
dotnet tool install --global --add-source .\nupkg\ husky
husky
