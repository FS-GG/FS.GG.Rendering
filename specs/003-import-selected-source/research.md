# Phase 0 Research: Import Selected Source (Stage R4)

Source inspected at commit `f759f399` (2026-06-14). This stage imports real code, so
research = establishing the source's actual build shape, backend, dependency order, and the
deltas needed for constitution compliance.

## Finding 1 — The source is ALREADY OpenGL (no Vulkan port needed)

**Decision**: Treat FR-005/SC-005 as **satisfied by the source**; the task is *cleanup* of
vestigial Vulkan naming, not a backend port.

**Evidence**:
- `Directory.Packages.props` references `Silk.NET.OpenGL` 2.23.0; **no `Silk.NET.Vulkan`**.
- `src/SkiaViewer/Host/OpenGl.fs`/`.fsi` is the host; `GRGlInterface`/GL throughout
  `SceneRenderer.fs`; **no `GRVkBackend`/`GRVkInterface`/`VkInstance`**.
- The 11 remaining `Vulkan` mentions are vestigial: comments ("GL successor to the former
  VulkanStartup"), a `ViewerBackendPreference.Vulkan` enum case that returns *"Vulkan backend
  is no longer supported; this viewer host presents through OpenGL (feature 119)"*, and stale
  doc-comment wording ("no live Vulkan window required").

**Rationale**: A prior earlier-session note assumed the old repo was Vulkan; the source has
since migrated to GL upstream. Importing it needs no port — only removal/rewrite of the dead
`Vulkan` enum case and stale comments so SC-005 ("no remaining Vulkan dependency") reads true.

**Alternatives considered**: A Vulkan→GL port (rejected — there is nothing to port).

> **Memory correction**: the project-overview note "the old repo used Vulkan" is outdated;
> the source presents through OpenGL as of feature 119.

## Finding 2 — Build configuration

**Decision**: Mirror the source build config, adapting only ownership metadata.

- Target `net10.0`, `LangVersion latest` (`Directory.Build.props`).
- Central package management (`Directory.Packages.props`): SkiaSharp `4.147.0-preview.3.1`
  (+ `NativeAssets.Linux`/`Win32`) — preview, **already explicitly pinned** (satisfies the
  constitution's preview-pin rule); `Silk.NET.OpenGL`/`Input`/`Windowing` `2.23.0`;
  `Elmish` `5.0.2`.
- No `global.json` SDK pin → rely on the installed net10 SDK.
- `Version` `0.1.0-preview.1`; `Authors` "FS-Skia-UI Contributors" → adapt to FS.GG.

**Alternatives considered**: Re-pinning SkiaSharp to a stable release (rejected — no stable
net10 SkiaSharp at this version line; keep the working preview pin).

## Finding 3 — Dependency order (import/build sequence)

From `ProjectReference`s:

```
Scene            (no refs)
Color            → Scene
Layout           → Scene
KeyboardInput    → Scene
Testing          → Scene
SkiaViewer       → KeyboardInput, Scene
Input            → Scene, SkiaViewer
Elmish           → Scene, SkiaViewer
Controls         → Scene, Layout, KeyboardInput
Controls.Elmish  → Controls, KeyboardInput, SkiaViewer
```

**Decision**: Import (and validate-build) in topological tiers:
1. `Scene` → 2. `Color`/`Layout`/`KeyboardInput`/`Testing` → 3. `SkiaViewer` →
4. `Input`/`Elmish` → 5. `Controls` → 6. `Controls.Elmish`. Build after each tier to localize
failures.

**Rationale**: A 28k-LOC import that fails to compile is hard to debug at once; tiered
import+build localizes errors to the smallest new surface.

## Finding 4 — The design-system / theme layers already exist as modules

**Decision**: Honor the four UI layers at the **module** level inside `Controls`; do **not**
project-split during import.

**Evidence**: `Controls/` already contains `DesignTokens.fs/.fsi`, `Theme.fs/.fsi`,
`Style.fs/.fsi`, and `design-tokens.tokens.json` as distinct modules; controls
(`Control`, `DataGrid`, `TextInput`, `Charts`, …) are separate.

**Rationale**: The R2 layering rule (one control set, many themes) is already structurally
honored by these modules. A project-level split is risky mid-import and unnecessary now; keep
the layer distinction at module/namespace level and leave a project split as a later,
separately-justified change (matches the lightweight working style).

**Alternatives considered**: Split `Controls` into `DesignSystem`/`Themes`/`Kits` projects now
(rejected — high churn/risk during import, no current benefit).

## Finding 5 — What is NOT imported

**Decision**: Exclude per R2/R3 — `src/SkillSupport`, `tests/Governance.Tests`,
`tests/SkillSupport.Tests`, `readiness/`, `docs/testSpecs`, and old feature-workflow artifacts.
Rewrite any imported reference to retired governance assumptions.

## Open item — package identity (verify at import)

`Directory.Build.props` does not set `PackageId`/`RootNamespace`; the `FS.Skia.UI.*` identity
must be located (per-project property or a props pattern) and preserved (FR-010). If the
source emits bare assembly-name packages, record the actual identity and keep it unchanged —
the rebrand is deferred to Stage R8 regardless.

## Out of scope (explicit deferrals)

- The rendering test harness → Stage R5 (Testing helpers + `captureScreenshotEvidence` seams
  are imported here as product source, but no harness project/CLI is built).
- Wiring tests into CI at chosen frequencies → Stage R6.
- Any package rebrand → Stage R8.
