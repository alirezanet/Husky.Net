#!/bin/sh
dotnet build ../Husky.csproj
dotnet pack -c Release ../Husky.csproj
dotnet tool install -g --no-cache --add-source ../nupkg/ --framework net6.0 --version 0.0.3 husky
