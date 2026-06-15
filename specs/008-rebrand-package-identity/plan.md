# Implementation Plan: Rebrand Package Identity (Migration Stage R8)

**Branch**: `008-rebrand-package-identity` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-rebrand-package-identity/spec.md`

## Summary

R8 is the final planned migration stage and the first that changes **product code**. It resolves
the deferred package-identity decision (`0001`) to an accepted **rebrand to `FS.GG.UI.*`** and
applies that rename as one coherent matrix: every runtime module's package ID, root namespace, and
assembly name moves from `FS.Skia.UI.<Module>` to `FS.GG.UI.<Module>`; the `dotnet new` template and
its package move to the `FS.GG.UI.Template` brand (including the user-facing `fs-skia-ui` →
`fs-gg-ui` short name and the `fs-skia-*` skill folders); and the surface baselines, fixtures, docs,
bridge note, and PROVENANCE follow. The product must still build and pass the default-tier validation
set, and the public API surface must differ only by the namespace prefix.

Because the rebrand is a release event, it sequences **publish-before-deprecate**: the new
`FS.GG.UI.*` packages are packed to the local feed first, then a **copy-ready deprecation notice**
mapping each old ID to its replacement is produced as a *recorded action* for the public feed —
never claimed as applied here (Constitution Principle VI). The old `FS.Skia.UI.*` IDs freeze at their
last published version; the new lineage starts at **`0.1.0-preview.1`**.

Only the `FS.Skia.UI.` **brand prefix** is rebranded. Descriptive Skia/SkiaSharp technology
references — the `SkiaViewer` module name, genuine `SkiaSharp`/`Skia` dependency references, and the
descriptive `skia` package tag — are preserved.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.

**Primary Dependencies**: SkiaSharp over OpenGL (Silk.NET windowing/GL/GLFW), Fable.Elmish,
Yoga.Net; Expecto for tests. Unchanged by R8 — this is an identity change, not a dependency change.

**Storage**: N/A. Pack output (local feed) location is `~/.local/share/nuget-local/` per constitution.

**Testing**: Default-tier "Local inner loop" suites (`Color/Scene/Layout/Input/KeyboardInput/Elmish/
Controls/Testing/SkiaViewer/Smoke.Tests` + `Lib.Tests`); CI surface-drift (`tests/surface-baselines`
+ `scripts/refresh-surface-baselines.fsx`) and docs build; release-only `Package.Tests` and template
`Product.Tests` (generated-consumer contract).

**Target Platform**: Linux/dev with live X11/GL (dev-baseline provides a GL context for the
GL-dependent suites).

**Project Type**: F# UI framework (10 packable runtime libraries) plus a `dotnet new` template and
its template package.

**Performance Goals**: N/A — no behavior or hot-path change. The rebrand MUST be behavior-neutral.

**Constraints**:
- Four identity facets move **together** as one matrix (package ID, root namespace, assembly name,
  template identity). A partial state is a failure, not an intermediate success.
- Public API surface unchanged apart from the namespace prefix (verified against `.fsi` + baselines).
- `FS.Skia.UI.` brand prefix only; descriptive Skia/SkiaSharp usage preserved.
- Publish-before-deprecate; old IDs deprecated (not deleted) with a forward pointer.
- No overclaiming: out-of-tree feed/old-repo actions are copy-ready + recorded, never "applied here".

**Scale/Scope**: 10 runtime modules + 1 template package. The `FS.Skia.UI` token appears in ~403
files / ~3707 occurrences repo-wide; the **product-source + active-docs** rename surface is the
in-scope subset. Historical `specs/**`, `docs/imported/**`, and `docs/audit/**` are **out of scope**
(history — see Structure Decision). Resolved decisions (no remaining NEEDS CLARIFICATION): starting
version `0.1.0-preview.1`; brand scope = prefix + user-facing tokens + `fs-skia-*` skill folders.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Constitution rule | R8 status |
|---|---|
| **Package identity** — "any rebrand to `FS.GG.UI.*` is a separate, explicit release decision; publishes replacement packages before deprecating old IDs" | ✅ This *is* that explicit decision (`0001` → accepted). Publish-before-deprecate is the plan's sequencing; old IDs deprecated, not deleted. |
| **Principle VI — Observability / no overclaiming** (and spec's no-overclaim rule) | ✅ Feed publish + old-repo edits delivered as copy-ready content + recorded action, marked not-yet-applied. |
| **Change Classification** — Tier 1 (changes package/inter-project identity contracts) | ✅ Declared Tier 1: full artifact chain (spec/plan, `.fsi` namespace updates, surface-baseline updates, test evidence, docs). |
| **Principle II — visibility in `.fsi`; surface-area baselines validated** | ✅ Baselines re-pointed to the new namespace; surface-drift check confirms only the prefix changed (FR-005). |
| **Principle V — test evidence** | ✅ Build + default-tier suites + surface-drift are the rename's evidence; no assertions weakened. No synthetic evidence introduced. |
| **Principle I — Spec→FSI→tests→impl** | ✅ Identity-only; no new public surface. `.fsi` signatures change only their `namespace` line; behavior and shape unchanged. |
| **Idiomatic simplicity / Elmish boundary / GL backend** | ✅ Untouched — no code logic, dependency, or backend change. |

**Result: PASS — no violations.** Complexity Tracking is empty (nothing to justify).

## Project Structure

### Documentation (this feature)

```text
specs/008-rebrand-package-identity/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — resolved decisions + rename mechanics
├── data-model.md        # Phase 1 — identity entities + the rename matrix
├── quickstart.md        # Phase 1 — runnable validation guide
├── contracts/           # Phase 1
│   ├── rename-matrix.md            # old→new identity for all 10 modules + template
│   ├── surface-invariance.md       # the "only the namespace prefix changed" contract
│   └── deprecation-notice.md       # copy-ready recorded action for the public feed
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

The rename surface, grouped by identity facet. Each group is part of the single coherent matrix.

```text
# Facet 1 — Package ID / AssemblyName / Title / Description brand + Version (per fsproj)
src/{Color,Scene,Layout,Input,KeyboardInput,SkiaViewer,Elmish,Controls,Controls.Elmish,Testing}/*.fsproj
  └─ <PackageId>, <AssemblyName>, <Title>, brand text in <Description>  : FS.Skia.UI.* → FS.GG.UI.*
  └─ <Version> reset to 0.1.0-preview.1  (each fsproj currently overrides with the old 0.1.x-preview.1 lineage)
.template.package/FS.Skia.UI.Template.fsproj → FS.GG.UI.Template.fsproj  (file rename + <PackageId>/<Title>/<Description>)
Directory.Build.props                          (already defaults <Version> to 0.1.0-preview.1 — NO change; the per-fsproj overrides above are what R8 resets)

# Facet 2 — Root namespace (every .fs/.fsi + every `open` in consumers/tests) + runtime brand literals
src/**/*.fsi, src/**/*.fs                       : `namespace FS.Skia.UI.<M>`  → `namespace FS.GG.UI.<M>`
tests/**/*.fs                                   : `open FS.Skia.UI.<M>`        → `open FS.GG.UI.<M>`
src/Elmish/AnimationTick.fs, src/SkiaViewer/SkiaViewer.fs : kebab runtime literal `fs-skia-ui`/`fs-skia-ui-runtime` → `fs-gg-ui`/`fs-gg-ui-runtime`
tests/Elmish.Tests/AnimationTickTests.fs         : mirrored `fs-skia-ui` subId assertion → `fs-gg-ui` (lockstep, not a weakened assertion)

# Facet 3 — Surface-area baselines (filename + fully-qualified contents)
tests/surface-baselines/FS.Skia.UI.<M>.txt → FS.GG.UI.<M>.txt   (9 modules: rename file AND rewrite every FQ type line)
  └─ Color is NOT baseline-tracked (omitted from refresh-surface-baselines.fsx) — verify its invariance by .fsi inspection instead
tests/Package.Tests/{SurfaceAreaTests,PackageApiReference,NameCollisionSafety,GeneratedConsumerValidation}.fs

# Facet 4 — Template identity (dotnet new) + user-facing brand tokens
.template.config/template.json   : identity FS.Skia.UI.Template→FS.GG.UI.Template; packagePrefix default;
                                   name; shortName fs-skia-ui→fs-gg-ui; classifications; skill `source` paths
.template.config/generated/{CLAUDE.md,AGENTS.md,.claude/settings.json}
template/product-skills/fs-skia-*  → template/product-skills/fs-gg-*    (dir rename + cross-refs)
template/base/.{agents,claude}/skills/fs-skia-project → fs-gg-project    (dir rename + cross-refs)
template/base/docs/api-surface/**/*.fsi   : verbatim framework contract surface — FQ FS.Skia.UI.* → FS.GG.UI.*
template/base/**, .template.package/README.md, .github/workflows/release.yml (`dotnet new fs-skia-ui`)

# Facet 5 — Docs / provenance / governance keystone
docs/product/decisions/0001-package-identity.md     : deferred → accepted (rebrand); old→new map; version
docs/product/decisions/0002-template-ownership.md   : template-ID references → FS.GG.UI.Template
docs/bridge/package-identity-migration.md           : "retained/unchanged" → "rebranded at R8" + history scope
docs/bridge/old-repo-redirect.md (Block B)          : "no rename" → deprecation/redirect to new IDs
PROVENANCE.md                                       : R8 rebrand note; import-time mapping scoped as history
README.md, src/*/README.md, src/*/skill/SKILL.md    : brand references

# OUT OF SCOPE (history — left exactly as written)
specs/**            (001–007, 091–103 feature records describe what was true at their stage)
docs/imported/**    (imported source snapshots)
docs/audit/**       (mechanism inventory — historical record)
**/bin/**, **/obj/** (build artifacts)
```

**Structure Decision**: This is the existing repository, not a new layout — R8 edits identity in
place. The rename is organized by the four constitution-named facets (plus the user-facing/kebab
tokens the "full coherence" decision adds, and the docs/provenance closeout). The critical
boundary is **product source + active docs (in scope)** vs. **`specs/**`, `docs/imported/**`,
`docs/audit/**` (history, out of scope)** — historical records keep the identity that was true when
written, exactly as PROVENANCE's import-time mapping is preserved as history. Internal build wiring
is `ProjectReference`-based (not `PackageReference` on `FS.Skia.UI.*`), so renaming package IDs does
**not** break the internal build graph; namespace edits are what the compiler enforces.

## Complexity Tracking

> No Constitution Check violations. Nothing to justify.
