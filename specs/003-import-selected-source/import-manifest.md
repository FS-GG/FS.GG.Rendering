# Import manifest (Stage R4)

What was imported from `EHotwagner/FS-Skia-UI` @ `f759f399`, with disposition. Full path map
and adaptations: [`/PROVENANCE.md`](../../PROVENANCE.md). Format: `contracts/import-manifest.schema.md`.

## Runtime (src/) — import-as-is in dependency tiers

| Tier | Source | Repo | Disposition |
|---|---|---|---|
| 1 | `src/Scene` | `src/Scene` | import-as-is |
| 2 | `src/Color` `src/Layout` `src/KeyboardInput` `src/Testing` | `src/<same>` | import-as-is |
| 3 | `src/SkiaViewer` | `src/SkiaViewer` | import-as-is (GL; Vulkan enum case retained as graceful-degradation) |
| 4 | `src/Input` `src/Elmish` | `src/<same>` | import-as-is |
| 5 | `src/Controls` (incl. `DesignTokens`/`Theme`/`Style`/`Widgets` kit modules) | `src/Controls` | import-as-is (layers honored at module level) |
| 6 | `src/Controls.Elmish` | `src/Controls.Elmish` | import-as-is |

## Tests (tests/)

| Source | Repo | Disposition |
|---|---|---|
| `tests/{Color,Scene,Layout,Input,KeyboardInput,Elmish,SkiaViewer,Testing,Lib,Smoke}.Tests` | `tests/<same>` | import-as-is (local tier) |
| `tests/Controls.Tests` | `tests/Controls.Tests` | adapt (removed `CatalogTests.fs` + `build/Governance` ref) |
| `readiness/surface-baselines/*.txt`, `scripts/refresh-surface-baselines.fsx` | `tests/surface-baselines/`, `scripts/` | import-as-is (CI tier) |
| `tests/Package.Tests` | `tests/Package.Tests` | adapt (release-only; on disk, not in solution — wired at R6) |
| `tests/Governance.Tests`, `tests/SkillSupport.Tests` | — | **exclude** (governance) |
| `tests/Parity.Tests`, `tests/ControlsPreview.Harness` | — | **exclude** (R3 rewrite-pending/deferred → R5) |

## Template / docs / build

| Source | Repo | Disposition |
|---|---|---|
| `template`, `.template.config`, `.template.package` | `template/`, `.template.config/`, `.template.package/` | adapt (governed profile + full-governance fragment removed; residual → R6) |
| `docs/FS.GG/{design-and-controls,rendering-project}.md` | `docs/imported/` | import-as-is |
| `Directory.Build.props`, `Directory.Packages.props` | repo root | adapt (ownership metadata; identity preserved) |

## Excluded

`src/SkillSupport`, `build/**` (incl. `build/Governance/FS.Skia.UI.Build`), `readiness/`,
`docs/testSpecs`, `samples/`, `Container/`, `Mailbox/`, old `specs/**` workflow artifacts — **exclude**.
