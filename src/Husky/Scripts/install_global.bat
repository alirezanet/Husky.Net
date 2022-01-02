dotnet pack -c Release ../Husky.csproj
dotnet tool install --global --no-cache --add-source ..\nupkg\ husky
