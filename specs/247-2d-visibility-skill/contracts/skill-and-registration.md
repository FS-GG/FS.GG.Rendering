# Contract: `fs-gg-visibility` Skill + Registry Entries

The exact, coherence-gated touch-points for adding the skill. Every entry below must agree or the repo
gates fail. This follows the **shipped** collision (246) registration reality — captured in the
`adding-a-product-skill-touchpoints` memory — which differs from the 246 contract doc (no
`skillist-reference.md` edit; the `canonicalSources`/count-bump edits **are** required).

## Materialize condition (single source of truth)

`profile ∈ {game, sample-pack}` — expressed as the C-style
`(profile == "game" || profile == "sample-pack")` in `template.json` / `generate-skill-manifest.fsx`,
normalized to `profile in [game, sample-pack]` in the published manifest. The two MUST be semantically
equal (Feature 238).

## 1. Skill body — `template/product-skills/fs-gg-visibility/SKILL.md`

Frontmatter (name + description only) and sections mirroring `fs-gg-collision`:

```markdown
---
name: fs-gg-visibility
description: Compute 2D visibility in a generated FS.GG.UI product — the angular-sweep visibility polygon (line-of-sight, field-of-view, fog-of-war, 2D lighting) over an adaptable helper you own, reusing Point/SpatialGrid.
---
```

Required sections: `## Scope`, `## Public Contract` (points at the bundled
`docs/api-surface/Scene/*.fsi` `Point`/`Rect`/`Geometry` and `docs/api-surface/Canvas/SpatialGrid.fsi`,
plus the product-owned `src/<ProductDir>/Visibility.fs`), `## The world model` (segments over the shared
`Point`), `## Broad-phase cull` (`SpatialGrid.queryRadius`), `## The angular sweep` (endpoint collection,
cross-product ordering, nearest-hit-per-wedge; cite Red Blob Games), `## The visibility polygon`
(bounded closed ring; determinism note), `## Applications` (line-of-sight via `isVisible`, field-of-view
cone, fog-of-war mask, 2D light/shadow fill), `## The adaptable helper` (points at `Visibility.fs`, "yours
to edit or delete"), `## Common pitfalls` (geometry-clash footgun, `atan2` last-bit non-determinism vs the
cross-product comparator, O(segments) scan without the cull, unbounded rays / forgetting the radius bound,
deleting the file with the `Exists` guard understood), `## Build Commands`, `## Test Commands`,
`## Evidence`, `## Package Boundary`, `## Generated Product`, `## Persistent problems`, `## Related`
(`[[fs-gg-collision]]`, `[[fs-gg-game-core]]`, `[[fs-gg-scene]]`, `[[fs-gg-skiaviewer]]`),
`## Sources / links` (the Red Blob Games visibility article).

## 2. Manifest catalog — `scripts/generate-skill-manifest.fsx`

Add to the `catalog` list (after `fs-gg-ui-widgets`, keeping the existing ordering):

```fsharp
"fs-gg-visibility", "template/product-skills/fs-gg-visibility/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
```

Then regenerate `template/skill-manifest/skill-manifest.json` (the script computes the sha256 and
`supplied-by`). Resulting entry shape:

```json
{
  "id": "fs-gg-visibility",
  "scope": "product",
  "sha256": "<computed>",
  "resolvablePath": ".agents/skills/fs-gg-visibility/SKILL.md",
  "materializes-when": "profile in [game, sample-pack]",
  "supplied-by": "template/product-skills/fs-gg-visibility/"
}
```

## 3. Scaffold sources — `.template.config/template.json`

Two gated entries (place near the collision sources):

```json
{
  "condition": "(profile == \"game\" || profile == \"sample-pack\")",
  "source": "template/product-skills/fs-gg-visibility/",
  "target": ".agents/skills/fs-gg-visibility/",
  "copyOnly": ["**/*"],
  "comment": "Feature 247: the fs-gg-visibility skill. copyOnly so the body byte-matches its skill-manifest sha256. Gated identically to its materializes-when."
},
{
  "condition": "(profile == \"game\" || profile == \"sample-pack\")",
  "source": "template/fragments/visibility/src/Product/",
  "target": "src/Product/",
  "comment": "Feature 247: the import-and-adapt visibility helper source. NO copyOnly, so sourceName ('Product') substitution rewrites the namespace/dir. Compiled by the Exists-guarded, profile-gated Compile item in Product.fsproj (delete-safe)."
}
```

The second entry lets `sourceName` substitution rewrite `Product` → `<ProductDir>`/`<ProductName>` in the
copied `Visibility.fs` (no `copyOnly`, so substitution runs). The skill source uses `copyOnly` so the body
byte-matches its manifest sha256.

## 4. Compile item — `template/base/src/Product/Product.fsproj`

Under the existing `(profile == "game" || profile == "sample-pack")` region (with the Canvas ref and
`Collision.fs`), before `Model.fs`, add:

```xml
<!--#if (profile == "game" || profile == "sample-pack") -->
<Compile Include="Visibility.fs" Condition="Exists('Visibility.fs')" />
<!--#endif -->
```

## 5. Gate-enforced coherence edits (the easy-to-miss set)

- `tests/Package.Tests/Feature231SkillManifestTests.fs` — add
  `"fs-gg-visibility", "template/product-skills/fs-gg-visibility/SKILL.md"` to `canonicalSources`.
- `tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs` — add the same to `canonicalSources`.
- `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` — framework product-skill count `15 → 16`
  (the comment names collision as the 15th; visibility is the 16th on the same gate).
- `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` — add `"fs-gg-visibility"` to **both** the
  `game` and `sample-pack` `expectedFrameworkSkills` sets (and any `.agents`-only source count).
- `scripts/validate-lifecycle-template.fsx` — `frameworkChecked = 15 → 16`.

## 6. Model-swap + scaffold-map (FR-013 swap-guidance reach)

- `template/product-skills/fs-gg-model-swap/SKILL.md` — add `src/<ProductDir>/Visibility.fs` to the
  **"Replaceable — rewrite freely"** list (next to `Collision.fs`), noting the `Exists`-guarded compile
  item. This edits the model-swap body → **retriggers its manifest sha256 → regenerate the manifest.**
- `template/base/docs/scaffold-map.md` — classify `Visibility.fs` as replaceable/adaptable
  (consumer-owned), next to the `Collision.fs` entry, noting it compiles before `Model.fs`.

*(No `skillist-reference.md` edit and no `capabilities.yml` edit — matching the collision precedent;
verified `fs-gg-collision` is in neither.)*

## 7. Dev roots + wrapper + mirror

- `.agents/skills/fs-gg-visibility/SKILL.md` — canonical body, **byte-identical** to the product-skills
  body — and the `.claude/skills/fs-gg-visibility/` mirror, both via
  `template/lifecycle/materialize-skill-roots.fsx` (`FsGgMaterializeSkillRoots` target).
- `.agents/skills/fs-gg-product-visibility/SKILL.md` (Codex-active) **and**
  `.claude/skills/fs-gg-product-visibility/SKILL.md` (Claude-active) — thin wrappers: frontmatter
  `name: fs-gg-product-visibility` + the canonical description, body pointing at
  `../../../template/product-skills/fs-gg-visibility/SKILL.md`; the two differ only by the active-agent
  line. **Easy to miss** — not in the manifest.
- `scripts/check-agent-skill-parity.fsx` / `dotnet run --project tools/Rendering.Harness -- skill-parity`
  asserts `.claude/skills ≡ .agents/skills` (0 findings); re-commit the regenerated
  `docs/reports/skills-parity.md`.

## Gating assertions (`Feature247VisibilitySkillTests`)

- Manifest has `fs-gg-visibility` with the correct digest and `materializes-when`.
- `template.json` has both sources with a condition semantically equal to the manifest.
- Dev root + wrapper + mirror exist and are byte-equal where required.
- A `game`/`sample-pack` render materializes `Visibility.fs` + the skill; an `app`/`headless-scene`
  render materializes neither.
