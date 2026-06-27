# Implementation Plan: Refresh fs-gg-ui Template to Current Scene API

**Branch**: `201-refresh-template-scene-api` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/201-refresh-template-scene-api/spec.md`

## Summary

Bring the `fs-gg-ui` product template's seed code (`template/base/src/Product/*.fs`), its bundled
API-surface reference (`template/base/docs/api-surface/Scene`), and its single version pin
(`FsSkiaUiVersion` in `template/base/Directory.Packages.props`) into agreement with the FS.GG.UI
Scene/Controls/Viewer API as published by this repository's local package feed. The deliverable is a
template that, when scaffolded at any profile, restores, builds, and emits its scene/evidence with
zero API-drift errors.

**Approach**: detect-then-conform. Pack the local feed at the current source version, then for each
profile *generate* a product (which resolves the `//#if (profile == …)` template-engine guards),
restore it against the feed, build it, and run its evidence/governance checks. The build/evidence
output is the authoritative drift signal: every compile error or evidence failure attributable to the
Scene/Controls/Viewer API is a concrete edit to make in the seed source or bundled docs. Re-pin
`FsSkiaUiVersion` to exactly the version the feed produced, and confirm no stale literal remains.

> **Standing assumption — drift hypotheses are unverified until a product is generated and built.**
> The diff between `template/base/docs/api-surface/Scene/Scene.fsi` (a flattened full-surface
> snapshot) and the split live `src/Scene/*.fsi` files is **not** a reliable drift signal on its own —
> the two are organized differently by design. The only trustworthy evidence of drift is a real
> per-profile *generate → restore → build → evidence* run against a freshly packed feed. `/speckit-tasks`
> MUST schedule that run as the first Foundational step (before any seed-code edit), and every edit
> must be justified by an observed compiler/evidence failure, not by the raw `.fsi` diff.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`); template scaffolding via the dotnet template engine; build orchestration via `build.fsx` (FSI script, no FAKE).

**Primary Dependencies**: `FS.GG.UI.*` packages consumed from the local feed at `~/.local/share/nuget-local/` — Scene, Color, SkillSupport, Build (all profiles); SkiaViewer, Elmish (app/sample-pack); KeyboardInput, Layout, Controls, Controls.Elmish, DesignSystem, Themes.Default (app); Testing (governed). All pinned through the single `$(FsSkiaUiVersion)`.

**Storage**: N/A (source/template files only).

**Testing**: `template/base/tests/Product.Tests/GovernanceTests.fs` (text/structure assertions, profile-guarded); `build.fsx target Verify` (Dev + GeneratedGuidanceCheck + TemplateDrift + EvidenceGraph + EvidenceAudit + Test); the generated product's evidence CLI (`--scene-evidence`, `--layout-evidence`, and app-profile launch/image/screenshot commands). Verification is per-profile against a freshly generated product, not against `template/base` in place.

**Target Platform**: Linux desktop (SkiaSharp over OpenGL); headless-capable for the governed/headless-scene profiles and for evidence commands.

**Project Type**: Maintenance of a product-scaffolding template that lives inside the FS.GG.Rendering library/runtime repo. The seed product is an executable (`Product.fsproj`, `OutputType=Exe`) with no `.fsi` files — it is application code, not a packaged public surface.

**Performance Goals**: N/A — correctness/conformance task, no runtime performance target.

**Constraints**:
- `template/base` is **not directly compilable**: each seed `.fs` carries both a profile branch and its `//#else` counterpart delimited by template-engine `//#if`/`//#else`/`//#endif` comment directives. Validation MUST go through `dotnet new` generation (which strips the inactive branch) before building.
- `FsSkiaUiVersion` MUST remain the only FS.GG.UI version literal (constitution/feature-064 invariant; asserted by GovernanceTests).
- Bundled `docs/api-surface/**` is `copyOnly` in `template.json` (no `sourceName` substitution); edits there must preserve framework identifiers verbatim.
- Scope is API/version conformance only — no new product features, no profile changes, no unrelated refactors (FR-009).

**Scale/Scope**: 6 seed files (`Model.fs`, `View.fs`, `LayoutEvidence.fs`, `EvidenceCommands.fs`, `Program.fs`, `WindowOptions.fs`) across up to two profile branches each; 1 version pin; the bundled Scene api-surface; 4 profiles (`app`, `headless-scene`, `governed`, `sample-pack`). The seed touches Scene **and** Controls/Viewer/Elmish/Layout/KeyboardInput/DesignSystem surfaces, so "current Scene API" is read as "the current FS.GG.UI surface the seed consumes," with Scene as the named centerpiece.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ Pass (N/A shape) | No new public library surface is designed here; the FS.GG.UI API is the fixed *target*, not under change. "Tests" map to the template's existing governance + per-profile build/evidence, run before (red: drift) and after (green: conformant). |
| II. Visibility lives in `.fsi` | ✅ Pass | The seed product is an `Exe` with no `.fsi`; no library `.fsi` is added or modified. No `private`/`internal`/`public` modifiers introduced. |
| III. Idiomatic simplicity | ✅ Pass | Edits replace drifted API usage with the current idiom; no new operators/SRTP/reflection/CEs introduced. Any unavoidable complexity (e.g. a renamed-construct shim) is disclosed at the use site. |
| IV. Elmish/MVU boundary | ✅ Pass | The seed already models its state through MVU (`init`/`update`/`subscriptions`, `AdapterCommand`); the refresh preserves that boundary and only conforms type/constructor usage. |
| V. Test evidence is mandatory | ✅ Pass | Verification is real: generate each profile, restore against the packed feed, build, and run governance + evidence. Synthetic fallbacks already present in the seed (e.g. `writeFallbackPngEvidence`, marked `// SYNTHETIC`) are preserved with their disclosure; none are added. |
| VI. Observability and safe failure | ✅ Pass | No change to the seed's diagnostic/evidence reporting except where an API rename forces it; behavior preserved (FR-009). |

**Change classification**: Tier 2 (maintenance/conformance; no public framework API change, no new capability). No gate violations — Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/201-refresh-template-scene-api/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output — drift-detection method + decisions
├── data-model.md        # Phase 1 output — entities (seed file, pin, profile, drift item)
├── quickstart.md        # Phase 1 output — generate→restore→build→evidence per profile
├── contracts/           # Phase 1 output — conformance & evidence contracts
│   ├── api-surface-conformance.md
│   └── generated-product-evidence.md
├── checklists/
│   └── requirements.md  # Spec quality checklist (already created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
# Template under refresh (the work happens here)
template/base/
├── Directory.Packages.props            # FsSkiaUiVersion pin (FR-003/004) + per-profile FS.GG.UI.* pins
├── build.fsx                           # Verify target; resolves engine from FsSkiaUiVersion at runtime
├── src/Product/
│   ├── Model.fs                        # headless branch + app/sample-pack branch (Scene + Controls/Elmish/…)
│   ├── View.fs                         # Scene Group/Text/Rectangle (headless) + typed Controls front door (app)
│   ├── LayoutEvidence.fs               # LayoutEvidenceReport / Scene types
│   ├── EvidenceCommands.fs             # SceneEvidence/Viewer evidence CLI (both branches)
│   ├── Program.fs                      # entry point + host wiring per profile
│   └── WindowOptions.fs                # app/sample-pack only
├── docs/api-surface/Scene/Scene.fsi    # bundled Scene reference (FR-005) — copyOnly
└── tests/Product.Tests/GovernanceTests.fs  # profile-guarded governance assertions (FR-008)

# Authoritative current surface (the target — read-only here)
src/Scene/*.fsi                         # Types/Scene/Evidence/Inspection/TextShaping/SceneCodec/Animation
src/Controls/, src/SkiaViewer/, …       # other surfaces the app-profile seed consumes

# Tooling (used to pack/validate — not edited)
.template.config/template.json          # `dotnet new fs-gg-ui --profile <p>`; profile symbol + copyOnly
scripts/refresh-local-feed-and-samples.fsx  # packs FS.GG.UI.* to ~/.local/share/nuget-local/
tools/Rendering.Harness/ (package-feed)      # the packer + sample-pin classifier
Directory.Build.props                   # repo source <Version> — the version the feed packs at
```

**Structure Decision**: All edits are confined to `template/base/` (seed source, version pin, bundled Scene doc). `src/**` is the read-only conformance target. Packing/scaffolding tooling (`.template.config/`, `scripts/`, `tools/Rendering.Harness/`) is used as-is to produce the feed and generate products for validation, and is not modified.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
