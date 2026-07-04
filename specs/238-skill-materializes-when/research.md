# Phase 0 Research — record per-skill materialization conditions

All five open questions from the spec's Assumptions are resolved below. None required an external
spike; each is a design choice grounded in the existing generator/test and ADR-0017.

## R1 — `schemaVersion`: keep `1` (additive) vs bump to `2`

**Decision:** keep `schemaVersion: 1`. `materializes-when` and `supplied-by` are **optional additive**
keys on the existing `{id, scope, sha256, resolvablePath}` object.

**Rationale:** ADR-0017 frames the fields as "additive to ADR-0014's `{id, scope, sha256}`". Additive
keys are backward-compatible for any unknown-key-tolerant reader (`System.Text.Json` `GetProperty`
ignores extra keys — proven by Feature231's `readManifest`, which stays green). A version bump signals
a breaking change and would force the `.github#164` validator to negotiate versions for what is a pure
addition. Feature231's test also asserts `schemaVersion == 1`; keeping `1` leaves it untouched.

**Alternatives considered:** bump to `2` — rejected: implies a breaking change, churns the existing
test, and buys nothing since old readers already tolerate the new keys.

## R2 — condition source of truth: derive-in-generator vs hardcode + equivalence test

**Decision:** the generator carries the `materializes-when` string **alongside** each catalog entry
(one readable place, mirroring how it already hardcodes each skill's source path); a **new test parses
`.template.config/template.json` and asserts equivalence** per entry (FR-006). The stored value is the
**verbatim** `sources[].condition` string.

**Rationale:** the generator already hardcodes `id → source`; adding `id → condition` beside it keeps
it a flat, greppable table with no embedded conditional-parser. Full derivation would force the
generator to (a) parse template.json and (b) special-case `fs-gg-project`, whose body ships via the
whole-tree `template/base/.agents/` source (target `.agents/`, condition `(lifecycle == "spec-kit")`)
rather than a per-skill `.agents/skills/fs-gg-project/` row. Pushing that special case into the
producer is uglier than asserting equivalence in a test. Storing the condition **verbatim** (not a
normalized/re-parenthesized form) makes the equivalence test a trivial string compare — no expression
normalizer needed — and honors ADR-0017's "literally the `template.json` `sources[].condition`". This
satisfies FR-001 ("the exact condition the template applies … in template.json"); the spec's FR-001
table showed the logically-equivalent de-parenthesized form for readability only.

**Verbatim values (from template.json today):**

| id | `materializes-when` (verbatim) |
|---|---|
| `fs-gg-project` | `(lifecycle == "spec-kit")` |
| `fs-gg-scene` | `(profile == "app" \|\| profile == "headless-scene" \|\| profile == "governed" \|\| profile == "sample-pack" \|\| profile == "game")` |
| `fs-gg-symbology` | `(profile == "app" \|\| profile == "headless-scene" \|\| profile == "governed" \|\| profile == "sample-pack" \|\| profile == "game")` |
| `fs-gg-skiaviewer` | `(profile == "app" \|\| profile == "sample-pack" \|\| profile == "game")` |
| `fs-gg-elmish` | `(profile == "app" \|\| profile == "sample-pack" \|\| profile == "game")` |
| `fs-gg-keyboard-input` | `(profile == "app" \|\| profile == "game")` |
| `fs-gg-ui-widgets` | `(profile == "app" \|\| profile == "game")` |
| `fs-gg-styling` | `(profile == "app" \|\| profile == "game")` |
| `fs-gg-layout` | `(profile == "app" \|\| profile == "game")` |
| `fs-gg-testing` | `(profile == "governed")` |
| `fs-gg-samples` | `(profile == "sample-pack") && lifecycle == "spec-kit"` |
| `fs-gg-feedback-capture` | `(feedback == true) && lifecycle == "spec-kit"` |

**Test mapping (id → template.json source that emits the body):** reuse Feature231's proven pattern —
product / samples / feedback skills map by the source whose `target` is `.agents/skills/<id>/`;
`fs-gg-project` is the documented special case → the `source: "template/base/.agents/"` row's
condition. The test reads each matched source's `condition` and asserts string equality with the
manifest's `materializes-when`.

**Alternatives considered:** derive fully in the generator (rejected — leaks the fs-gg-project
special case into the producer and embeds a template.json parser there); store a normalized form
(rejected — needs an expression normalizer to compare, and diverges from "verbatim").

## R3 — `supplied-by` form

**Decision:** the repo-relative **provider source directory** holding the canonical `SKILL.md`, with a
trailing slash — i.e. `Path.GetDirectoryName(catalog-source)` + `/`. The generator already has the
source path per entry, so this is a one-line derivation.

**Values:** `fs-gg-project` → `template/base/.agents/skills/fs-gg-project/`; each product skill →
`template/product-skills/<id>/`; `fs-gg-samples` → `template/fragments/samples/skill/`;
`fs-gg-feedback-capture` → `template/feedback/skill/`.

**Note (fs-gg-project nuance):** `supplied-by` names where the **body** lives
(`…/skills/fs-gg-project/`); `materializes-when` names the **gate** applied to it (the parent
`template/base/.agents/` tree source). They intentionally differ for this one skill; documented in
data-model.md.

## R4 — JSON key naming: kebab-case vs camelCase

**Decision:** `materializes-when` and `supplied-by` (kebab-case), verbatim from ADR-0017 / issue #71,
even though the existing manifest uses camelCase (`schemaVersion`, `resolvablePath`).

**Rationale:** these two keys are the **cross-repo contract** the `.github#164` registry generator and
validator will read by name; matching the contract's field names is worth more than local camelCase
consistency. ADR-0017 and the epic both spell them kebab.

**Alternatives considered:** `materializesWhen` / `suppliedBy` — rejected: would force the `.github`
consumer to special-case Rendering's key spelling.

## R5 — does any in-repo consumer need to filter by `materializes-when`?

**Decision:** no in-repo behavioral change. Rendering's job is to **record** the condition; the
effective-set computation (and the `[missing]`/`[unexpected]` enforcement) lives in `.github#164`'s
`skill-union-assert.sh` with `--params scaffold-provenance.json`.

**Evidence:** the in-repo manifest consumers are the deterministic tests (Feature231 recomputes
digests; Feature219/204 assert emitted skill sets) and the materialize step — none computes an
"effective set" from conditions. `SkillRegistry` / `Audit.ownsVocabulary` concern the framework
**dev-surface skillist**, not the product manifest union. So "the sdd lane stops implying
fs-gg-project" is realized purely by recording the truth here; SDD (#53) and the gate (#164) consume
it.

## Cross-repo status (non-blocking)

ADR-0017 (`docs/adr/0017-…`), `docs/coordination/skill-registry.md`, and `registry/skills.yml` are
**not yet committed** in `FS-GG/.github` (still "Proposed" per epic #163). This feature does not depend
on them existing — it makes Rendering's producer manifest the honest input those artifacts will later
read. A local decision-record mirror (`docs/product/decisions/0017-*`, matching the existing
`0014-skill-vendoring…` mirror) is deferred until the org ADR lands, to avoid mirroring an unwritten
decision.
