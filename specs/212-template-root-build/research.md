# Phase 0 Research: Root-buildable generated products

All Technical-Context unknowns are design choices (no external NEEDS CLARIFICATION). Each is resolved
below with decision / rationale / alternatives.

## R1 — Root solution format: `.slnx`

- **Decision**: Emit a root `Product.slnx` (the XML solution format). `sourceName=Product` rewrites the
  filename and the project paths to `<Name>.slnx` referencing `src/<Name>/<Name>.fsproj` and
  `tests/<Name>.Tests/<Name>.Tests.fsproj`. Minimal content:
  ```xml
  <Solution>
    <Project Path="src/Product/Product.fsproj" />
    <Project Path="tests/Product.Tests/Product.Tests.fsproj" />
  </Solution>
  ```
- **Rationale**: The issue (FS-GG/FS.GG.Rendering#9) names `.slnx` explicitly. `.slnx` is human-diffable
  XML (no GUID churn like classic `.sln`), supported by the .NET 10 SDK for `dotnet build`/`test`/`run`
  on the containing directory. A single solution at the root lets stock `dotnet build` (no project arg)
  resolve the whole product, which is what SDD's probes invoke. The two project paths use the capitalized
  `Product` segment so `sourceName` rewrites them consistently with the renamed directories/files.
- **Alternatives considered**: Classic `.sln` (rejected — GUID noise, larger diff, the issue specifies
  `.slnx`); no solution + rely on globbing (rejected — `dotnet build <dir>` needs exactly one project or
  solution in the dir; with `src/` + `tests/` subdirs and no root file it errors).
- **Gotcha**: `sourceName=Product` is case-aware and also rewrites lowercase `product`→`<name>` (memory:
  template authoring gotchas). The `.slnx` contains only the capitalized path segment `Product`, so the
  rewrite is correct; `global.json` contains no `product` token.

## R2 — SDK pin: `global.json`

- **Decision**: Emit `global.json` pinning the .NET 10 SDK band, e.g.
  `{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature", "allowPrerelease": false } }`.
  Exact baseline version is an implementation detail; the policy is "pin to the 10.0.x band, roll forward
  to the latest installed feature/patch."
- **Rationale**: Makes the stock root build reproducible on machines whose default SDK differs (the
  6.0.428 + 10.0.301 split on this box is exactly the failure mode). `rollForward: latestFeature` keeps
  builds inside `net10.0` (matching the `Directory.Build.props` `TargetFramework` and CI's
  `dotnet-version: '10.0.x'`) while tolerating patch/feature upgrades, so it neither pins too hard (build
  breaks when 10.0.100 isn't present) nor floats to a different major (silently wrong SDK). On a missing
  band it fails fast with a clear SDK-resolution error rather than building against an unexpected SDK
  (FR-002 / SC-006).
- **Alternatives considered**: No `global.json` (rejected — non-reproducible; default SDK on the box may
  be 6.0.x → resolves to net6 toolchain or fails confusingly); exact pin `10.0.301` + `rollForward:
  disable` (rejected — brittle, breaks on any consumer without that exact patch); `rollForward:
  latestMajor` (rejected — would allow drifting off net10).

## R3 — Verb wrapper ↔ FAKE mapping (`restore|build|test|run|verify|pack`)

- **Decision**: `build.sh`/`build.cmd` are thin shims that invoke `dotnet fsi build.fsx -t <Target>` per
  verb. `build.fsx` gains **pass-through** targets for the build-graph verbs; the governed targets are
  unchanged:
  | Verb | FAKE target | Action |
  |---|---|---|
  | `restore` | `Restore` (new) | `dotnet restore` on the single root `.slnx` |
  | `build` | `Build` (new) | `dotnet build` on the root `.slnx` |
  | `test` | `Test` (existing, **unchanged**) | current `dotnet test tests/Product.Tests/... -m:1 --disable-build-servers` |
  | `run` | `Run` (new) | `dotnet run --project src/<Name>` |
  | `verify` | `Verify` (existing, **unchanged**) | current rich evidence+test path |
  | `pack` | `Pack` (new) | `dotnet pack` on the root `.slnx` (`-c Release`) |
  The new targets locate the single `*.slnx`/`src` project in the working dir (name-agnostic) so the
  wrapper and `build.fsx` need no literal `<Name>`. Unknown/missing verb → print the supported-verb list
  and exit non-zero.
- **Rationale**: Satisfies "verb wrapper delegating to FAKE" (every verb goes through `build.fsx`, the
  single orchestration entry) **and** keeps stock `dotnet build/test/run` working independently via the
  `.slnx`. The two paths build the same `.slnx`, so FR-010 (no divergence) holds by construction.
  `Verify`/`Test` are untouched, honoring "No change to FAKE Verify semantics." Reuses the existing
  `targetFromArgs`/`run` dispatch in `build.fsx` (idiomatic simplicity, Principle III).
- **Alternatives considered**: Wrapper calls stock `dotnet` directly for build-graph verbs and only
  `test`/`verify` go to FAKE (rejected — issue says verbs delegate to FAKE; splitting the entry point is
  less uniform and risks divergence). Re-implement build/test/run inside the wrapper shells (rejected —
  duplicates logic across `.sh`/`.cmd`, drifts from FAKE).

## R4 — Headless `dotnet run` assertion in CI

- **Decision**: The release test asserts `dotnet run --project src/GeneratedProduct -c Release` **exits 0**
  at the product root. No display server / xvfb is required.
- **Rationale**: `Program.main` calls `Viewer.runApp`/`ControlsElmish.runInteractiveApp`; in a headless CI
  host the runtime-capability probe yields `Result.Error { Classification = UnsupportedEnvironment }`,
  which `main` maps to **exit 0** (explicit safe-degrade, Constitution Principle VI) after printing a
  `status=unsupported ...` diagnostic line. So `dotnet run` resolves and executes the entrypoint and exits
  promptly without opening a window or hanging — proving root runnability deterministically. On a real
  display the same command opens the persistent window (out of scope for CI).
- **Alternatives considered**: `xvfb-run dotnet run` (rejected — heavier, and the GL/persistent-window
  path would block until close); pass an evidence subcommand via `tryRunEvidenceCommand` for a fully
  deterministic headless run (kept as a **fallback** if any profile's unsupported-host path ever returns
  non-zero — the live smoke task will confirm exit 0 first). The `run` assertion applies only to runnable
  profiles (`app`); non-runnable profiles (`headless-scene`, `governed`, `sample-pack`) are built/tested
  but not run.

## R5 — Template emit/substitution wiring (`template.json`)

- **Decision**: Add the four new files under the existing `template/base/` source so they are copied with
  default `sourceName` substitution. Ensure they are **not** added to `exclude`/`copyOnly` for that
  source. They are part of the **ungated** product (not the gated `.agents/`+`.claude/` lifecycle set), so
  they ship for `lifecycle ∈ {spec-kit, sdd, none}` and for both `designSystem` values unchanged.
- **Rationale**: `Product.slnx` needs the `Product→<Name>` rewrite (so `copyOnly` would be wrong); the
  shell wrappers and `global.json` are content-neutral but live in the same ungated source. Placing them
  in `template/base/` (not under the lifecycle-gated sub-sources) guarantees FR-008 coverage.
- **Alternatives considered**: A dedicated `sources` block (rejected — unnecessary; the base source
  already targets `./` with substitution). `copyOnly` for the wrappers (harmless but inconsistent —
  leave them under normal substitution since they contain no rewrite-sensitive tokens).

## R6 — Cross-repo contract coherence

- **Decision**: Treat this as a `contract-change` against `fs-gg-ui-template`. As part of resolution,
  update `FS-GG/.github` → `registry/dependencies.yml` + `docs/registry/compatibility.md` to record that
  the template now guarantees a root-buildable product (the surface SDD's composition-acceptance probes
  consume), linking issue FS-GG/FS.GG.Rendering#9 as the tracker. Coordinate via the
  `cross-repo-coordination` skill.
- **Rationale**: ADR-0001 mandates registry coherence for contract changes; SDD (FS-GG/.github#16,
  "uniform build" pillar) depends on stock build/run commands working at the product root. The board item
  is already labeled `contract-change` with `Contract: fs-gg-ui-template`.
- **Alternatives considered**: Ship template-only with no registry note (rejected — violates ADR-0001 and
  leaves SDD's dependency implicit).

## Resolved unknowns summary

| Unknown | Resolution |
|---|---|
| Root solution format | `.slnx`, references src + tests, name-rewritten (R1) |
| SDK reproducibility | `global.json`, 10.0.x band, `rollForward: latestFeature` (R2) |
| Verb→FAKE mapping | thin shim → `build.fsx -t`; new Restore/Build/Run/Pack, Test/Verify frozen (R3) |
| Headless run assertion | assert exit 0 via `UnsupportedEnvironment` degrade; app profile only (R4) |
| Template emit/substitution | ungated `template/base/` source, substitution on, no copyOnly freeze (R5) |
| Contract coherence | registry/compatibility update + tracker link, via coordination skill (R6) |
