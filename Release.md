# Release Process

This project uses `dev` as the integration branch and `master` for production releases, with [GitVersion](https://gitversion.net/) (Mainline mode) for semantic versioning. NuGet publishing is handled by GitHub Actions on push to `master`.

## Branch Overview

| Branch | Purpose |
|---|---|
| `master` | Production releases. Pushes here trigger NuGet publish. |
| `dev` | Default/integration branch for the next release. |
| `feature/*` | Feature branches off `dev`. |

## Versioning Notes

- Tags are **`v`-prefixed**: `vX.Y.Z` (e.g. `v7.2.0`), unlike some sibling projects that use bare `X.Y.Z` tags.
- GitVersion runs in **Mainline** mode (`GitVersion.yml`): the version is derived from the most recent tag on `master`, incrementing per merge. Tagging the release commit `vX.Y.Z` pins the published package version to `X.Y.Z`.
- The csproj also carries a hardcoded `<PackageVersion>` used for local builds. CI overrides it via `-p:PackageVersion=<GitVersion output>`, but keep it in sync with the release so local packs match.

## Release Steps

### 1. Ensure `dev` is ready

- All feature branches for the release are merged into `dev`.
- CI is green on `dev`.

### 2. Finalize the release on `dev`

Make the release-only changes in `src/Serilog.Sinks.MongoDB/Serilog.Sinks.MongoDB.csproj`:

- **Update `PackageVersion`** to the new `X.Y.Z`.
- **Update `PackageReleaseNotes`** with a one-line summary of the release (see existing format, e.g. `v7.2 - ...`).
- **Update `TargetFrameworks`** if adding/dropping TFMs.
- **Update `Description`** if the library's capabilities have changed.

Then update the docs:

- **Update `CHANGES.md`** with a new section at the top using the existing format:

  ```markdown
  ## X.Y.Z (YYYY-MM-DD)
   * **BREAKING**: ... (if any)
   * Fixed: ... ([#NN](https://github.com/ChangemakerStudios/serilog-sinks-mongodb/issues/NN)) - Thanks to [@contributor](https://github.com/contributor)!
   * Added: ...
   * Updated: ...
  ```

- **Update `README.md`** with any new/changed usage examples.

Build and run tests locally:

```bash
dotnet build
dotnet test
```

Commit and push to `dev`, and confirm CI is green.

### 3. Merge `dev` into `master` and tag

```bash
git checkout master
git pull origin master
git merge --no-ff dev
git tag vX.Y.Z
git push origin master --tags
```

This push triggers the GitHub Actions workflow (`.github/workflows/deploy.yml`) which will:

1. Run tests.
2. Calculate the package version via GitVersion (Mainline mode).
3. Pack the NuGet package (with `snupkg` symbols).
4. Publish `Serilog.Sinks.MongoDB` to nuget.org.

### 4. Sync `dev` with `master`

```bash
git checkout dev
git merge master
git push origin dev
```

### 5. Create a GitHub Release

- Go to **Releases** > **Draft a new release**.
- Choose the `vX.Y.Z` tag.
- Title: `vX.Y.Z`
- Body: Copy the corresponding section from `CHANGES.md`.
- Publish.

## Hotfix Process

For critical fixes to a released version:

```bash
git checkout master
git checkout -b hotfix/X.Y.Z+1
# make fix, update PackageVersion/PackageReleaseNotes/CHANGES.md, commit
git checkout master
git merge --no-ff hotfix/X.Y.Z+1
git tag vX.Y.Z+1
git push origin master --tags

git checkout dev
git merge master
git push origin dev

git branch -d hotfix/X.Y.Z+1
```

## CI/CD Details

- **Workflow**: `.github/workflows/deploy.yml`
- **Triggers**: All pushes and pull requests (build + test). NuGet publish only on push to `master`.
- **Versioning**: GitVersion with `Mainline` mode (`GitVersion.yml`), seeded by `vX.Y.Z` tags.
- **NuGet API key**: Stored as `NUGETKEY` repository secret.
- **Tests**: `dotnet test` runs directly on the CI runner (no external MongoDB container required).
