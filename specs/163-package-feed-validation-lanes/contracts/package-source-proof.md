# Contract: Package Source Proof

## Purpose

Prove that selected package-consuming samples resolve `FS.GG.UI.*` packages from the configured
local feed only, while third-party packages continue to resolve from approved external sources.

## Source Rules

Generated proof config must include package source mapping equivalent to:

```text
source: nuget-local
  path: ~/.local/share/nuget-local/
  patterns:
    - FS.GG.UI.*

source: nuget.org
  url: https://api.nuget.org/v3/index.json
  patterns:
    - *
```

The generated config may include additional approved third-party sources only when they are
recorded in evidence.

## Proof Execution

Default proof:

- Creates or uses a dedicated isolated `NUGET_PACKAGES` cache.
- Does not clear global NuGet caches.
- Writes generated NuGet config under the evidence directory.
- Runs restore for selected samples with the generated config and isolated cache.
- Records package source rules, local feed path, cache path, restore command, restore log, and
  package assets paths.

Cold proof:

- Requires explicit `--cold`.
- May clear global caches only when `--clear-global-cache` is also supplied.
- Records whether global caches were cleared and which command did it.

## Evidence Files

```text
package-proof/
|-- package-versions.md
|-- package-pins.md
|-- source-rules.nuget.config
|-- source-proof.md
|-- source-proof.json
|-- restore.log
`-- assets/
    `-- <sample-id>-project.assets.json
```

## `source-proof.json` Fields

- `status`: `passed`, `failed`, or `environment-limited`.
- `feedPath`.
- `cachePath`.
- `globalCacheCleared`.
- `selectedSamples`.
- `currentPackages`.
- `packagePins`.
- `sourceRules`.
- `resolvedPackages`.
- `violations`.
- `restoreCommand`.
- `restoreLog`.
- `assetsFiles`.
- `generatedAtUtc`.

## Acceptance Rules

Source proof passes only when:

- All selected `FS.GG.UI.*` pins are current or have accepted compatibility exceptions.
- Every expected `FS.GG.UI.*` id/version is present in the local feed.
- Restore succeeds with the generated source rules.
- Every restored `FS.GG.UI.*` package is allowed only by the local-feed rule.
- No `FS.GG.UI.*` package resolves from an external source or an unapproved cache condition.
- Third-party packages resolve only from approved external sources.

## Failure Classification

- `stale-pin`: Selected sample declares a stale `FS.GG.UI.*` version.
- `missing-local-package`: Expected id/version is absent from the local feed.
- `source-violation`: `FS.GG.UI.*` can resolve from a non-local source.
- `restore-failed`: Restore command failed before source proof could complete.
- `assets-unreadable`: Restore assets were missing or unreadable.
- `cache-policy-violation`: Proof used global cache clearing without explicit cold mode.
- `no-selected-samples`: No package-consuming samples were selected.
- `no-package-pins`: A selected sample has no `FS.GG.UI.*` package references and cannot satisfy
  package-consuming proof.
