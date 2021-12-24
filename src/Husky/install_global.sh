#!/bin/sh
dotnet build
dotnet pack -c Release
dotnet tool install -g --no-cache --add-source ./nupkg/ --framework net6.0 --version 0.0.3 husky
