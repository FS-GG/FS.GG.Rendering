# Contract — the 13th skill-manifest entry

**Artifact**: `template/skill-manifest/skill-manifest.json` (`schemaVersion: 1`)
**Producer (sole)**: `scripts/generate-skill-manifest.fsx`
**Gate source**: `.template.config/template.json` `sources[]`
**Consumers**: `Feature231*` / `Feature238*` (in-repo); the cross-repo `registry/skills.yml` generator +
`skill-union-assert.sh` (`.github#164`).

## The entry (generator output — do not hand-edit)

```json
{
  "id": "fs-gg-game-core",
  "scope": "product",
  "sha256": "<computed>",
  "resolvablePath": ".agents/skills/fs-gg-game-core/SKILL.md",
  "materializes-when": "(profile == \"game\" || profile == \"sample-pack\")",
  "supplied-by": "template/product-skills/fs-gg-game-core/"
}
```

## The generator catalog tuple (the only hand edit)

Added to the catalog list in `generate-skill-manifest.fsx`, kept sorted asc by id:

```fsharp
"fs-gg-game-core", "template/product-skills/fs-gg-game-core/SKILL.md",
    "(profile == \"game\" || profile == \"sample-pack\")"
```

## The `template.json` source (the emission gate — must match `materializes-when` verbatim)

```json
{
  "condition": "(profile == \"game\" || profile == \"sample-pack\")",
  "source": "template/product-skills/fs-gg-game-core/",
  "target": ".agents/skills/fs-gg-game-core/",
  "copyOnly": ["**/*"]
}
```

## Invariants (test-enforced)

| # | Invariant | Test |
|---|---|---|
| M1 | manifest has 13 entries, ascending by id | `Feature231SkillManifestTests` |
| M2 | `sha256` == `sha256(SKILL.md text)` | `Feature231SkillManifestTests` |
| M3 | `materializes-when` == the `template.json` source `condition` (no drift) | `Feature238SkillMaterializesWhenTests` |
| M4 | evaluates `true` for `{game}`,`{sample-pack}`; `false` for `{app}`,`{headless-scene}`,`{governed}` | `Feature238` / `Feature219` |
| M5 | `supplied-by` == `template/product-skills/fs-gg-game-core/` | `Feature238` |
| M6 | `game` & `sample-pack` framework-skill rows include `fs-gg-game-core`; product-skill sources count 9→10 | `Feature219EmitFrameworkSkillsTests` |
| M7 | `expectedProductSkillIds` includes `fs-gg-game-core` (9→10) | `Feature225ProductSkillVocabularyTests` |
| M8 | the twelve prior entries are byte-identical to the pre-feature manifest | diff / `--check` |
| M9 | `generate-skill-manifest.fsx --check` reports up-to-date | CI + local |

## Explicitly out of scope

- `registry/skills.yml` shape and the `[missing]`/`[unexpected]` gate (`.github#164`) — this manifest only
  supplies the honest 13-entry input.
