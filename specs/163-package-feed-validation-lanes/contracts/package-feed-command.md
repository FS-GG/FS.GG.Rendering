# Contract: Package Feed Command

## Entry Points

Maintainer-friendly script:

```bash
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode check
```

Canonical harness command exposed by the script:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  package-feed --sample samples/AntShowcase --mode check
```

The script may build or invoke the harness, but the observable arguments and evidence below are
the stable contract.

## Arguments

- `--sample <path>`: Selected package-consuming sample root. May be repeated.
- `--mode check|refresh|proof`: Operation mode. Default is `check`.
- `--feed <path>`: Local feed path. Default is `~/.local/share/nuget-local/`.
- `--out <path>`: Evidence output directory.
- `--pack`: Pack current source projects to the local feed before checking.
- `--isolated-cache <path>`: NuGet package cache for proof mode.
- `--cold`: Use cold-proof behavior.
- `--clear-global-cache`: Destructive cache clear. Valid only with `--cold`.
- `--allow-exception <id>`: Accept a documented compatibility exception.

## Modes

### `check`

Discovers current packable `FS.GG.UI.*` package versions and selected sample package references.
Fails before restore/build/test when any selected sample has a stale pin without an accepted
compatibility exception.

### `refresh`

Performs `check`, updates selected sample `FS.GG.UI.*` pins to the discovered expected versions,
and writes changed file evidence. It does not silently create compatibility exceptions.

### `proof`

Performs `check`, verifies expected packages exist in the local feed, restores selected samples
with the source rules from [Package Source Proof](package-source-proof.md), and writes proof
evidence.

## Exit Codes

- `0`: Operation passed.
- `1`: Validation failed, for example stale pin, missing expected feed package, restore failure, or
  source violation.
- `2`: Usage error.
- `3`: Infrastructure error, for example unreadable project file or NuGet invocation failure that
  cannot be classified as validation failure.

## Required Output

The command prints and writes:

- Current `FS.GG.UI.*` package ids, versions, and project paths.
- Selected sample roots and project files inspected.
- For each selected package pin: package id, sample path, declared version, expected version, and
  status.
- Local feed path and expected package file status.
- For refresh mode: files changed and before/after versions.
- For proof mode: package cache path, generated NuGet config path, package source rules, restore
  command, resolved package versions, source violations, and logs.

## Failure Requirements

Stale pin failures must include:

- Package id.
- Expected version.
- Actual declared version.
- Sample project path.
- Compatibility exception status.

Source violation failures must include:

- Package id.
- Resolved version.
- Violating source key/path/URL when known.
- Expected source rule.
- Restore log path.
