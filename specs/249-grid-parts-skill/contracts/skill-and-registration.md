# Contract: `fs-gg-grids` Skill + Registry Entries

The exact, coherence-gated touch-points for adding the skill. Every entry below must agree or the repo
gates fail. This follows the **shipped** collision (246) / visibility (247) registration reality —
captured in the `adding-a-product-skill-touchpoints` memory — which differs from the 246 contract doc (no
`skillist-reference.md` edit; the `canonicalSources`/count-bump edits **are** required).

## Materialize condition (single source of truth)

`profile ∈ {game, sample-pack}` — expressed as the C-style
`(profile == "game" || profile == "sample-pack")` in `template.json` / `generate-skill-manifest.fsx`,
normalized to `profile in [game, sample-pack]` in the published manifest. The two MUST be semantically
equal (Feature 238).

## 1. Skill body — `template/product-skills/fs-gg-grids/SKILL.md`

Frontmatter (name + description only) and sections mirroring `fs-gg-visibility`:

```markdown
---
name: fs-gg-grids
description: Address the parts of a grid in a generated FS.GG.UI product — faces, edges, and vertices with one canonical coordinate each, the six adjacency conversions, and the pixel mapping, over an adaptable helper you own, reusing Cell/Point/Rect.
---
```

Required sections: `## Scope`, `## Public Contract` (points at the bundled `docs/api-surface/Canvas/*.fsi`
`Cell` and `docs/api-surface/Scene/*.fsi` `Point`/`Rect`, plus the product-owned `src/<ProductDir>/Grids.fs`),
`## The parts of a grid` (faces/edges/vertices; cite Red Blob Games "Parts of a grid"), `## Canonical
coordinates` (one name per part; the `Edge` orientation + col/row scheme and the `Vertex` corner lattice;
cite Red Blob Games "Grid edges"), `## Adjacency conversions` (the six `cellCorners`/`cellEdges`/`edgeCells`/
`edgeVertices`/`vertexCells`/`vertexEdges`; the fixed list order; the round-trip property), `## Pixel
mapping` (`cellRect`/`cellCenter`/`vertexPoint`/`edgeSegment`/`edgeMidpoint`/`cellAt`; `GridSpec`;
non-finite guards), `## Applications` (edge-walls / fences on a boundary, autotiling / marching-squares
over vertices, region borders, cursor snapping via `cellAt`), `## The adaptable helper` (points at
`Grids.fs`, "yours to edit or delete"), `## Common pitfalls` (introducing a look-alike `Cell`/`Point` type
instead of reusing the shared ones; giving an edge two names instead of one canonical coordinate; confusing
edge orientation; off-by-one corner/cell indexing; deleting the file with the `Exists` guard understood),
`## Build Commands`, `## Test Commands`, `## Evidence`, `## Package Boundary`, `## Generated Product`,
`## Persistent problems`, `## Related` (`[[fs-gg-collision]]`, `[[fs-gg-visibility]]`, `[[fs-gg-game-core]]`,
`[[fs-gg-scene]]`, `[[fs-gg-skiaviewer]]`), `## Sources / links` (the two Red Blob Games references:
"Parts of a grid" and "Grid edges").

## 2. Manifest catalog — `scripts/generate-skill-manifest.fsx`

Add to the `catalog` list **alphabetically** (after `fs-gg-game-core`, before `fs-gg-keyboard-input`,
keeping the existing ordering):

```fsharp
"fs-gg-grids", "template/product-skills/fs-gg-grids/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
```

Then regenerate `template/skill-manifest/skill-manifest.json` (the script computes the sha256 and
`supplied-by`). Resulting entry shape:

```json
{
  "id": "fs-gg-grids",
  "scope": "product",
  "sha256": "<computed>",
  "resolvablePath": ".agents/skills/fs-gg-grids/SKILL.md",
  "materializes-when": "profile in [game, sample-pack]",
  "supplied-by": "template/product-skills/fs-gg-grids/"
}
```

## 3. Scaffold sources — `.template.config/template.json`

Two gated entries (place near the visibility sources):

```json
{
  "condition": "(profile == \"game\" || profile == \"sample-pack\")",
  "source": "template/product-skills/fs-gg-grids/",
  "target": ".agents/skills/fs-gg-grids/",
  "copyOnly": ["**/*"],
  "comment": "Feature 249: the fs-gg-grids skill. copyOnly so the body byte-matches its skill-manifest sha256. Gated identically to its materializes-when."
},
{
  "condition": "(profile == \"game\" || profile == \"sample-pack\")",
  "source": "template/fragments/grids/src/",
  "target": "src/",
  "comment": "Feature 249: the import-and-adapt grid-parts helper source. NO copyOnly, so sourceName ('Product') substitution rewrites the namespace AND the Product/ path segment (fileRename) to src/<ProductDir>/Grids.fs. Compiled by the Exists-guarded, profile-gated Compile item in Product.fsproj (delete-safe)."
}
```

> **Fragment target — the Feature 246→247 fix (do not regress).** The fragment source is
> `source: template/fragments/grids/src/`, `target: src/` — **not** `target: src/Product/`. The
> `Product/` segment lives *under* the source root so `fileRename` (sourceName `Product` →
> `<ProductDir>`) rewrites the whole path to `src/<ProductDir>/Grids.fs`. An explicit `target: src/Product/`
> would orphan the file in a literal `src/Product/` directory that never compiles (captured in
> [[fragment-target-sourcename-rename]]). The skill source uses `copyOnly` so the body byte-matches its
> manifest sha256; the fragment source has **no** `copyOnly` so substitution runs.

## 4. Compile item — `template/base/src/Product/Product.fsproj`

Under the existing `(profile == "game" || profile == "sample-pack")` region (with the Canvas ref,
`Collision.fs`, and `Visibility.fs`), before `Model.fs`, add:

```xml
<!--#if (profile == "game" || profile == "sample-pack") -->
<Compile Include="Grids.fs" Condition="Exists('Grids.fs')" />
<!--#endif -->
```

## 5. Gate-enforced coherence edits (the easy-to-miss set)

The **current** framework product-skill count is **16** (visibility, 247, is the 16th on this gate);
grids is the **17th**.

- `tests/Package.Tests/Feature231SkillManifestTests.fs` — add
  `"fs-gg-grids", "template/product-skills/fs-gg-grids/SKILL.md"` to `canonicalSources`.
- `tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs` — add the same to `canonicalSources`.
- `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` — framework product-skill count `16 → 17`
  (the comment names visibility as the 16th; grids is the 17th on the same gate).
- `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` — add `"fs-gg-grids"` to **both** the
  `game` and `sample-pack` `expectedFrameworkSkills` sets (and any `.agents`-only narration/source count).
- `scripts/validate-lifecycle-template.fsx` — `frameworkChecked = 16 → 17`.

## 6. Model-swap + scaffold-map (FR-012 swap-guidance reach)

- `template/product-skills/fs-gg-model-swap/SKILL.md` — add `src/<ProductDir>/Grids.fs` to the
  **"Replaceable — rewrite freely"** list (next to `Collision.fs`/`Visibility.fs`), noting the
  `Exists`-guarded compile item. This edits the model-swap body → **retriggers its manifest sha256 →
  regenerate the manifest.**
- `template/base/docs/scaffold-map.md` — classify `Grids.fs` as replaceable/adaptable (consumer-owned),
  next to the `Collision.fs`/`Visibility.fs` entries, noting it compiles before `Model.fs`.

*(No `skillist-reference.md` edit and no `capabilities.yml` edit — matching the collision/visibility
precedent; verified `fs-gg-collision`/`fs-gg-visibility` are in neither.)*

## 7. Dev roots + wrapper + mirror

- `.agents/skills/fs-gg-grids/SKILL.md` — canonical body, **byte-identical** to the product-skills body —
  and the `.claude/skills/fs-gg-grids/` mirror, both via `template/lifecycle/materialize-skill-roots.fsx`
  (`FsGgMaterializeSkillRoots` target).
- `.agents/skills/fs-gg-product-grids/SKILL.md` (Codex-active) **and**
  `.claude/skills/fs-gg-product-grids/SKILL.md` (Claude-active) — thin wrappers: frontmatter
  `name: fs-gg-product-grids` + the canonical description, body pointing at
  `../../../template/product-skills/fs-gg-grids/SKILL.md`; the two differ only by the active-agent line.
  **Easy to miss** — not in the manifest.
- `scripts/check-agent-skill-parity.fsx` / `dotnet run --project tools/Rendering.Harness -- skill-parity`
  asserts `.claude/skills ≡ .agents/skills` (0 findings); re-commit the regenerated
  `docs/reports/skills-parity.md`.

## Gating assertions (`Feature249GridsSkillTests`)

- Manifest has `fs-gg-grids` with the correct digest and `materializes-when`.
- `template.json` has both sources with a condition semantically equal to the manifest; the fragment source
  is `source template/fragments/grids/src/`, `target src/` (no `copyOnly`).
- Dev root + wrapper + mirror exist and are byte-equal where required.
- A `game`/`sample-pack` render materializes `Grids.fs` (at `src/<ProductDir>/`) + the skill; an
  `app`/`headless-scene` render materializes neither.
