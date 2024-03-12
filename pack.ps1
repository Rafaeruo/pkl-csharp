Write-Output "🥒 Building..."

& dotnet build --configuration Release

Write-Output "🥒 Packing nuget..."

& dotnet pack Pkl --configuration Release --no-build --no-restore -o artifacts