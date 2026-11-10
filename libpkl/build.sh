#!/bin/bash
# Builds libpkl for the current OS/architecture from the apple/pkl source at the tag in libpkl/VERSION.
#
# libpkl isn't published by the Pkl project yet, so we build it ourselves. Once it is published, replace
# this script with a download of the release archive.
#
# Requirements: git, Java 21+ (25 is what Pkl's CI uses), a C compiler, and zlib headers.
# Cross-compilation is not supported, so run this on the platform you want the library for.
#
# Usage: libpkl/build.sh [output-dir]   (default: artifacts/libpkl)
# Env:   PKL_SRC_DIR  reuse an existing checkout of apple/pkl instead of cloning (it must be at the right tag)

set -euo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd "$here/.." && pwd)
version=$(tr -d '[:space:]' < "$here/VERSION")
out_dir=${1:-$repo_root/artifacts/libpkl}
src_dir=${PKL_SRC_DIR:-$repo_root/obj/pkl-src/$version}

case "$(uname -s)" in
  Darwin) os=macos ;;
  Linux)  os=linux ;;
  *) echo "Unsupported OS $(uname -s). Use libpkl/build.ps1 on Windows." >&2; exit 1 ;;
esac

case "$(uname -m)" in
  arm64|aarch64) arch=aarch64; arch_rid=arm64 ;;
  x86_64|amd64)  arch=amd64;   arch_rid=x64 ;;
  *) echo "Unsupported architecture $(uname -m)" >&2; exit 1 ;;
esac

# Pkl's own name for the target (the folder under libpkl/build/native-libs) and the .NET runtime identifier.
if [ "$os" = linux ] && [ -n "${PKL_MUSL:-}" ]; then
  pkl_target=alpine-linux-$arch
  rid=linux-musl-$arch_rid
  gradle_args=(-Dpkl.musl=true)
else
  pkl_target=$os-$arch
  gradle_args=()
  case "$os" in
    macos) rid=osx-$arch_rid ;;
    linux) rid=linux-$arch_rid ;;
  esac
fi

if [ ! -d "$src_dir/.git" ]; then
  echo "🥒 Cloning apple/pkl $version..."
  mkdir -p "$(dirname "$src_dir")"
  git clone --depth 1 --branch "$version" https://github.com/apple/pkl.git "$src_dir"
fi

echo "🥒 Building libpkl $version for $pkl_target (this takes a while)..."
(cd "$src_dir" && ./gradlew --no-daemon ${gradle_args[@]+"${gradle_args[@]}"} libpkl:buildNative)

built=$src_dir/libpkl/build/native-libs/$pkl_target
if [ ! -d "$built/lib" ]; then
  echo "Expected build output at $built but it doesn't exist." >&2
  exit 1
fi

echo "🥒 Collecting output into $out_dir/$rid..."
rm -rf "$out_dir/$rid"
mkdir -p "$out_dir/$rid"
cp -R "$built/." "$out_dir/$rid/"
echo "$version" > "$out_dir/$rid/PKL_VERSION"

echo
echo "Built libpkl $version for $rid:"
find "$out_dir/$rid" -type f -exec ls -lh {} +
