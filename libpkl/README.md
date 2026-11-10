# libpkl build recipe

[libpkl](https://pkl-lang.org/main/current/libpkl/index.html) is the C library, added in Pkl 0.33, that lets a
binding call into Pkl in-process instead of spawning the `pkl` CLI. The Pkl project doesn't publish it yet, so
this folder holds a recipe for building it from source. **No binaries are committed.**

- `VERSION` is the `apple/pkl` tag to build.
- `build.sh` builds for macOS and Linux (set `PKL_MUSL=1` for Alpine/musl, which needs Pkl's musl and static zlib setup first).
- `build.ps1` builds for Windows.
- `.github/workflows/build-libpkl.yml` runs the build for every supported platform and uploads the results.

Output goes to `artifacts/libpkl/<rid>/` (`include/pkl.h`, `lib/libpkl.*`, licence files, and `PKL_VERSION`),
where `<rid>` is a .NET runtime identifier: `osx-arm64`, `linux-x64`, `linux-arm64`, `win-x64`.

Builds are slow and memory hungry (GraalVM native-image), and cross-compilation isn't supported, so each platform
must be built on that platform. Pkl doesn't build macOS x64.

When Pkl starts publishing libpkl, replace the build in the scripts and workflow with a download of the release
archive and delete this recipe.
