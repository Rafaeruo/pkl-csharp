#!/bin/bash

set -e

echo "🥒 Building..."

dotnet build --configuration Release

echo "🥒 Packing nuget..."

dotnet pack Pkl --configuration Release --no-build --no-restore -o artifacts
