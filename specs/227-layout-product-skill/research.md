# Phase 0 Research: fs-gg-layout consumer product-skill

All unknowns are resolved from the shipped `fs-gg-styling` (Feature 226) precedent and the live repo state; no external research needed (offline-safe, content-only feature).

## R1 — Profile gating for the new skill

**Decision**: Wire `fs-gg-layout` on `(profile == "app" || profile == "game")`, emitting to both `.agents/skills/fs-gg-layout/` and `.claude/skills/fs-gg-layout/`.

**Rationale**: These are the two profiles that reference Layout as a *used* capability (the game choice text names Layout; `template/base/src/Product/Program.fs` re-exports the `LayoutEvidence` region API). The verified `.template.config/template.json` wiring for the sibling `fs-gg-styling` and `fs-gg-ui-widgets` is exactly this predicate — gated on product surface, never on lifecycle (no `lifecycle == "spec-kit"` clause), so the skill follows the product under `spec-kit`/`sdd`/`none` alike.

**Alternatives considered**: (a) also `sample-pack`/`headless-scene`/`governed` — rejected: those profiles do not vendor the controls/interaction skills and the spec (SC-002) requires layout stay scoped to app+game; (b) a `lifecycle`-gated source — rejected: would violate FR-003 and red the Feature 219 "lifecycle-independent" assertion.

## R2 — Is the `fs-gg-product-layout` wrapper pair in scope?

**Decision**: Yes. Add `.agents/skills/fs-gg-product-layout/SKILL.md` and `.claude/skills/fs-gg-product-layout/SKILL.md`, each a thin alias routing to `../../../template/product-skills/fs-gg-layout/SKILL.md`.

**Rationale**: All 8 currently-shipped product-skills carry a matching `fs-gg-product-*` wrapper; `skillist-reference.md` states "each also ships a `fs-gg-product-<name>` wrapper alias alongside it"; Feature 226 added `fs-gg-product-styling` as part of the same pattern. The skill-parity harness (`tools/Rendering.Harness/SkillParity.fs`) discovers canonical↔wrapper pairs dynamically and emits a `MissingWrapper` finding for a canonical skill with no wrapper — so shipping the body without the wrapper would fail parity (Feature 226 reported canonical 21→22, wrappers 43→45 "inventoried with its wrapper").

**Alternatives considered**: body-only (no wrapper) — rejected: breaks the documented parity invariant and the "each also ships a wrapper" catalog claim.

## R3 — Which repo-owned enumerations/gates must the skill join?

**Decision**: Join/adjust exactly these (each verified against the Feature 226 diff):

| Surface | Kind | Change |
|---|---|---|
| `.template.config/template.json` | wiring | +2 gated `sources` (app\|game → .agents + .claude) |
| `template/base/docs/skillist-reference.md` | catalog (Feature 224) | +1 row: `fs-gg-layout` \| path \| `app, game` |
| `Feature225ProductSkillVocabularyTests.fs` `expectedProductSkillIds` | leak-guard backstop | +`fs-gg-layout` (8→9) |
| `Feature219EmitFrameworkSkillsTests.fs` `expectedFrameworkSkills` | emission matrix | +`fs-gg-layout` on app+game rows; source floor `16→18` |
| `Feature204LifecycleTemplateTests.fs` | framework-source floor | `>=16` → `>=18` |
| `docs/reports/skills-parity.md` | generated report | regenerate (canonical +1, wrapper +2) |

**Rationale**: These are the surfaces the Feature 226 ship touched. Discovery in the parity/leak harness is dynamic, so no canonical *list* is hand-edited; the backstop set, catalog, and matrix floors are the only human-maintained enumerations and must move in lockstep or the release-only Package.Tests gate fails (as it did on the 226 first-pass, run 28480885576, when the matrix was not updated).

**Alternatives considered**: relying on dynamic discovery alone — rejected: the Feature 219/204 exact-set and source-count assertions and the Feature 225 backstop are fixed and would red without the edits.

## R4 — Content boundary (consumer slice vs framework engine)

**Decision**: The skill documents only the consumer layout surface an app/game author uses — compute HUD + gameplay/content regions from output size, keep an active item inside the gameplay region, and the `LayoutEvidence` region/bounds shape the starter ships (`hudRegionForSize`, `gameplayRegionForSize`, `activeGameplayBoundsForSize`, `movement/spawnUsesGameplayRegion`, `layoutEvidenceForSize`). It explicitly bounds out the Yoga-backed layout **engine** internals (`Layout.evaluate`, `Defaults.layoutNode`, `.fsi`/surface-baselines) which remain owned by the framework `src/Layout/skill/SKILL.md`.

**Rationale**: Mirrors the `fs-gg-styling` "consume-a-style, not the resolver" boundary (FR-002). Keeps the two `fs-gg-layout` ids (framework vs consumer) non-overlapping in purpose even though they share a `name:`.

**Alternatives considered**: documenting `Layout.evaluate`/`layoutNode` in the consumer skill — rejected: that is the framework engine surface, out of the consumer slice, and would duplicate/contradict the upstream skill.

## R5 — Leak-guard compliance (Feature 225)

**Decision**: Author the body to pass the leak guard from the first draft: no `Feature \d+` / `spec-\d+` stamps, no `readiness`/`package-feed`/`.gitignore`/`BaseOutputPath` framework-evidence tokens, and any `specs/.../feedback` mention (there should be none) only inside a spec-kit-gated paragraph.

**Rationale**: The Feature 225 guard scans every discovered product-skill; a fresh `fs-gg-layout` is auto-included by dynamic discovery, so it is scanned the moment it exists and must be clean.

**Alternatives considered**: n/a — this is a hard gate.

## R6 — Version/delivery posture

**Decision**: Content-only, **no** `fs-gg-ui-template` version bump; no tag triple, no registry flip. Delivery to consumers rides the next coherent-set republish sequenced by epic #34 (as with siblings #35–#38), out of scope here.

**Rationale**: Additive consumer content under the existing contract, identical to Feature 226 (`bc4fa32` "content-only, no bump"). The `fs-gg-ui` republish runbook (memory) is the separate delivery step.
