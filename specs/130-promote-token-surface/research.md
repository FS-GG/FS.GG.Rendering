# Phase 0 Research — F5 Public-Surface Promotion

Decisions resolving the Technical Context. No `NEEDS CLARIFICATION` remained in the spec; the open
items here are scope/mechanism choices a Tier-1 promotion must pin down before design.

## R1 — Promotion mechanism: remove `internal`, add `.fsi` (not modifier-flip)

**Decision**: For each promoted module, **remove the `internal` access modifier from the `.fs`** and **add a
curated `.fsi`** declaring the public surface. Visibility is governed solely by the `.fsi`.

**Rationale**: Constitution Principle II forbids `public`/`internal`/`private` on `.fs` top-level bindings —
"visibility lives in the `.fsi`". A module with a paired `.fsi` is exactly as public as its `.fsi` says. This is
also how the *existing* public `DesignTokens` already works (generated `.fs` with no modifier + curated `.fsi`).
Simply deleting `internal` without an `.fsi` would make the whole module public *without* a curated contract and
would violate "every public module MUST have a corresponding `.fsi`".

**Alternatives considered**: (a) Leave `internal` and widen `InternalsVisibleTo` to external consumers — rejected:
IVT is not a public contract, is invisible to the surface gate, and does not work for out-of-repo packages.
(b) Add a thin public wrapper module re-exporting internal bindings — rejected: two surfaces for one thing, extra
indirection, and the internal originals would still need a story.

## R2 — Which symbols are promoted (the "chosen subset")

**Decision**: Promote **`StyleResolver`** in full (`IntentPolicy`, `neutralPolicy`, `baseStyleFor`, `resolve`,
`resolveDefault`) and the **entire generated `DesignTokensExt` taxonomy** (Seed; Map.Light/Dark; Alias.Light/Dark;
Component.*; Space; Density; Type.*; Elevation). **Defer** the F2/127 `ColorPolicy` engine (see R5).

**Rationale**: The roadmap names the consumers (spec Assumptions): D2 concrete themes and G3 Ant showcase consume
the taxonomy as their design vocabulary, and themes/apps supply `IntentPolicy` values through the resolver seam
(US1/US3). The taxonomy is *all token values* — there are no internal helper bindings inside it to keep private —
so "the chosen subset" is the whole generated vocabulary; promoting it as a unit is the coherent, reviewable
contract. `StyleResolver` is small and semantic; its seam is the entire point of F4 and is useless to external
consumers while internal.

**Alternatives considered**: (a) Promote only the resolver, keep tokens internal — rejected: the feature is the
token-surface promotion the prior phases deferred; deferring it again just moves the problem. (b) Promote a hand-
picked token slice and keep the rest internal — rejected: it would split one generated module into a public `.fsi`
part and a private remainder, but anything omitted from a paired `.fsi` becomes **private** (not IVT-visible), which
would *break* in-repo consumers/tests (conflicts with FR-009's "still reachable in-repo"). A clean split would mean
two physical modules — unjustified churn for a vocabulary that is consumed as a whole.

## R3 — The taxonomy `.fsi` is GENERATED (not hand-curated)

**Decision**: Extend `scripts/generate-design-tokens.fsx` to emit **`DesignTokensExt.fsi`** alongside the `.fs`, by
a second recursive walk of the DTCG tree that emits `val name : type` instead of `let name : type = literal`.
Extend `--check` (drift mode) to verify **both** files against committed.

**Rationale**: The taxonomy has ~130 leaf values across nested modules. Hand-curating that `.fsi` (as
`DesignTokens.fsi` is) would inevitably drift from the generator on every token edit. Generating both keeps them
byte-locked and makes the drift gate authoritative for the public surface too. The generator already walks the tree
to emit `let`s; the `val` walk reuses the same key→name and leaf→type mapping, so it is a small, low-risk addition.

**Alternatives considered**: (a) Hand-curate the `.fsi` once — rejected: guaranteed drift, defeats the generator's
purpose, and the surface gate would catch the drift only *after* a mismatched build. (b) Auto-derive the surface
without an `.fsi` (rely on no-modifier default) — rejected: violates Principle II's `.fsi`-required rule.

**Note**: This is a deliberate, documented divergence from `DesignTokens.fsi`'s hand-curation (recorded in the
decision record). The `.fsi`'s "no JSON dep in product assemblies" invariant is preserved — `System.Text.Json` is
used at *script* time only, never compiled into `DesignSystem`.

## R4 — Do NOT unify `DesignTokens` (flat) with `DesignTokensExt` (taxonomy)

**Decision**: Leave the existing public flat `DesignTokens` module untouched; promote `DesignTokensExt` as a
*separate* public module alongside it.

**Rationale**: Unifying would *change* an existing public contract (rename/move the flat primitives that
`Theme.light`/`Theme.dark` and external consumers already bind), turning a clean additive promotion into a
breaking change with migration cost — out of proportion to F5's goal. The two coexist cleanly: `DesignTokens` =
the curated primitives that feed today's `Theme`; `DesignTokensExt` = the richer Ant taxonomy for future themes.

**Alternatives considered**: Unify now — rejected as scope creep and a breaking change; recorded in the decision
record as explicitly deferred (a candidate for a later, dedicated phase).

## R5 — Defer the F2/127 ColorPolicy promotion (record the decision)

**Decision**: Do **not** promote `ColorPolicy`. Record in the decision record that it stays internal for this phase.

**Rationale**: (1) `FS.GG.UI.Color` is **absent from the surface-baseline refresh table** (`scripts/refresh-
surface-baselines.fsx`) — it has no committed public-surface baseline, so promoting its internals would either be
*unguarded* by the drift gate (contradicting F5's deliberate-gating purpose) or force adding a whole new package
baseline (scope creep). (2) F3/128's `--design-system wcag|ant` template parameter already exposes the policy
*choice* to consumers at the level they actually use it. (3) No current or near-term consumer needs the policy
*engine* itself public. FR-003 is satisfied by the recorded deferral.

**Alternatives considered**: Promote `ColorPolicy` + add a `FS.GG.UI.Color` baseline row to the refresh table —
rejected for this phase; noted as a clean follow-up if a consumer ever needs the engine directly.

## R6 — Surface-baseline blast radius is exactly one package

**Decision**: Regenerate only `tests/surface-baselines/FS.GG.UI.DesignSystem.txt`; expect new rows for
`...StyleResolver`, `...StyleResolver+IntentPolicy`, and the `...DesignTokensExt[+Sub...]` nested-module types.

**Rationale**: The baseline is **type-granular** (modules-as-types, not per-`val`) and excludes compiler-generated
types. Promotion adds public *types* only in `DesignSystem`; no other package re-exports them, so no other baseline
moves. Removing the `InternalsVisibleTo` grants is invisible to the gate (IVT ≠ public surface), so it cannot add
or remove baseline rows. This makes SC-002/SC-007 (every added row ⇔ a decision-record symbol) directly checkable
by `git diff` on the one file.

**Verification caveat (carried from prior features)**: `refresh-surface-baselines.fsx` has **no real `--check`**
— it always rewrites. Confirm "only-intended-rows-changed" by `git diff tests/surface-baselines/`, not by an exit
code. Likewise the token generator's `--check` is the authoritative drift check for `DesignTokensExt.{fs,fsi}`.

## R7 — Public-path test evidence (no IVT)

**Decision**: Add `tests/Controls.Tests/Feature130PublicSurfaceTests.fs` exercising the promoted symbols through
the **public API only** — no reliance on any `InternalsVisibleTo` grant. After the grants are removed (R6), the
existing Feature126/Feature129 tests *also* run against the public surface, which is itself proof; the new file
adds explicit value-parity, neutral-render-parity, and divergent-`IntentPolicy` reachability assertions.

**Rationale**: FR-008/SC-001 require proof the surface is genuinely consumable externally. The strongest cheap
proof is: (a) the symbols appear in the regenerated *public* baseline, and (b) a test compiles and calls them with
the IVT grants gone. Controls.Tests already references `DesignSystem`; with the grants removed, any access it makes
is necessarily public.

**Alternatives considered**: Spin up a brand-new test project with no DesignSystem reference history — rejected as
unnecessary; removing the IVT grants already converts every existing access into a public-only access, which is the
same proof at lower cost.
