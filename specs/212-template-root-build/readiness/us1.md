# US1 — Stock root build/test/run (Feature 212)

Verified on a really-scaffolded product (`--name Acme`) 2026-06-28. Quickstart Scenario A is green
(see [smoke.md](./smoke.md) for the restore/build/test/run transcript).

## T009 — Reconcile with pre-existing root files (FR-003)

- Exactly **one** `Directory.Build.props` and **one** root `*.slnx` at the product root (no
  duplication/conflict): `Directory.Build.props count: 1`, `root .slnx count: 1`.
- The root build **inherits** `net10.0` + the lockfile policy from the pre-existing
  `Directory.Build.props` (`<TargetFramework>net10.0</TargetFramework>`,
  `RestorePackagesWithLockFile=true`, gated `RestoreLockedMode`). The new `.slnx` adds no TFM/policy
  of its own — it only references the two projects, which inherit the props.
- Per-project lockfiles written by the stock restore: `src/Acme/packages.lock.json`,
  `tests/Acme.Tests/packages.lock.json` — the existing lockfile policy operates unchanged under the
  root build.

## T010 — Name rewrite (FR-001 / AS-5)

- `Acme.slnx` exists at the product root and references `src/Acme/Acme.fsproj` and
  `tests/Acme.Tests/Acme.Tests.fsproj` (sourceName `Product`→`Acme` applied to filename, paths, and
  references). Directories renamed: `src/Acme`, `tests/Acme.Tests`.
- `global.json` carries **no** placeholder token (content-neutral) — emitted verbatim.

## T011 — FR-010 parity (stock set == FAKE set)

- Stock `dotnet build` (no project arg) resolves `Acme.slnx` → builds `{ src/Acme, tests/Acme.Tests }`.
- FAKE `Build` (`dotnet fsi build.fsx -t Build`) shells `dotnet build "Acme.slnx"` → builds the
  **same** `{ src/Acme, tests/Acme.Tests }` (transcript: `Acme -> .../Acme.dll`,
  `Acme.Tests -> .../Acme.Tests.dll`, `Build succeeded.`).
- Parity holds **by construction**: both paths build the single root `.slnx`. No silent divergence.

## T012 — SDK-pin reproducibility (Quickstart C / SC-006)

- Positive: inside the product, `dotnet --version` → `10.0.301`. The `global.json` pin
  (`version 10.0.100`, `rollForward latestFeature`) rolls forward to the installed 10.0.x feature
  band — the net10 toolchain resolves even though a 6.0.428 SDK is also installed.
- Negative (fail-fast): with `global.json` temporarily set to an unresolvable band
  (`version 99.0.100`, `rollForward disable`), `dotnet build` **fails fast** (exit 155):
  `Install the [99.0.100] .NET SDK or update [.../global.json] to match an installed SDK.` —
  a clear SDK-resolution error, **not** a silent wrong-SDK build. `global.json` restored after.

**Checkpoint**: Quickstart Scenario A green on a really-scaffolded product — US1 (MVP) is
independently deliverable.
</content>
