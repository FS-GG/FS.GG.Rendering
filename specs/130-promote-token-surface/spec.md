# Feature Specification: Promote Token, Policy & Resolver Surface to Public — Workstream F, Phase F5

**Feature Branch**: `130-promote-token-surface`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "next item in fs.gg" → resolved to Workstream **F5**: deliberately promote the chosen public token / policy / resolver surface (the Ant-derived token taxonomy from F1/126, the central visual-state style resolver and its intent-policy seam from F4/129, and the color-validation policy from F2/127), regenerate the per-package public-surface baselines and the design-token-drift baseline in the same change, and author a decision record.

## Overview

Across F1–F4 the framework grew three substantial capabilities that all landed **internal/additive** on purpose, with **zero public-surface delta**:

- **F1 (126)** — the Ant-derived token taxonomy (seed → map → alias → component, plus spacing, named density, a type scale, and elevation), generated from the DTCG source. Reachable only in-repo via `InternalsVisibleTo`.
- **F2 (127)** — the `wcag`/`ant` color-validation policy engine. Internal to the Color layer.
- **F4 (129)** — the central, total, deterministic visual-state style resolver (`theme + kind + intent + visual-state(s) → style`) and its overridable **intent-policy seam**, which proves that intent divergence (e.g. a real `Danger` red) is reachable **without forking any control** — but only from a test, because the seam is internal.

Each of those features explicitly **deferred public promotion to F5** (see `specs/129-central-style-resolver/spec.md`: *"public token/policy/resolver promotion is deferred to F5"*). F5 is that deliberate promotion: it turns the **chosen subset** of this proven-internal surface into a **public, documented, stable contract**, regenerates the two CI drift gates in lock-step, and records the decision.

This phase is the gate that **unblocks the rest of the design-language work**: D2 (concrete Ant/Fluent/Material themes) and G3 (the Ant-styled showcase) need to build on these tokens and resolver from **outside** the framework's internals, and third-party theme/app authors need a public API to supply their own intent policies. F5 is **surface-only**: it changes *what is callable*, not *what is rendered* — default behaviour stays byte-identical.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Theme & component authors build on a stable public design surface (Priority: P1)

A theme author (the framework's own D2 work first, and eventually third parties) wants to assemble a concrete theme and style controls using the Ant-derived token taxonomy and the central style resolver. Today they cannot: the tokens, the resolver, and the intent-policy seam are all `internal`, reachable only by in-repo code holding an `InternalsVisibleTo` grant. F5 promotes the chosen subset to a public, documented contract so this work can happen in a separate package/assembly (or an external consumer) without special access.

**Why this priority**: This is the whole point of F5 and the blocking dependency for the remaining design-language roadmap (D2 themes, G3 showcase). Without it, every downstream theme must live inside the framework's internals — defeating the layered-package design that D1 established. Delivered alone, it is already a viable, valuable contract.

**Independent Test**: From an assembly that has **no** `InternalsVisibleTo` grant (e.g. a fresh test project or sample referencing only the published packages), reference the promoted token taxonomy and the resolver, assemble a style, and confirm it compiles and resolves — proving the surface is genuinely consumable from outside the framework's internals.

**Acceptance Scenarios**:

1. **Given** a consumer that references only the published packages (no internals grant), **When** it names the promoted token-taxonomy symbols and the public resolver path, **Then** the code compiles and resolves a style without needing internal access.
2. **Given** the promoted resolver path is invoked with the default policy, **When** a button is styled across every intent and visual state, **Then** the resolved output is byte-identical to today's rendered output (promotion is surface-only, not behavioural).
3. **Given** the promoted public surface, **When** the per-package surface baselines are regenerated, **Then** the only new rows are the deliberately-promoted symbols — no incidental surface leaks in.

---

### User Story 2 - The promotion is deliberate, gated, and recorded (Priority: P2)

The framework maintainer needs the promotion to be a single, reviewable, reversible change: the two CI drift gates (per-package public-surface baselines and the design-token-drift baseline) must be regenerated **in the same commit** so the build stays green, and a decision record must capture exactly what became public, what was intentionally kept internal, and why.

**Why this priority**: The token taxonomy and package surface are the two things most likely to redden CI (per the master plan's risk register). A promotion that lands without regenerating both baselines breaks the build; one that lands without a decision record leaves an undocumented, hard-to-reverse public contract. This story makes the promotion safe and auditable, but it rides on the P1 surface choice.

**Independent Test**: Regenerate both baselines on the feature branch and confirm the build is green; diff the baselines and confirm every added row corresponds to a symbol named in the decision record (and vice-versa) — no orphan public surface, no undocumented promotion.

**Acceptance Scenarios**:

1. **Given** the chosen surface is made public, **When** the per-package surface baselines and the design-token-drift baseline are regenerated, **Then** both CI gates pass green within the same change.
2. **Given** the promotion, **When** the decision record under `docs/product/decisions/` is reviewed, **Then** it names every promoted symbol, every symbol deliberately kept internal, the rationale, and the stability/reversibility commitment.
3. **Given** the baseline diff and the decision record, **When** they are compared, **Then** they agree exactly — every new public row is accounted for and every promised promotion is present.

---

### User Story 3 - Application developers supply a custom intent policy (Priority: P3)

An application developer using the published packages wants a `Danger` button to actually render a divergent (e.g. red) style, by supplying their own intent policy — not by forking the control. Today the F4 seam proves this is reachable, but only from an in-repo test, because the policy type and the resolver entry point are internal. F5 makes the seam public so external consumers can supply a real policy.

**Why this priority**: This realises the user-facing payoff of the F4 seam for external consumers, but it is downstream of the surface choice (P1) and the gating discipline (P2), and the default product behaviour does not depend on it.

**Independent Test**: From a consumer with no internals grant, supply a non-default intent policy that maps `danger` to a divergent colour, resolve a button's style, and confirm the result differs from the neutral-policy result — with zero edits to any control's code.

**Acceptance Scenarios**:

1. **Given** the public intent-policy seam, **When** a consumer supplies a divergent policy and resolves a `danger` button, **Then** the resolved style differs from the default-neutral resolution.
2. **Given** no custom policy is supplied, **When** a control resolves its style, **Then** the default-neutral behaviour is byte-identical to today (the seam is opt-in; the default is unchanged).

---

### Edge Cases

- **A symbol with no current consumer**: the taxonomy contains far more tokens than D2/G3 need today. "Chosen subset" is resolved at **module granularity** (see Assumptions and plan R2): the `DesignTokensExt` taxonomy is promoted **as one coherent unit** because it is all token *values* with no internal helper bindings to keep private, and any leaf omitted from its paired `.fsi` would become **private** — breaking in-repo consumers (conflicting with FR-009). Selectivity is exercised at the **module/candidate** level instead: `ColorPolicy` (F2/127) and the `DesignTokens`↔`DesignTokensExt` unification are the deliberately-deferred candidates, each recorded in the decision record. Promoting *unconsumed candidate modules* wholesale is **not** the goal — it would create an unmaintainable, hard-to-reverse public contract.
- **A promoted symbol that later proves wrong**: because every promotion is named in the decision record and visible in the baseline diff, it is traceable and reversible; there must be no orphan public surface that the record does not explain.
- **Token value vs. token visibility**: promotion changes *visibility only*. No existing token **value** changes, and the public `Theme` record shape is unchanged.
- **Default behaviour under the neutral policy**: making the resolver/seam public must not change rendered output for any consumer who does not opt into a divergent policy — byte-identical at rest and in motion.
- **Naming**: public names are curated in signature files and may differ from the internal names; the public contract must read coherently to an outside consumer, independent of internal generation details.
- **Two gates, one change**: regenerating only one of the two baselines (surface vs. token-drift) leaves the build red; both must move together.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose the **chosen subset** of the Ant-derived token taxonomy (from F1/126) as public API, reachable by external package consumers **without** any internals-visibility grant.
- **FR-002**: The system MUST expose the central style-resolution path (`theme + control-kind + semantic-intent + visual-state(s) → resolved style`) and its **overridable intent-policy seam** (from F4/129) as public API, so consumers and themes can supply a divergent intent policy.
- **FR-003**: The system MUST make a deliberate, recorded decision about the color-validation policy surface (`wcag`/`ant`, from F2/127): either promote the chosen subset to public API, or document in the decision record that it remains internal for this phase and why.
- **FR-004**: Promotion MUST be **surface-only** — **zero** change to any existing token **value**, **zero** change to the public `Theme` record shape, and **byte-identical** rendered output under the default (neutral) policy across every intent and visual state.
- **FR-005**: The per-package public-surface baselines **and** the design-token-drift baseline MUST be regenerated **within the same change**, leaving both CI gates green; the regenerated rows attributable to this feature MUST be **exactly** the deliberately-promoted symbols and nothing else (no incidental surface leakage).
- **FR-006**: A decision record MUST be authored under `docs/product/decisions/` recording **what was promoted**, **what was intentionally kept internal**, the **rationale**, and the **stability / reversibility** commitment for the new public contract.
- **FR-007**: The newly-public surface MUST be declared through the repo's curated signature mechanism (visibility lives in the signature, not in implementation access modifiers), per the project's visibility discipline.
- **FR-008**: The newly-public surface MUST be exercised by tests that reach it **through the public API only** (no internals grant), proving it is genuinely consumable from outside the framework.
- **FR-009**: Symbols **not** chosen for promotion MUST remain internal/additive — still reachable in-repo through the existing internals mechanism — so the proven-internal experiment is not dumped public wholesale.
- **FR-010**: The promoted public surface MUST be documented for an outside consumer. For the hand-curated `StyleResolver` surface this means a doc comment on **every** public member (type, fields, and each `val`). For the **generated** `DesignTokensExt` taxonomy (~130 leaves) documentation is required at **nested-module granularity** — each layer/sub-module (`Seed`, `Map.Light/Dark`, `Alias.*`, `Component.*`, `Space`, `Density`, `Type.*`, `Elevation`) carries a generated doc comment describing the layer's meaning and intended use; per-leaf doc comments are not required (the leaf name + type + the DTCG source are self-describing). This module-granularity choice for the taxonomy is recorded in the decision record.
- **FR-011**: The change MUST NOT introduce any new external/third-party dependency, and MUST NOT add or change any product runtime behaviour beyond making the chosen surface callable.
- **FR-012**: Test pass/skip counts MUST remain consistent — only **additive** new tests for the public-consumption path — with no behavioural regression and a green full suite.

### Key Entities *(include if feature involves data)*

- **Public token surface**: the chosen, documented subset of the F1 taxonomy (seed / map / alias / component layers, plus spacing, density, type scale, elevation) that becomes a public contract. Distinct from the full internal taxonomy, of which it is a deliberately-selected part.
- **Public style resolver & intent-policy seam**: the total resolution path plus the overridable policy that maps semantic intent to a style adjustment. The default policy is intent-neutral (today's behaviour); a non-default policy is what enables intent divergence.
- **Color-policy surface**: the `wcag`/`ant` validation policies — candidate for promotion; final disposition recorded in the decision record.
- **Surface baselines**: the committed per-package public-API baselines and the design-token-drift baseline — the two CI gates that must be regenerated in lock-step.
- **Decision record**: the document under `docs/product/decisions/` that names the promoted/retained surface, the rationale, and the stability commitment; the single source of truth that the baseline diff must agree with.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A consumer that holds **no** internals-visibility grant can name the promoted tokens, invoke the public resolver, and supply a custom intent policy — proven by at least one test that references only the published surface.
- **SC-002**: Both CI gates — the per-package public-surface baseline and the design-token-drift baseline — pass green after a clean regenerate, and **100%** of the baseline rows added by this feature correspond one-to-one to symbols named in the decision record (no orphan rows, no undocumented promotion).
- **SC-003**: **100%** of existing token values are byte-identical before and after; the public `Theme` record shape is unchanged; rendered output is byte-identical under the default neutral policy across the full intent × visual-state cross-product.
- **SC-004**: A decision record exists that enumerates every promoted symbol, every deliberately-retained-internal symbol, and the rationale, and is reviewable on its own (a reviewer can determine the full scope of the new public contract from the record alone).
- **SC-005**: A consumer can make a control render a divergent intent style (e.g. `Danger`) by supplying a public intent policy, with **zero** edits to any control's code.
- **SC-006**: Test pass/skip counts are unchanged except for the additive public-path tests, and the full suite is green (0 failures).
- **SC-007**: Every promoted public symbol is traceable to both the decision record and the baseline diff — the promotion is fully reversible because nothing public is unaccounted-for.

## Assumptions

- **"Chosen subset" = consumer-driven**: the surface promoted is the subset with a real downstream consumer (D2 concrete themes, G3 Ant showcase, external theme/intent authors). Speculative or not-yet-consumed symbols are deliberately kept internal (FR-009). The exact per-symbol list is a planning decision; the spec requires only that the promotion be deliberate, documented, baseline-regenerated, and consumer-justified.
- **Scope spans tokens + resolver + (decision on) policy**: per the F4 spec's deferral note, F5 owns "token/policy/resolver promotion". Token taxonomy (FR-001) and resolver/seam (FR-002) promotion are firmly in scope; the color-policy engine's promotion (FR-003) is evaluated this phase and MAY be recorded as remaining internal — the `--design-system wcag|ant` template parameter (F3/128) already exposes the policy choice at the template level, so promoting the engine itself is only warranted if a consumer needs it.
- **Decision-record location & numbering**: the record lands under `docs/product/decisions/` and continues the existing sequence (the next number after `0003-designsystem-namespace-relocation.md`).
- **Behaviour neutrality**: no `Theme` record shape change, no token-value change, and the default (neutral) policy preserves today's byte-identical rendered output — F5 is a visibility change, not a behavioural one.
- **CI invariants**: the per-package surface-drift gate (committed surface baselines) and the design-token-drift gate are the binding hazards; both are regenerated in the same change, never as a follow-up.
- **Environment/build**: F# on `net10.0`, single solution `FS.GG.Rendering.slnx`, `TreatWarningsAsErrors=true` (code must be warning-clean), headless deterministic tier (no GL context required for the surface/parity checks).
- **Downstream (enabled by F5)**: D2 concrete themes, G3 Ant-styled showcase, and external theme/intent authors. **Out of scope**: building any concrete theme (D2); switching on visible intent divergence in the default product; expanding the `Theme` record; migrating controls beyond what F4 already wired; wiring the color policy as a runtime gate on resolver output.
