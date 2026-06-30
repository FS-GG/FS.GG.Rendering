# In-scope file inventory (T003)

Read-only confirmation of the files this feature touches/validates. Captured 2026-06-30.

## Shipped consumer docs (EDIT targets)

| File | Exists | Role |
|---|---|---|
| `template/base/docs/skillist-reference.md` | ✅ | Catalog of skill ids → paths; today lists defunct ids + false provenance header. |
| `template/base/docs/scaffold-map.md` | ✅ | Prose scaffold map; today names dangling skills in inline code-spans (`fs-gg-typed-controls`, `fs-gg-controls-host`, `fs-gg-viewer-host`). |

## Product skills under `template/product-skills/` (resolution surface)

All 7 present, each with a `name:` matching its directory:

| Directory | `name:` |
|---|---|
| `fs-gg-elmish` | `fs-gg-elmish` |
| `fs-gg-keyboard-input` | `fs-gg-keyboard-input` |
| `fs-gg-scene` | `fs-gg-scene` |
| `fs-gg-skiaviewer` | `fs-gg-skiaviewer` |
| `fs-gg-symbology` | `fs-gg-symbology` |
| `fs-gg-testing` | `fs-gg-testing` |
| `fs-gg-ui-widgets` | `fs-gg-ui-widgets` |

## Check home / reuse

- `tools/Rendering.Harness/SkillParity.fs` — reused for discovery (`discoverDefaultSurfaces`,
  `inventorySkills`, `parseFrontMatter`, `defaultRequest`). Builds clean.
- `tests/Package.Tests/Package.Tests.fsproj` — new test home (`Feature224SkillCatalogCurrencyTests.fs`).
  Builds clean in Release; restores (lockfile-opt-out).
- Evidence root: `specs/224-skill-catalog-currency/readiness/` (gitignored by default — allowlist added before staging).
