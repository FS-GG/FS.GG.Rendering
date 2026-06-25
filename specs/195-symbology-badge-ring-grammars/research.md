# Phase 0 Research: Badge & Ring Alternative Symbology Grammars

All decisions are grounded against the repository tree on 2026-06-25 (specs 192–194 complete). The spec's
Assumptions block already fixes the high-level visual identity and explicitly declares the exact geometry a
**design-loop detail, not a contract** — so there are **no blocking NEEDS CLARIFICATION**. The questions below
are design choices the plan resolves to keep the implementation honest to the contract (FR-003 every channel
observable, FR-004 determinism, FR-010 Token zero-drift).

---

## D1 — How is "grammar selection" exposed without disturbing the Token grammar?

- **Decision**: Add a first-class value `[<RequireQualifiedAccess>] type Grammar = Token | Badge | Ring`. Add
  pure sibling render functions `badge : Token -> Scene` and `ring : Token -> Scene`, a dispatcher
  `render : Grammar -> Token -> Scene`, and grammar-parameterized boards `galleryIn : Grammar -> int -> float
  -> Token list -> Scene`, `filmstripIn : Grammar -> int -> (Motion * Token) list -> Scene`, and
  `animateIn : Grammar -> Motion -> Token -> float -> Scene`. The existing `token`/`animate`/`gallery`/
  `filmstrip` keep their **exact** signatures and become the `Grammar.Token` path.
- **Rationale**: The unit of A/B comparison is the form-factor choice (Key Entity "Grammar selection"), so it
  should be a value a board function takes — not a string or a flag. `[<RequireQualifiedAccess>]` makes
  `Grammar.Token` unambiguous against the existing `Token` *record* in the same namespace. Keeping the four
  existing functions untouched is the simplest way to guarantee **zero behavioural drift** (FR-010/SC-006):
  their bodies are not edited, and the Token golden test pins their output byte-for-byte.
- **DRY note (optional, not a contract)**: `render Grammar.Token` MAY simply call the existing `token`, and
  `galleryIn Grammar.Token` MAY share a body with `gallery`, **provided** the Token output stays byte-identical
  (the determinism/golden test is the gate). If sharing risks any drift, keep them separate — zero drift wins
  over DRY.
- **Alternatives considered**: (a) a `grammar` field on `Token` — rejected: pollutes the channel vocabulary
  (the spec forbids new `Token` fields, Assumptions "One channel vocabulary") and conflates *what to encode*
  with *how to draw it*. (b) Three separate modules `Badge`/`Ring`/`Token` — rejected: fractures the single
  shared vocabulary and complicates the board API; the spec frames them as **sibling elements** of one grammar
  family. (c) Changing `gallery`/`filmstrip` to *take* a `Grammar` parameter — rejected: a signature change to
  the existing functions is gratuitous surface churn and risks Token drift; additive `*In` siblings are safer.

## D2 — Where do the new functions live (project / module / compile order)?

- **Decision**: Same package `src/Symbology/`, same `module Symbology`, same files `Symbology.fsi`/
  `Symbology.fs`. No new file, no new project, no `.fsproj` change. Compile order unchanged.
- **Rationale**: FR-012 keeps the grammars in the **pure scene-only layer**; they reference only the channel/
  `Token` types and `FS.GG.UI.Scene` primitives already in scope here. Co-locating with `token` keeps the one
  shared vocabulary singular and adds nothing to the dependency closure. `src/Symbology.Render/` is reserved for
  the raster/host bridge and is **not** touched.
- **Alternatives considered**: a new `Grammars.fs` file — rejected as needless ceremony for two functions that
  share every helper (`clamp01`, `factionColor`, `lerpColor`, `placeholder`) with `token`.

## D3 — Badge channel siting (FR-003 — every channel observable, FR-006 screen-aligned)

- **Decision**: A compact, **screen-aligned** framed emblem. Per-channel siting (the testable default; geometry
  tunable in the design loop):

  | Channel (`Token` field) | Badge primitive | Kind |
  |---|---|---|
  | Faction (`Faction`) | frame stroke **hue** (faction palette; `Custom` honoured) | categorical |
  | Threat (`Threat`) | frame stroke **width** | ordered/continuous |
  | State (`State`) | frame **solid vs dashed** (`PathEffect.Dash`) | categorical |
  | Charge (`Charge`) | interior **radial gradient** alpha | continuous |
  | Health (`Health`) | bottom **health bar** length + green→red hue | continuous |
  | Speed (`Speed`) | row of **pips** (0..4) | ordered |
  | Shield (`Shield`) | corner **mount** dot | boolean |
  | Sigil (`Sigil`) | centre **vector sigil** (reuse the Token sigil shapes) | categorical |
  | Klass (`Klass`) | class-driven **frame outline / corner profile** | categorical |
  | Heading (`Heading`) | discrete **edge pip** at the heading direction (NOT whole-body rotation) | continuous |

- **Rationale**: Mirrors the Token grammar's primitive vocabulary (so the same `Scene` primitives and helpers
  apply) while presenting a stable, non-directional register some games prefer. Heading is preserved as a
  discrete pip so FR-003 holds without rotating the body (FR-006). Every channel maps to a primitive whose
  variation is observably distinct → satisfies SC-002 channel-by-channel.

## D4 — Ring channel siting (FR-003, FR-006, FR-007 health arc monotone)

- **Decision**: A centred **radial gauge**. Per-channel siting (testable default; geometry tunable):

  | Channel (`Token` field) | Ring primitive | Kind |
  |---|---|---|
  | Faction (`Faction`) | outer-ring **hue** | categorical |
  | Threat (`Threat`) | ring **thickness** | ordered/continuous |
  | State (`State`) | ring **solid vs dashed** | categorical |
  | Charge (`Charge`) | radial interior **gradient** alpha | continuous |
  | **Health (`Health`)** | **arc sweep** around the rim, sweep **monotone↑** in health, green→red hue | continuous |
  | Speed (`Speed`) | rim **beads** (0..4) | ordered |
  | Shield (`Shield`) | ring **mount** dot | boolean |
  | Sigil (`Sigil`) | centre **vector sigil** | categorical |
  | Klass (`Klass`) | class-driven **inner glyph** | categorical |
  | Heading (`Heading`) | heading **needle** from centre (indicator, not body rotation) | continuous |

- **Rationale**: The radial form makes continuous channels (health, charge) read as **arc/rim** quantities,
  which is the whole point of offering Ring as an alternative register. FR-007 is encoded explicitly:
  `healthSweep = maxSweep * clamp01 Health`, strictly monotone non-decreasing in `Health` — a dedicated test
  asserts monotonicity across the full `[0,1]` range (SC-002 for the health channel, FR-007).

## D5 — Which motions are grammar-agnostic (FR-014)?

- **Decision**: For `animateIn` on Badge/Ring, apply **only centre/radius** overlays — the `Pulse`, `Blink`,
  `Damage` family (a concentric ring / corner dot / red wash drawn around the symbol centre at radius `R`).
  Heading-/tail-bound motions (`Spin`'s rotating tick, `Moving`'s directional echo) are **not** expressed on the
  screen-aligned grammars this iteration; on Badge/Ring those cases render the **static base symbol** (the
  grammar-agnostic identity), keeping `animateIn` total and deterministic. The existing Token `animate` is
  **unchanged** and still renders all six motions.
- **Rationale**: The spec Assumptions scope motion to "overlays that are grammar-agnostic (centred pulse/blink/
  etc. around the symbol radius); bespoke per-grammar motion is deferred." A centred overlay draws identically
  regardless of the base symbol beneath it, so it stays deterministic and consistent across grammars. Falling
  back to the static symbol for directional motions (rather than inventing a Badge-specific spin) honours
  "Motion behaviour that cannot be expressed identically across grammars is out of scope" (FR-014) without
  dropping determinism. Static galleries remain the **primary** review surface for the new grammars.
- **Alternatives considered**: (a) inventing per-grammar Spin/Moving — rejected: out of scope (FR-015,
  "bespoke per-grammar motion deferred"). (b) throwing on a non-agnostic motion — rejected: violates safe-
  failure (VI) and FR-014's determinism requirement; a total function that degrades to the static symbol is
  safer and still observable.

## D6 — Degenerate input across grammars (FR-005/SC-004)

- **Decision**: Reuse the existing `placeholder` helper. `badge`/`ring`/`render`/`animateIn` all short-circuit
  to `placeholder t` when `R <= 0`, exactly as `token`/`drawSymbol` do today. No grammar raises on degenerate
  or otherwise valid input.
- **Rationale**: One placeholder rule for all three grammars keeps the contract uniform (Edge Cases:
  "mirroring the Token grammar's existing rule") and reuses tested code.

## D7 — Token zero-drift verification strategy (FR-010/SC-006)

- **Decision**: Do **not** edit the bodies of `token`/`animate`/`gallery`/`filmstrip`. Add a determinism/golden
  test that pins the **canonical bytes** of the existing Token output for a fixed `Token`/roster and asserts
  they are byte-unchanged after this feature. The full pre-existing `Symbology.Tests` battery must stay green
  with no assertion weakened.
- **Rationale**: The cheapest, most honest guarantee of "zero behavioural drift" is to not touch the code and
  to pin its bytes. Per the rendering-evidence memory, compare **canonical SceneCodec bytes**, not a coarse
  readback hash, for an exact determinism claim.

## D8 — Linter grammar-independence (FR-009/SC-005)

- **Decision**: No linter change. `Legibility.score`/`scoreAnimated` take `Token list` / `(Motion * Token)
  list` and never see a `Grammar`. The new test renders a fixed roster in all three grammars (asserting the
  *scenes differ*, so the test is meaningful) yet asserts `Legibility.score roster` returns the **identical**
  `Report` regardless of grammar — true by construction because the grammar never enters the linter's input.
- **Rationale**: Grammar-independence is structural, not behavioural: the governance gate scores the encoded
  channel *values*, which are the same `Token`s no matter which grammar draws them. The test makes the
  structural fact observable and guards against a future regression that might smuggle grammar into scoring.

## D9 — Test-battery shape

- **Decision**: Extend the existing batteries to iterate over a `grammars = [ Grammar.Token; Grammar.Badge;
  Grammar.Ring ]` list rather than duplicating files: ChannelPresence (vary one channel, assert canonical bytes
  change) for Badge & Ring; Determinism (same `Token` twice ⇒ identical bytes) for all three; Placeholder
  (`R <= 0` ⇒ visible non-empty placeholder, no throw) for all three; Gallery/Filmstrip reproducibility per
  grammar incl. empty/single roster. Add a focused Ring health-arc **monotonicity** test (FR-007) and a linter
  grammar-independence test. Pin the Token golden bytes (D7).
- **Rationale**: Parameterizing over `grammars` proves SC-001 (one mapping, three grammars) directly and keeps
  the new coverage close to the established battery style. New behaviour unique to a grammar (Ring health
  monotonicity) gets its own focused test.

## D10 — Skill grammar documentation (FR-013/SC-007)

- **Decision**: Update the canonical `src/Symbology/skill/SKILL.md` grammar section to present **Badge** and
  **Ring** as selectable grammars alongside the Directional Token — when to prefer each (directional vs stable
  emblem vs radial gauge), and the key invariant that the `ChannelMap` is **unchanged** across grammars (the
  unit of change stays the mapping). Mirror the edit to `template/product-skills/fs-gg-symbology/SKILL.md`; the
  `.claude/` and `.agents/` trees are pointer wrappers that inherit it. Run `scripts/check-agent-skill-parity.fsx`
  and require **zero** critical/high findings (SC-007).
- **Rationale**: FR-013 requires the loop docs to describe the new grammars as selectable behind one vocabulary;
  the existing CRITIQUE/INTAKE steps already reference "pick grammar (default + only v1: Directional Token)" —
  that line is updated to name all three. Authoring canonically + mirroring is the established parity discipline
  (specs 192–194).

---

### Summary of decisions

| # | Decision |
|---|---|
| D1 | `type Grammar = Token \| Badge \| Ring` + `badge`/`ring`/`render`/`galleryIn`/`filmstripIn`/`animateIn`; existing four functions untouched |
| D2 | Same package/module/files; no new project; compile order unchanged |
| D3 | Badge = screen-aligned framed emblem; full per-channel siting; heading = edge pip |
| D4 | Ring = centred radial gauge; full per-channel siting; health = monotone arc sweep |
| D5 | Grammar-agnostic motion = Pulse/Blink/Damage (centre/radius); directional motions fall back to static on Badge/Ring; Token `animate` unchanged |
| D6 | Reuse `placeholder` for `R <= 0` in all grammars; never throw |
| D7 | Don't edit existing bodies; pin Token canonical bytes for zero-drift |
| D8 | No linter change; grammar-independence is structural; assert identical report across grammars |
| D9 | Parameterize existing batteries over a `grammars` list; add Ring health-monotonicity + linter-independence tests |
| D10 | Document Badge/Ring as selectable grammars canonically + mirror; pass skill-parity |

All NEEDS CLARIFICATION resolved (none were blocking; geometry is design-loop, not contract).
