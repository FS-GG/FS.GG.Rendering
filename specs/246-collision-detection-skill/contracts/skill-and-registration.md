# Contract: `fs-gg-collision` Skill + Registry Entries

The exact, coherence-gated touch-points for adding the skill. Every entry below must agree or the repo
gates fail (`Feature231SkillManifestTests`, `Feature238…`, `Feature246CollisionSkillTests`, skill-parity).

## Materialize condition (single source of truth)

`profile ∈ {game, sample-pack}` — expressed as the C-style
`(profile == "game" || profile == "sample-pack")` in `template.json`, normalized to
`profile in [game, sample-pack]` in the published manifest. The two MUST be semantically equal
(Feature 238).

## 1. Skill body — `template/product-skills/fs-gg-collision/SKILL.md`

Frontmatter (name + description only) and sections mirroring `fs-gg-game-core`:

```markdown
---
name: fs-gg-collision
description: Detect and resolve collisions in a generated FS.GG.UI product — broad-phase over SpatialGrid, narrow-phase over Geometry, and an adaptable response layer you own.
---
```

Required sections: `## Scope`, `## Public Contract` (points at the bundled
`docs/api-surface/Scene/Scene.fsi` `Geometry` and `docs/api-surface/Canvas/SpatialGrid.fsi`, plus the
product-owned `src/<ProductDir>/Collision.fs`), `## Detection (narrow-phase)`, `## Broad-phase`,
`## Response` (the editable rule; determinism note), `## The adaptable helper` (points at `Collision.fs`,
says "yours to edit or delete"), `## Common pitfalls` (geometry-clash footgun, O(n²) scan, float-tie in
ordering, deleting the file without the `Exists` guard understood), `## Build Commands`,
`## Test Commands`, `## Evidence`, `## Package Boundary`, `## Generated Product`, `## Persistent problems`,
`## Related` (`[[fs-gg-game-core]]`, `[[fs-gg-scene]]`, `[[fs-gg-skiaviewer]]`), `## Sources / links`.

## 2. Manifest catalog — `scripts/generate-skill-manifest.fsx`

Add to the `catalog` list (alphabetical by id, so between `fs-gg-audio` and `fs-gg-elmish`):

```fsharp
"fs-gg-collision", "template/product-skills/fs-gg-collision/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
```

Then regenerate `template/skill-manifest/skill-manifest.json` (the script computes the sha256 and
`supplied-by`). Resulting entry shape:

```json
{
  "id": "fs-gg-collision",
  "scope": "product",
  "sha256": "<computed>",
  "resolvablePath": ".agents/skills/fs-gg-collision/SKILL.md",
  "materializes-when": "profile in [game, sample-pack]",
  "supplied-by": "template/product-skills/fs-gg-collision/"
}
```

## 3. Scaffold sources — `.template.config/template.json`

Two gated entries (place near the other game/sample-pack skill sources):

```json
{
  "condition": "(profile == \"game\" || profile == \"sample-pack\")",
  "source": "template/product-skills/fs-gg-collision/",
  "target": ".agents/skills/fs-gg-collision/",
  "copyOnly": ["**/*"]
},
{
  "condition": "(profile == \"game\" || profile == \"sample-pack\")",
  "source": "template/fragments/collision/src/Product/",
  "target": "src/Product/"
}
```

The second entry lets `sourceName` substitution rewrite `Product` → `<ProductDir>`/`<ProductName>` in the
copied `Collision.fs` (no `copyOnly`, so substitution runs). The skill source uses `copyOnly` so the body
byte-matches its manifest sha256.

## 4. Compile item — `template/base/src/Product/Product.fsproj`

Under the existing `(profile == "game" || profile == "sample-pack")` region (with the Canvas ref), add:

```xml
<!--#if (profile == "game" || profile == "sample-pack") -->
<Compile Include="Collision.fs" Condition="Exists('Collision.fs')" />
<!--#endif -->
```

## 5. Registry doc — `template/base/docs/skillist-reference.md`

Add the `fs-gg-collision` row to the full-registry catalog (matching the existing row format:
id, materialize condition, one-line purpose).

## 6. game-core trim — `template/product-skills/fs-gg-game-core/SKILL.md`

Replace the body of `## Collision` with a short pointer: "Collision detection and response now have a
dedicated skill — see `[[fs-gg-collision]]`." Keep the `## Culling` / `## Spatial queries` sections
(they are not collision-response). Update `## Related` to add `[[fs-gg-collision]]`. This retriggers the
game-core sha256 → regenerate manifest.

## 7. Dev roots + mirror

`.agents/skills/fs-gg-collision/SKILL.md` (canonical, byte-identical to the product-skills body) and the
`.claude/skills/fs-gg-collision/` mirror via `template/lifecycle/materialize-skill-roots.fsx`
(`FsGgMaterializeSkillRoots` target); `scripts/check-agent-skill-parity.fsx` asserts
`.claude/skills ≡ .agents/skills`.

## Gating assertions (`Feature246CollisionSkillTests`)

- Manifest has `fs-gg-collision` with the correct digest and `materializes-when`.
- `template.json` has both sources with a condition semantically equal to the manifest.
- `skillist-reference.md` lists it; dev root + mirror exist and are byte-equal.
- A `game`/`sample-pack` render materializes `Collision.fs` + the skill; an `app`/`headless-scene`
  render materializes neither.
- `fs-gg-game-core` no longer contains the detailed collision write-up (only a pointer).
