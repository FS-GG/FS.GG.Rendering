# Contract: Documentation reference grammar

The structural contract every F6 doc and the skill MUST follow so the coverage check (FR-008) can verify it. Line-parsed — no Markdown/YAML parser dependency (research R2).

## Front-matter

Each doc begins with a minimal front-matter block delimited by `---` lines. The check reads only these keys (other keys are ignored):

**Pattern doc** (`docs/product/ant-design/patterns/<family>.md`):
```
---
family: <one Catalog.categories value, lowercase, e.g. display>
---
```

**Recipe doc** (`docs/product/ant-design/templates/<template>.md`):
```
---
template: <one of: workbench | list | detail | form | result | exception>
status: groundwork
---
```

Rules:
- `family` MUST be a single token equal (case-sensitive) to a value returned by `Catalog.categories`.
- `template` MUST be exactly one of the six fixed names.
- `status` on a recipe MUST be the literal `groundwork`.

## Machine-checked references block

Every pattern doc, every recipe, and the skill MUST contain a section:

```
## Machine-checked references

` ``refs
control:<catalog-id>
token:<Module>.<member>
resolver:<member>
policy:<name>
doc:<relative-path>
part:<AntComponent>/<partName>
` ``
```
*(fence shown spaced to avoid nesting; in the real doc it is a normal triple-backtick block with info-string `refs`.)*

Rules:
- One reference per line, format `prefix:value`, no spaces around `:`.
- Blank lines inside the block are ignored.
- Allowed prefixes: `control`, `token`, `resolver`, `policy`, `doc`, `part`. Any other prefix fails the check.
- **Pattern docs** MUST include ≥1 `control:`, ≥1 `token:`, ≥1 `resolver:`, the applicable `policy:` line, and ≥1 `part:` (the Ant semantic-part mapping, FR-011).
- **Recipe docs** MUST include ≥1 `control:` and ≥1 `token:`.
- **The skill** MUST include ≥1 `doc:` (linking a pattern doc) and ≥1 of `token:`/`resolver:`/`policy:`.

## Reference resolution (source of truth)

| Prefix | Value form | Resolves when |
|---|---|---|
| `control` | catalog id, e.g. `button-primary` | value ∈ `Catalog.supportedControls |> List.map (fun c -> c.Id)` |
| `resolver` | member name, e.g. `resolveDefault` | a public member of `FS.GG.UI.DesignSystem.StyleResolver` |
| `token` | `Module.member`, e.g. `Seed.colorPrimary` | the member exists on the named public token type in `FS.GG.UI.DesignSystem` (`DesignTokensExt.*` or flat `DesignTokens`) |
| `policy` | name, e.g. `ant` | `ColorPolicy.byName value` returns a policy (via existing `Color` IVT) — `wcag`/`ant` |
| `doc` | repo-relative path | `File.Exists` relative to the citing file's directory |
| `part` | `<AntComponent>/<partName>`, e.g. `Button/icon` | **shape only — does not resolve against code.** Valid when the value contains exactly one `/`, with a non-empty `<AntComponent>` before it and a non-empty `<partName>` after it. It is a declared reference to Ant's upstream semantic-part vocabulary (snapshot at `docs/product/ant-design/reference/ant-llms-sources.md`), not a repo symbol — the check verifies presence and shape, never semantic equivalence to a repo region (that is a review concern, FR-011). |

## Required-content markers

- Every **pattern doc** MUST contain the fixed line (verbatim substring): `Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.`
- The **skill** MUST contain that no-React/DOM statement AND a one-semantic-control-set / no-per-theme-fork statement.

## Semantic-part mapping (FR-011)

- Each **pattern doc** MUST declare, via `part:` refs, ≥1 Ant component and its named semantic parts (e.g. `part:Button/root`, `part:Button/icon`).
- A pattern doc that carries any `part:` ref MUST also carry the resolving `control:`, `token:`, and `resolver:` refs that anchor those parts to repo machinery (already required for pattern docs).
- The doc prose SHOULD map each declared part to its repo control region + token + resolver state and cite the snapshot; this prose mapping is a **review** concern. The check enforces only that the `part:` refs are present and well-shaped and that the companion code refs resolve.
- The `<AntComponent>` names come from the curated snapshot (`docs/product/ant-design/reference/ant-llms-sources.md`), which records the upstream source URL and retrieval date.

## Stability

This grammar is internal to F6's docs + test. It is not a public API; changing it only affects this feature's docs and `Feature131AntPatternDocsTests`. No `.fsi` or surface baseline is involved.
