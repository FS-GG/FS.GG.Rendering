# Contract: skill body + gate-enforced registration

**Feature**: `248-grid-line-drawing` | **Date**: 2026-07-05

## `template/product-skills/fs-gg-line-drawing/SKILL.md`

Frontmatter `name: fs-gg-line-drawing` + a one-line `description`. Body sections (modeled on
`fs-gg-visibility/SKILL.md`): Scope; Public Contract (the reused `Cell`/`Pathfinding` surfaces + the
product-owned `LineDrawing.fs`); the grid model; the Bresenham line; the supercover variant; line-of-sight;
Applications (tile LOS, beams, drawing walls/roads, movement along a line); the adaptable helper; Common
pitfalls (float-lerp drift, thin-line diagonal gaps for sight, `Cell` vs `Point`, re-rolled `(row,col)`);
Build/Test/Evidence/Package-boundary/Generated-product/Related/Sources. Materializes for `game` and
`sample-pack`.

## Gate-enforced registry touch-points (the coherent set)

| File | Edit | Condition / value |
|------|------|-------------------|
| `scripts/generate-skill-manifest.fsx` | add `fs-gg-line-drawing` to `catalog` | source `template/product-skills/fs-gg-line-drawing/SKILL.md`, condition `(profile == "game" \|\| profile == "sample-pack")` |
| `template/skill-manifest/skill-manifest.json` | REGEN via the script | new id + sha256 + materializes-when + supplied-by |
| `.template.config/template.json` | 2 gated sources | skill → `.agents/skills/fs-gg-line-drawing/` (`copyOnly`); fragment `source: template/fragments/line-drawing/src/` `target: src/` (NO copyOnly) — both gated `(profile == "game" \|\| profile == "sample-pack")` |
| `template/base/src/Product/Product.fsproj` | add Compile item | `<Compile Include="LineDrawing.fs" Condition="Exists('LineDrawing.fs')" />` in the game/sample-pack block, before `Model.fs` |
| `tests/Package.Tests/Feature231SkillManifestTests.fs` | add to `canonicalSources` | `("fs-gg-line-drawing", "template/product-skills/fs-gg-line-drawing/SKILL.md")` |
| `tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs` | add to `canonicalSources` | same tuple + materializes-when condition |
| `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` | count bump | framework product-skill `16 → 17` |
| `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` | add to `game` + `sample-pack` sets | `fs-gg-line-drawing` (sample-pack `11 → 12`, game `15 → 16`) |
| `scripts/validate-lifecycle-template.fsx` | count bump | `frameworkChecked = 16 → 17` |
| `.agents/skills/fs-gg-product-line-drawing/SKILL.md` | NEW wrapper | Codex-active thin wrapper, points at canonical body |
| `.claude/skills/fs-gg-product-line-drawing/SKILL.md` | NEW wrapper | Claude-active thin wrapper (differs only by that one line) |
| `docs/reports/skills-parity.md` | REGEN | `dotnet run --project tools/Rendering.Harness -- skill-parity` → 0 findings |
| `template/base/docs/scaffold-map.md` | classify replaceable | `LineDrawing.fs` (game / sample-pack) adaptable |
| `template/product-skills/fs-gg-model-swap/SKILL.md` | add to Replaceable list | `LineDrawing.fs` (game / sample-pack) — retriggers manifest sha256 |

## NOT touched (matches the collision/visibility precedent)

- `template/capabilities.yml` — only package/fragment-backed *package* capabilities.
- `template/base/docs/skillist-reference.md` — a curated subset.
- No `.agents/skills/fs-gg-line-drawing/` canonical dev root — 247 committed only the two
  `fs-gg-product-*` wrappers, not a canonical `.agents` root.
