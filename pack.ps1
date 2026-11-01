Write-Output "🥒 Building..."

& dotnet build --configuration Release

Write-Output "🥒 Packing nuget..."

& dotnet pack Pkl --configuration Release --no-build --no-restore -o artifacts
& dotnet pack Microsoft.Extensions.Configuration.Pkl --configuration Release --no-build --no-restore -o artifacts
& dotnet pack PklGenerator --configuration Release --no-build --no-restore -o artifacts
