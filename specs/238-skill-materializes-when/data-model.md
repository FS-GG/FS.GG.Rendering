# Phase 1 Data Model — skill-manifest entry (v1 + additive fields)

The manifest is a single JSON document produced only by `scripts/generate-skill-manifest.fsx`. This
feature adds two optional string fields per entry; nothing else about the shape changes.

## Document

```
{
  "schemaVersion": 1,          // unchanged (R1)
  "skills": [ <entry>, ... ]   // sorted by id, ascending (unchanged)
}
```

## Entry

| Field | Type | New? | Meaning | Rule |
|---|---|---|---|---|
| `id` | string | — | skill id, e.g. `fs-gg-project` | matches the catalog; unique; entries sorted by it |
| `scope` | string | — | always `"product"` | provider manifest ships product scope only |
| `sha256` | string | — | hex SHA256 of the canonical `SKILL.md` UTF-8 text | 64-char lowercase hex; digest-fresh |
| `resolvablePath` | string | — | `.agents/skills/<id>/SKILL.md` | provider-source-root skill path |
| `materializes-when` | string | **yes** | the boolean condition gating body emission | **verbatim** equal to the `template.json` `sources[].condition` that emits this body (FR-001, FR-006) |
| `supplied-by` | string | **yes** | provider source dir holding the canonical body | `dirname(catalog-source) + "/"` (FR-002) |

### Field grammar — `materializes-when`

Same expression grammar as `template.json` source conditions: `==` comparisons of `profile` /
`lifecycle` / `feedback` against quoted string / `true`, combined with `||` / `&&`, optionally
parenthesized. Stored **verbatim** so the drift test is a string compare and a downstream evaluator can
reuse the existing conditional engine. Empty/absent is not permitted for these 12 entries (every skill
in this manifest is conditionally emitted; none is unconditional).

## The 12 entries (id → new fields)

| id | `materializes-when` | `supplied-by` |
|---|---|---|
| `fs-gg-elmish` | `(profile == "app" \|\| profile == "sample-pack" \|\| profile == "game")` | `template/product-skills/fs-gg-elmish/` |
| `fs-gg-feedback-capture` | `(feedback == true) && lifecycle == "spec-kit"` | `template/feedback/skill/` |
| `fs-gg-keyboard-input` | `(profile == "app" \|\| profile == "game")` | `template/product-skills/fs-gg-keyboard-input/` |
| `fs-gg-layout` | `(profile == "app" \|\| profile == "game")` | `template/product-skills/fs-gg-layout/` |
| `fs-gg-project` | `(lifecycle == "spec-kit")` | `template/base/.agents/skills/fs-gg-project/` |
| `fs-gg-samples` | `(profile == "sample-pack") && lifecycle == "spec-kit"` | `template/fragments/samples/skill/` |
| `fs-gg-scene` | `(profile == "app" \|\| profile == "headless-scene" \|\| profile == "governed" \|\| profile == "sample-pack" \|\| profile == "game")` | `template/product-skills/fs-gg-scene/` |
| `fs-gg-skiaviewer` | `(profile == "app" \|\| profile == "sample-pack" \|\| profile == "game")` | `template/product-skills/fs-gg-skiaviewer/` |
| `fs-gg-styling` | `(profile == "app" \|\| profile == "game")` | `template/product-skills/fs-gg-styling/` |
| `fs-gg-symbology` | `(profile == "app" \|\| profile == "headless-scene" \|\| profile == "governed" \|\| profile == "sample-pack" \|\| profile == "game")` | `template/product-skills/fs-gg-symbology/` |
| `fs-gg-testing` | `(profile == "governed")` | `template/product-skills/fs-gg-testing/` |
| `fs-gg-ui-widgets` | `(profile == "app" \|\| profile == "game")` | `template/product-skills/fs-gg-ui-widgets/` |

## The `fs-gg-project` special case (the point of the feature)

- **Body source (`supplied-by`)**: `template/base/.agents/skills/fs-gg-project/` — a per-skill dir.
- **Gate (`materializes-when`)**: `(lifecycle == "spec-kit")` — but that condition is applied by the
  **whole-tree** `template/base/.agents/` source (target `.agents/`), not by a `.agents/skills/
  fs-gg-project/`-targeted row. `supplied-by` ≠ the gating source; that is expected and documented.
- **Honesty outcome**: under sdd-lane params (`lifecycle=sdd`) the condition is **false**, so the
  entry is `declared ∧ condition-false ∧ absent` (legitimately suppressed) instead of the untyped
  `declared ∧ absent` it is today. This is the single record that unblocks issue #71.

## Invariants (enforced by tests)

1. `materializes-when(id)` == the verbatim condition on that skill's `template.json` body source
   (Feature238 test; guards drift both ways).
2. `supplied-by(id)` == `dirname(catalog-source(id)) + "/"` (Feature238 test).
3. Every entry has both new fields, non-empty (Feature238 test).
4. `schemaVersion == 1`, entries sorted, `scope == "product"`, digests fresh, catalog ≡ emission rows
   (Feature231 tests — unchanged, still green).
5. `--check` mode of the generator reports up-to-date after regeneration (existing gate).
