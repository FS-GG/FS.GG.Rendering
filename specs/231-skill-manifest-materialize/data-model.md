# Data Model — Feature 231

## Skill manifest (`template/skill-manifest/skill-manifest.json` → `.agents/skills/skill-manifest.json`)

Conforms to `skill-manifest` schema v1 (`Fsgg.Schemas.SkillManifest`, `skillManifestVersion = 1`).

| Field | Type | Rule |
|---|---|---|
| `schemaVersion` | int | `1` |
| `skills` | entry list | full product-scope catalog, sorted by `id` |
| `skills[].id` | string | skill directory name (`fs-gg-*`) |
| `skills[].scope` | string | `"product"` for every entry (process skills are not catalogued here) |
| `skills[].sha256` | string | lowercase-hex SHA256 of the canonical `SKILL.md` UTF-8 bytes |
| `skills[].resolvablePath` | string | `.agents/skills/<id>/SKILL.md` (no inline `body`) |

### Catalog (12 entries) and canonical sources

| id | canonical source | ships when |
|---|---|---|
| `fs-gg-scene` | `template/product-skills/fs-gg-scene/` | all profiles |
| `fs-gg-symbology` | `template/product-skills/fs-gg-symbology/` (+ `reference.fsx`) | all profiles |
| `fs-gg-skiaviewer` | `template/product-skills/fs-gg-skiaviewer/` | app, sample-pack, game |
| `fs-gg-elmish` | `template/product-skills/fs-gg-elmish/` | app, sample-pack, game |
| `fs-gg-keyboard-input` | `template/product-skills/fs-gg-keyboard-input/` | app, game |
| `fs-gg-ui-widgets` | `template/product-skills/fs-gg-ui-widgets/` | app, game |
| `fs-gg-styling` | `template/product-skills/fs-gg-styling/` | app, game |
| `fs-gg-layout` | `template/product-skills/fs-gg-layout/` | app, game |
| `fs-gg-testing` | `template/product-skills/fs-gg-testing/` | governed |
| `fs-gg-samples` | `template/fragments/samples/skill/` | sample-pack ∧ spec-kit |
| `fs-gg-feedback-capture` | `template/feedback/skill/` | feedback ∧ spec-kit |
| `fs-gg-project` | `template/base/.agents/skills/fs-gg-project/` | spec-kit |

Invariants: canonical bodies are **name-neutral** (no `Product`/`product` substitution
tokens); emission is `copyOnly` so materialized bytes ≡ canonical bytes ≡ digest.

## Skill union (per concrete scaffold)

```
union(scaffold) = { manifest entry | .agents/skills/<id>/SKILL.md emitted }      // digest-checked
               ∪ { present skill ∉ manifest }                                    // speckit-* etc.; Sha256 = "" (presence + identity only)
```

## Agent-skill roots

`[".claude"; ".codex"; ".agents"]` — vendored constant, parity-asserted against
`Fsgg.Schemas.agentSkillRoots`. Provider source root: `.agents`. Mirror targets:
`.claude`, `.codex`. Skill file path shape: `<root>/skills/<id>/SKILL.md`.

## Materialize step artifacts (spec-kit lane only)

| Artifact | Emitted to | Notes |
|---|---|---|
| `template/lifecycle/skill-mirror-vendored.fs` | `.specify/scripts/fs-gg/skill-mirror-vendored.fs` | pure module `FsGg.Vendored.SkillMirror`; body transliterates `Fsgg.SkillMirror` |
| `template/lifecycle/materialize-skill-roots.fsx` | `.specify/scripts/fs-gg/materialize-skill-roots.fsx` | `#load`s the module; IO driver; `--enforce` flag |
| stamp | `.specify/.fs-gg/skill-roots.stamp` | MSBuild incrementality output (gitignored in product) |

Driver behavior: enumerate all files under `.agents/skills/**` → `retargetSkillPath` into
`.claude`/`.codex` (skip-if-byte-identical writes) → load manifest → build expected set →
`verify` over the three roots → print per-skill drift; exit non-zero only with `--enforce`.

## MSBuild target (in `template/base/Directory.Build.props`)

`FsGgMaterializeSkillRoots`: `BeforeTargets="Build"`;
`Condition="Exists('$(MSBuildThisFileDirectory).specify/scripts/fs-gg/materialize-skill-roots.fsx')"`;
`Inputs` = manifest + `.agents/skills/**`; `Outputs` = stamp; body = `Exec dotnet fsi <script>`
(+ `Touch` stamp). Fires in no lifecycle but spec-kit (script absent elsewhere).

## Drift report (from vendored/library `verify`)

Per skill: `MissingRoots: root list`, `Divergent: bool`, `HashMismatchRoots: root list`;
all-clean skills are not reported.

## template.json source-row delta

| Row group | Before | After |
|---|---|---|
| blanket repo `.agents/skills/` → `.agents/` | 1 (whole dev surface) | 1 (`include: speckit-*/**`) |
| blanket → `.claude/`, `.codex/` | 2 | 0 |
| product-skill `.agents` rows | 9 | 9 (`copyOnly` added) |
| product-skill `.claude`/`.codex` twins | 18 | 0 |
| samples/feedback skill rows | 6 (3 roots × 2) | 2 (`.agents` only) |
| manifest row | 0 | 1 (ungated, `copyOnly`) |
| materialize-script row | 0 | 1 (spec-kit-gated, `copyOnly`) |
