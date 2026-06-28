# Phase 1 Data Model: Root-buildable generated products

This feature has no runtime data entities; its "model" is the set of **emitted build artifacts** and
their relationships/validation rules. Each entity below is a file the template emits into a generated
product (or a wiring rule that governs emission).

## Entity: Root solution (`<Name>.slnx`)

- **Represents**: the product-root file tying the source and test projects together for the stock .NET
  toolchain.
- **Template source**: `template/base/Product.slnx` → emitted as `<Name>.slnx`.
- **Fields / content**:
  - `Project Path` → `src/<Name>/<Name>.fsproj`
  - `Project Path` → `tests/<Name>.Tests/<Name>.Tests.fsproj`
- **Relationships**: references the **product project** and the **test project**; consumed by the **verb
  wrapper** (build/restore/pack targets) and by stock `dotnet build/test`.
- **Validation rules**:
  - Exactly one `.slnx` at the product root (so `dotnet build`/`test` with no project arg resolve it).
  - Referenced project paths exist after `sourceName` rewrite (capitalized `Product` segment only).
  - Project set == the set FAKE builds (FR-010).

## Entity: SDK pin (`global.json`)

- **Represents**: the product-root declaration of the expected .NET SDK band.
- **Template source**: `template/base/global.json` (content-neutral; copied verbatim).
- **Fields / content**: `sdk.version` (10.0.x baseline), `sdk.rollForward = latestFeature`,
  `sdk.allowPrerelease = false`.
- **Relationships**: governs which SDK resolves for the **root solution** build.
- **Validation rules**:
  - Keeps the build inside `net10.0` (consistent with `Directory.Build.props` `TargetFramework`).
  - On a host missing the band, build fails fast with an SDK-resolution error (no silent wrong-SDK build).

## Entity: Build-verb wrapper (`build.sh` / `build.cmd`)

- **Represents**: the uniform product-root entry exposing `restore|build|test|run|verify|pack`.
- **Template source**: `template/base/build.sh`, `template/base/build.cmd` (parity pair, like the existing
  `fake.sh`/`fake.cmd`).
- **Fields / behavior**: maps each verb → `dotnet fsi build.fsx -t <Target>`; unknown/missing verb →
  prints supported verbs + non-zero exit.
- **Relationships**: delegates to **`build.fsx` (FAKE path)**; indirectly drives the **root solution**.
- **Validation rules**:
  - All six verbs available on both shells; equivalent behavior (SC-003).
  - `verify` ≡ FAKE `Verify`; `test` ≡ FAKE `Test` (semantics frozen, FR-007).

## Entity: FAKE build path (`build.fsx`) — extended

- **Represents**: the single rich/governed orchestration script (pre-existing).
- **Template source**: `template/base/build.fsx` (edited).
- **Change**: add pass-through targets `Restore`, `Build`, `Run`, `Pack` that shell to stock `dotnet`
  against the root `.slnx` / `src/<Name>`; **`Test` and `Verify` unchanged**.
- **Relationships**: invoked by the **verb wrapper**; operates on the **root solution**.
- **Validation rules**:
  - New targets locate the single `.slnx` / `src` project name-agnostically (no literal `<Name>`).
  - `Verify` output for a scaffolded product identical before/after (SC-004).

## Entity: Release/instantiation assertion (`release.yml` → `template-product-tests`)

- **Represents**: the release-only regression gate proving root buildability.
- **Source**: `.github/workflows/release.yml`, `template-product-tests` job (edited).
- **Behavior**: after `dotnet new fs-gg-ui --name GeneratedProduct`, run at the product root:
  `dotnet build` (stock), `dotnet test` (stock), `dotnet run --project src/GeneratedProduct` (asserts
  exit 0).
- **Relationships**: exercises **root solution** + **SDK pin** through the real consumer path.
- **Validation rules**:
  - Passes when root build/test/run hold; fails (blocks release) on regression (SC-005).
  - `run` assertion applies to the runnable (`app`) profile.

## Entity: Contract/registry record (`fs-gg-ui-template`)

- **Represents**: the cross-repo contract coherence for the new root-buildable surface.
- **Source**: `FS-GG/.github` → `registry/dependencies.yml` + `docs/registry/compatibility.md` (separate
  repo, via `cross-repo-coordination`).
- **Validation rules**: a `contract-change` resolution updates the registry + compatibility projection and
  links tracker FS-GG/FS.GG.Rendering#9.

## State / lifecycle notes

- All product-side entities are emitted for **every** `profile` and **every** `lifecycle`
  (`spec-kit|sdd|none`), and are **byte-neutral** across `designSystem` (`wcag|ant`). They live in the
  ungated `template/base/` source, not the gated `.agents/`+`.claude/` lifecycle sub-sources.
