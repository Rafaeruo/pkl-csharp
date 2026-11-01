#!/bin/bash
# Builds the pkl-csharp Pkl package (the @CSharpType annotation) for publishing.
# Requires the `pkl` CLI.

set -e

lib_version=$(sed -n 's#.*<VersionPrefix>\(.*\)</VersionPrefix>.*#\1#p' Directory.Build.props)
pkl_version=$(sed -n 's#^ *version = "\(.*\)"#\1#p' PklPackage/PklProject)

if [ "$lib_version" != "$pkl_version" ]; then
  echo "PklPackage/PklProject has version $pkl_version but Directory.Build.props has $lib_version. Make them match." >&2
  exit 1
fi

echo "🥒 Packaging the Pkl package..."

pkl project package PklPackage --output-path artifacts/pkl --skip-publish-check

echo
echo "Attach these files to the GitHub release tagged $lib_version:"
ls artifacts/pkl
