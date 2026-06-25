# Feature Specification: Symbology Legibility Linter

**Feature Branch**: `194-symbology-legibility-linter`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "next item in the plan" — milestone **M7 (governance)** of `docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`: a **legibility linter** that scores a per-game channel mapping against the fixed channel-grammar capacities (§4) and warns on overload. M1–M5 (the agent loop, spec 192) and M6 (the live board, spec 193) are complete; this feature delivers the first of M7's three independent threads — the linter — and explicitly leaves the Badge/Ring grammars and label text (the rest of M7) deferred.

## User Scenarios & Testing *(mandatory)*

The symbology system (specs 192/193) gives a designer or agent a fixed channel grammar — stroke hue → faction, motion rhythm → activity, size → magnitude, silhouette+sigil → class/identity, rotation → heading, stroke width → threat, interior gradient → charge, belly arc → health, tail beads → speed, dash → inspection state, corner mount → boolean status — and a render→eyeball→tweak loop. Each channel has a **reliable level count** (the number of distinct values the eye can pre-attentively separate). Today the only check that a mapping respects those capacities is a human eyeballing the board against the legibility rules in the `fs-gg-symbology` skill. This feature adds a **mechanical** check: a pure linter that scores the produced symbols against the capacities and reports overloads, so the human/agent critique step has an objective backstop.

### User Story 1 - Score a symbol set for legibility overload and get an actionable report (Priority: P1)

A designer (or agent) has applied their per-game mapping to a roster, producing a set of symbol descriptions (the channel values each unit will render with). Before approving the board, they want to know — without eyeballing — whether any channel is carrying more distinct levels than the eye can reliably separate, or whether any unit's channel values fall outside the range the grammar can encode. They pass the produced symbol set to the linter and receive a structured **legibility report**: per-channel a count of the distinct levels used versus that channel's reliable capacity, a list of findings (each naming the channel, a severity, a human-readable reason, and which unit(s) triggered it), and an overall verdict (clean / has warnings).

**Why this priority**: This is the foundational capability and the minimum viable product — the linter itself. Without a pure, deterministic scoring of a symbol set against the fixed channel capacities, there is nothing to integrate into the loop and nothing to gate on. A standalone report is valuable on its own: it turns the subjective "is this board overloaded?" question into a reproducible, pinnable answer over the same channel grammar the rest of the system already uses.

**Independent Test**: Construct several symbol sets by hand — (a) a within-capacity set spanning the channels, (b) a set that uses more distinct levels on one channel than its capacity (e.g., more distinct factions than the hue capacity, or more tail-bead counts than the bead capacity), and (c) a set with an out-of-domain unit (e.g., a magnitude/threat/charge/health outside its valid range, or a zero/negative size). Run the linter on each and assert: (a) yields no findings and a clean verdict, (b) yields a finding that names the overloaded channel and points at the contributing units, and (c) yields a finding that names the out-of-domain channel and unit. Run the linter twice on the same set and assert the report is identical.

**Acceptance Scenarios**:

1. **Given** a symbol set whose every channel uses no more distinct levels than that channel's reliable capacity, **When** the linter scores it, **Then** it reports zero overload findings and a clean overall verdict.
2. **Given** a symbol set in which one categorical/ordered channel uses more distinct levels than its reliable capacity (e.g., distinct factions > the hue capacity, or distinct tail-bead counts > the bead capacity), **When** the linter scores it, **Then** it emits a finding that names exactly that channel, reports the used-vs-capacity counts, and identifies the contributing units, while channels within capacity produce no finding.
3. **Given** a symbol set containing a unit whose channel value is outside the grammar's encodable range (e.g., a normalised magnitude outside its valid band, a tail-bead/speed count beyond what the symbol can legibly show, or a zero/negative drawable size), **When** the linter scores it, **Then** it emits a finding naming that channel and that unit with an appropriate severity, and the unit's degenerate state is reported rather than crashing the linter.
4. **Given** the same symbol set, **When** the linter scores it twice (in the same process or across processes), **Then** the two reports are identical — the linter is a pure, deterministic function of its input with no wall-clock, randomness, or IO.
5. **Given** the report from any run, **When** a caller inspects it, **Then** each finding exposes a machine-readable channel identity and severity (so the caller can filter or gate) alongside a human-readable message, and the report exposes an overall pass/has-warnings signal.

---

### User Story 2 - Use the linter as the mechanical backstop in the design loop's critique step (Priority: P2)

The `fs-gg-symbology` skill drives the render→eyeball→tweak loop; its CRITIQUE step (step 4) today is a human self-check against the written legibility rules ("faction separable? class distinct? any channel overloaded beyond its reliable level count?"). The designer/agent wants that step to actually **run** the linter on the symbol set the current mapping produces, so overload is caught mechanically before the board is presented for review, and so the unit of change stays the per-game mapping (tweak the mapping until the linter is clean), never the fixed grammar.

**Why this priority**: US1 makes the linter exist and usable; this story makes it part of the established workflow so every loop iteration benefits from it consistently. It depends on US1 (the linter must exist before the loop can call it) and is therefore lower priority, but it is what turns "a linter exists" into "the design loop catches overload automatically." It also proves the linter agrees with prior human judgment: the already-approved roster from the M5 dry-run must lint clean.

**Independent Test**: Confirm the orchestrating guidance is present and consistent across the project's mirrored skill trees (the linter is referenced in the CRITIQUE step with the recipe to run it) and passes the project's skill-parity check. Run the linter on the previously approved symbol set from the M5 dry-run / M6 board and assert it reports clean (no overload). Take a deliberately overloaded variant of that mapping's output and assert the linter surfaces concrete findings an agent can act on by tweaking the mapping.

**Acceptance Scenarios**:

1. **Given** the design loop's CRITIQUE step, **When** an iteration runs, **Then** the documented protocol invokes the linter on the symbol set the current mapping produces and treats its findings as the mechanical complement to the human eyeball check, before the board is presented for review.
2. **Given** the previously approved symbol set (the M5 dry-run final set / the M6 board roster), **When** the linter scores it, **Then** it reports clean — the linter agrees with the prior human approval rather than contradicting an already-shipped design.
3. **Given** a linter report with overload findings, **When** the loop tweaks the design, **Then** the unit of change is the per-game mapping (re-mapping stats onto channels to spread or reduce levels), never the fixed grammar/library internals.
4. **Given** the orchestrating skill, **When** the project's skill trees are checked, **Then** the linter guidance is present and consistent across every mirrored tree and the skill-parity check passes.

---

### Edge Cases

- **Empty symbol set**: scoring a board with no units returns a report with no findings and a clean verdict (vacuously legible), never a crash or a divide-by-zero.
- **All-identical roster**: a board where every unit has identical channel values uses exactly one distinct level per channel — far under every capacity — so it lints clean.
- **Degenerate / zero-area unit**: a unit with zero or negative drawable size (which the grammar already renders as a visible placeholder) is reported as a degenerate-unit finding rather than crashing the linter; scoring continues for the remaining units.
- **Continuous channels**: channels the eye reads as a magnitude/gradient rather than as categories (heading rotation, the health arc) are not subject to a distinct-level overload finding; they are only flagged when a unit's value is outside the encodable range.
- **Custom faction colours**: each distinct custom affiliation colour counts as a distinct level on the faction-hue channel, so a roster that invents many custom colours can exceed the hue capacity and is flagged.
- **Whole-board motion load**: when the scored board carries motion rhythms (an animated board), more than one simultaneously-active rhythm across the board is reported against the motion-channel budget; a per-symbol rhythm is structurally single (the grammar applies one motion per symbol) and is never itself flagged as a stack.
- **Advisory, never blocking**: the linter only produces a report; it never mutates a symbol, never alters a rendered board, and never raises on valid-but-overloaded input. Whether warnings block is the caller's choice, made from the report's severities — not forced by the linter.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a pure legibility linter that takes a produced symbol set (the channel values of each unit, optionally paired with each unit's motion rhythm for an animated board) and returns a structured legibility report, as a deterministic function of its input with no wall-clock, randomness, or IO.
- **FR-002**: The linter MUST encode the fixed channel-grammar capacities (the §4 reliable-level counts and which channels are categorical/ordered versus continuous) as fixed data drawn from the same grammar the symbol library already defines, so the linter and the renderer reason over one shared channel vocabulary.
- **FR-003**: The linter MUST detect **level overload**: for each categorical or ordered channel (e.g., faction/hue, class/silhouette, identity sigil, inspection state, boolean mount, speed/tail-beads, and the ordered magnitude channels), it MUST count the distinct levels used across the symbol set and emit a finding when that count exceeds the channel's reliable capacity, naming the channel and reporting the used-vs-capacity counts.
- **FR-004**: The linter MUST detect **out-of-domain values**: a unit whose channel value falls outside the range the grammar can legibly encode (e.g., a normalised magnitude — threat, charge, health — outside its valid band, a tail-bead/speed count beyond the legible maximum, or a zero/negative drawable size) MUST produce a finding naming the channel and the offending unit.
- **FR-005**: The linter MUST report a **degenerate unit** (zero or negative drawable area — the placeholder case from the symbol grammar) as a finding rather than crashing, and MUST continue scoring the remaining units.
- **FR-006**: Each finding MUST carry a machine-readable channel identity and a severity (at minimum a "warning" advisory level and an "error"/critical level for values the grammar cannot encode at all), a human-readable message, and the identity (index/handle) of the unit(s) that triggered it, so a caller can filter, sort, or gate on findings programmatically.
- **FR-007**: The report MUST expose, in addition to the findings, a per-channel usage summary (distinct levels used versus capacity for each channel) and an overall verdict signal (clean versus has-warnings) derived from the findings, so a caller can both inspect detail and make a one-line pass/fail decision.
- **FR-008**: The linter MUST be **advisory**: scoring a symbol set MUST NOT mutate any symbol description, MUST NOT alter any rendered board, and MUST NOT raise on valid input (including valid-but-overloaded input). Whether warnings are treated as blocking is a decision left to the caller, made from the report's severities.
- **FR-009**: The linter MUST treat continuous channels (those the eye reads as a magnitude/gradient rather than as separable categories — at minimum heading rotation and the health arc) as exempt from level-overload findings, flagging them only on out-of-domain values (FR-004).
- **FR-010**: When the scored board carries motion rhythms, the linter MUST evaluate whole-board **motion load** against the motion-channel budget (more than one simultaneously-active rhythm across the board is an overload), while never flagging a single per-symbol rhythm as a stack (the grammar applies one motion per symbol structurally).
- **FR-011**: Scoring an empty symbol set MUST return a clean report with no findings (vacuously legible), and scoring an all-identical roster MUST report one distinct level per channel and lint clean.
- **FR-012**: The linter MUST live in the pure layer of the symbology stack — it depends only on the existing channel/symbol-description types and performs no rendering, no rasterisation, no GPU work, and no IO — so it never pulls raster/host machinery into the pure symbol vocabulary.
- **FR-013**: All new capability MUST be reachable through the project's existing public package surface and MUST land specification-first (interface before implementation) with the new public surface captured in the symbology surface baseline; delivering the linter MUST NOT change the existing core scene / viewer / controls / canvas public surfaces, and MUST NOT change the existing symbology rendering surface (`token`/`animate`/`gallery`/`filmstrip`) behaviour.
- **FR-014**: The previously approved symbol set from the M5 dry-run / M6 board MUST lint clean under the linter — the mechanical check MUST agree with the prior human approval of that design rather than contradicting an already-shipped board.
- **FR-015**: The `fs-gg-symbology` orchestrating skill MUST be updated so the design loop's CRITIQUE step invokes the linter on the symbol set the current mapping produces (the mechanical complement to the human eyeball check), keeping the unit of change the per-game mapping and never the grammar; the updated guidance MUST be present and consistent across the project's mirrored skill trees and MUST pass the project's skill-parity check.
- **FR-016**: Scope MUST remain the **Directional Token** grammar only; the Badge and Ring alternative grammars and label text (the remaining M7 threads) are out of scope for this feature.

### Key Entities *(include if feature involves data)*

- **Channel capacity table**: the fixed data the linter encodes — for each channel of the §4 grammar, its reliable level count and whether it is categorical, ordered, or continuous. The stable reference the linter scores against; drawn from the same grammar the symbol library defines.
- **Symbol set under test**: the produced channel values of a roster (each unit's symbol description, optionally paired with its motion rhythm) — the input the linter scores. This is the *output* of a per-game mapping, not the mapping function itself (which is product/loop code outside the library).
- **Legibility finding**: a single reported issue — a machine-readable channel identity, a severity (warning / error), a human-readable reason, and the offending unit identity(ies). The unit of action: the designer/agent tweaks the mapping until findings clear.
- **Channel usage summary**: per channel, the distinct levels used versus that channel's capacity — the evidence behind overload findings and a quick read of how close each channel is to its budget.
- **Legibility report**: the linter's whole output — the findings, the per-channel usage summary, and an overall verdict (clean / has-warnings). Pure and reproducible from the symbol set alone.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Scoring the same symbol set any number of times (in-process and across processes) produces an identical report — 100% reproducibility, with no wall-clock, randomness, or IO dependency.
- **SC-002**: For every categorical/ordered channel the linter scores, a symbol set crafted to exceed that channel's reliable capacity by one or more levels produces a finding that names exactly that channel and reports the used-vs-capacity counts, while every channel that is within capacity produces no finding (no false positives on a within-capacity board).
- **SC-003**: A symbol set with a unit whose channel value is out of the encodable domain (out-of-band magnitude, excess speed/bead count, or zero/negative size) produces a finding naming the channel and the offending unit, and the linter completes the scan for the remaining units without crashing.
- **SC-004**: Scoring an empty symbol set and scoring an all-identical roster each return a clean report (no findings); a degenerate/zero-area unit is reported as a finding rather than crashing — every edge case is handled without exception.
- **SC-005**: The previously approved symbol set (the M5 dry-run final set / the M6 board roster) lints clean under the linter, demonstrating the mechanical check agrees with the prior human approval.
- **SC-006**: Each finding is independently machine-actionable: a caller can, from the report alone, filter findings by channel and by severity and derive a single pass/has-warnings decision, without parsing human-readable text.
- **SC-007**: Delivering this feature changes only the symbology public-surface baseline (the new linter surface); the existing core scene / viewer / controls / canvas public surfaces and the existing symbology rendering behaviour show zero drift, verified by the surface gate and the existing symbology tests staying green.
- **SC-008**: The updated orchestrating skill is present and consistent across all mirrored skill trees and passes the skill-parity check, with the CRITIQUE step referencing the linter.

## Assumptions

- **Scope = M7 legibility linter only**: this feature delivers the first of M7's three independent threads. The **Badge/Ring alternative grammars** and **label text** (the other two M7 threads) remain out of scope and deferred, as do M6's already-shipped concerns.
- **The linter scores the mapping's output, not its source**: because the per-game `stats → symbol description` mapping is product/loop code that lives outside the pure library, the linter analyses the produced symbol set (the channel values, optionally with motion) that the mapping yields over a roster — the observable, pure surface — rather than inspecting the mapping function itself.
- **Capacities come from the fixed §4 channel grammar**: the reliable-level counts (faction-hue ~7, motion ~6, size ~4, threat/charge/speed ordered ~4, dash/mount inspection ~3, silhouette/sigil from the available enum set; rotation and health continuous) are taken from the existing grammar table the symbol library and `fs-gg-symbology` skill already document; exact numeric thresholds and the precise channel→capacity encoding are an implementation detail for `/speckit-plan`, bounded by that table.
- **Advisory, not a gate**: "warn on overload" means the linter returns a report with severities; it does not itself fail a build or block a render. Any gating (e.g., treating a warning as a CI failure) is a separate caller decision exposed through the report, not imposed by the linter.
- **Pure layer, Token grammar only**: the linter is pure (no IO/GL/raster) and operates over the existing Directional-Token channel types; it adds no new third-party dependency and does not re-open the fixed grammar.
- **Specification-first with a moving surface baseline**: the linter lands interface-first with semantic tests fail-before/pass-after and a regenerated symbology surface baseline, with zero drift on every other public surface (mirrors spec 192's surface discipline).
- **Skill mirrored across all skill trees**: the updated `fs-gg-symbology` guidance is authored once and mirrored to every skill tree that carries the first-party skills, gated by the skill-parity check.
- **Exact project/module home deferred to `/speckit-plan`**: whether the linter is a new module in the existing pure `FS.GG.UI.Symbology` library or a sibling pure component is an implementation decision for planning; the binding constraint is that it stays pure, depends only on the channel/symbol types, and touches no core or rendering surface.
- **Validation environment**: the linter is pure logic, fully testable headlessly (no GL, no raster); the "approved set lints clean" evidence reuses the existing M5/M6 roster already in the tree.
