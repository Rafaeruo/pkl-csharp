# Builds libpkl for Windows (x64) from the apple/pkl source at the tag in libpkl/VERSION.
#
# libpkl isn't published by the Pkl project yet, so we build it ourselves. Once it is published, replace
# this script with a download of the release archive.
#
# Requirements: git, Java 21+ (25 is what Pkl's CI uses), and the MSVC toolchain on PATH (run it from a
# "x64 Native Tools" prompt, or use ilammy/msvc-dev-cmd in CI).
#
# Usage: libpkl/build.ps1 [-OutDir <dir>]   (default: artifacts/libpkl)
# Env:   PKL_SRC_DIR  reuse an existing checkout of apple/pkl instead of cloning (it must be at the right tag)

param([string]$OutDir)

$ErrorActionPreference = 'Stop'

$here = $PSScriptRoot
$repoRoot = Split-Path $here -Parent
$version = (Get-Content (Join-Path $here 'VERSION') -Raw).Trim()
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'artifacts/libpkl' }
$srcDir = if ($env:PKL_SRC_DIR) { $env:PKL_SRC_DIR } else { Join-Path $repoRoot "obj/pkl-src/$version" }

$pklTarget = 'windows-amd64'
$rid = 'win-x64'

if (-not (Test-Path (Join-Path $srcDir '.git'))) {
    Write-Host "🥒 Cloning apple/pkl $version..."
    New-Item -ItemType Directory -Force -Path (Split-Path $srcDir -Parent) | Out-Null
    git clone --depth 1 --branch $version https://github.com/apple/pkl.git $srcDir
    if ($LASTEXITCODE -ne 0) { throw 'git clone failed' }
}

Write-Host "🥒 Building libpkl $version for $pklTarget (this takes a while)..."
Push-Location $srcDir
try {
    & ./gradlew.bat --no-daemon libpkl:buildNative
    if ($LASTEXITCODE -ne 0) { throw 'gradle build failed' }
} finally {
    Pop-Location
}

$built = Join-Path $srcDir "libpkl/build/native-libs/$pklTarget"
if (-not (Test-Path (Join-Path $built 'lib'))) { throw "Expected build output at $built but it doesn't exist." }

$dest = Join-Path $OutDir $rid
Write-Host "🥒 Collecting output into $dest..."
if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item -Recurse -Force (Join-Path $built '*') $dest
Set-Content -NoNewline -Path (Join-Path $dest 'PKL_VERSION') -Value $version

Write-Host "Built libpkl $version for ${rid}:"
Get-ChildItem -Recurse -File $dest | ForEach-Object { '{0,12:N0}  {1}' -f $_.Length, $_.FullName }
