# Releasing

A release is made by pushing a tag. The [Release workflow](.github/workflows/release.yml) builds, runs the tests
against a real Pkl, packs everything and creates the GitHub release. Pushing the packages to NuGet is done by hand.

## Making a release

1. Set the new version in both places (they must match; the workflow fails otherwise):
   - `VersionPrefix` in `Directory.Build.props`
   - `version` in `PklPackage/PklProject`

   Also update the version in the README's install snippets and in the `@CSharpType` package URL.
2. Merge the change to `master`.
3. Tag the merged commit and push the tag:

   ```sh
   git tag <version> && git push origin <version>
   # or with jj
   jj tag set <version> -r master && jj git push --tag <version>
   ```
4. When the workflow has succeeded, push the packages to NuGet (below).

The workflow runs for tags that look like `1.2.3`. It refuses to continue unless the tag is exactly the version in
`Directory.Build.props` (no `v` prefix) and the tagged commit is on `master`.

## What the workflow produces

The GitHub release gets these files attached:

- The three NuGet packages and their symbol packages (`.nupkg` and `.snupkg`). These are the exact packages the
  workflow built after the tests passed.
- The Pkl package that provides the `@CSharpType` annotation. It has to be there: its URL is
  `https://github.com/Rafaeruo/pkl-csharp/releases/download/<version>/pkl-csharp@<version>`.

## Pushing to NuGet

Download the packages from the release and push them with your own API key:

```sh
gh release download <version> --pattern '*.nupkg' --pattern '*.snupkg' --dir /tmp/pkl-csharp-<version>
dotnet nuget push "/tmp/pkl-csharp-<version>/*.nupkg" \
  --api-key <your key> \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

The `.snupkg` symbol package next to each `.nupkg` is pushed along with it. `--skip-duplicate` makes it safe to
repeat if a push is interrupted.

## If a run fails

Fix the problem and re-run the workflow from the Actions tab (or delete and re-push the tag). Re-running replaces the
release assets.
