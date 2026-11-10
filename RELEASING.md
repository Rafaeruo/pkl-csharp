# Releasing

A release is made by pushing a tag. The [Release workflow](.github/workflows/release.yml) builds, runs the tests
against a real Pkl, packs everything and creates the GitHub release and pushes the packages to nuget.org using trusted publishing (no API key is stored).

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
4. When the workflow has succeeded, check the packages appeared on nuget.org.

The workflow runs for tags that look like `1.2.3`. It refuses to continue unless the tag is exactly the version in
`Directory.Build.props` (no `v` prefix) and the tagged commit is on `master`.

## What the workflow produces

The GitHub release gets these files attached:

- The three NuGet packages and their symbol packages (`.nupkg` and `.snupkg`). These are the exact packages the
  workflow built after the tests passed.
- The Pkl package that provides the `@CSharpType` annotation. It has to be there: its URL is
  `https://github.com/Rafaeruo/pkl-csharp/releases/download/<version>/pkl-csharp@<version>`.

## Pushing to NuGet

The workflow logs in to nuget.org with `NuGet/login` as `jeremyvaartjes` (the user who created the policy, which is owned by the `PklCommunity` organisation), using the trusted publishing
policy configured on nuget.org for `release.yml`, then pushes the `.nupkg` files (and their `.snupkg` symbol packages)
with `--skip-duplicate`, so re-running is safe.

## If a run fails

Fix the problem and re-run the workflow from the Actions tab (or delete and re-push the tag). Re-running replaces the
release assets.
