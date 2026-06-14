# Phase 0 Research: Verifying Imported Mechanisms

All Technical Context items were resolvable from the existing repository; there are no open `NEEDS CLARIFICATION` markers. This file records the methodology decisions that shape the audit, each as Decision / Rationale / Alternatives.

## D1. What "verify as advertised" means operationally

**Decision**: Split every mechanism's claim into two independent dimensions and verify each separately: **correctness** (output with the mechanism on equals output with it off/bypassed) and **effectiveness** (with the mechanism on, the advertised work-reduction actually occurs, measured by counters, by a meaningful margin over a baseline with it off). A mechanism only earns "works as advertised" when both hold.

**Rationale**: The request's core fear — "does it really work as advertised" — has two distinct failure modes. A cache can be *correct but never hit* (a silent no-op that adds risk and complexity for no benefit) or *effective but wrong* (returns stale results fast). Existing imported tests heavily cover correctness and barely touch effectiveness, so collapsing the two would let the more dangerous, less-tested failure mode pass unexamined.

**Alternatives considered**:
- *Correctness only* (trust the counters): rejected — leaves the central "no-op" worry unaddressed.
- *Wall-clock timing as the effectiveness proof*: rejected for the deterministic tier — timing is noisy and non-deterministic; the code already exposes exact work-reduction counters (`RemeasuredNodeCount`, `MemoHits`, `PictureCacheHits`, `VirtualMaterialized`, `DirtyArea`, replay `stats`). Counters give deterministic, headless, gate-able effectiveness evidence. Real wall-clock timing is reserved for the genuinely timing-shaped claims (frame pacing, frame-rate cap) on the capability tier.

## D2. Trusting the imported tests vs re-verifying them

**Decision**: Treat the imported per-feature suites (Feature091/092/097/113/116/117/120…) as *claimed* evidence, not *accepted* evidence. For each, add a **discriminating-power check**: deliberately bypass the mechanism (flip its oracle flag, or feed an input that must defeat it) and confirm the correctness assertion turns red. A test that stays green when the mechanism is off proves nothing and becomes a finding in its own right.

**Rationale**: The tests arrived with the code from `fs-skia-ui`; their green status here only proves they compile and pass, not that they would catch a regression. Constitution Principle V demands tests that "fail before the change and pass after." Discriminating-power is the cheapest way to confirm an imported test actually constrains the mechanism.

**Alternatives considered**:
- *Re-author every test from scratch*: rejected — wasteful where an imported test already discriminates; the audit's job is to verify the evidence, not duplicate it.
- *Mutation-testing tool*: rejected as heavyweight for this scope; a targeted manual bypass per mechanism (the oracle flags exist precisely for this) gives the same signal without new tooling.

## D3. Effectiveness measurement via existing counters

**Decision**: Drive each work-reduction claim through one `RetainedRender.step` (or `evaluateIncremental`, or `PictureReplayCache.stats`) and assert the relevant field of `WorkReductionRecord`/`FrameMetrics` against a baseline produced with the mechanism disabled. Use three canonical scenarios: **localized change in a large tree** (incremental/damage/memo should touch a small fraction), **repeated render of unchanged content** (caches should reach near-100% hit steady-state), **collection larger than viewport** (virtualization should bound materialized count).

**Rationale**: These counters are the code's own self-reported effectiveness surface and are deterministic (no wall-clock, feature-numbered 104–120). Asserting them enabled-vs-disabled directly answers "is this a no-op?" and is gate-safe headless. The three scenarios map one-to-one onto the spec's User Story 3 acceptance scenarios.

**Alternatives considered**:
- *Trust the counters without an enabled-vs-disabled baseline*: rejected — a counter reading "10 recomputed of 1000" is only meaningful against the 1000-node full baseline; the comparison is the evidence.
- *New bespoke instrumentation*: rejected — the counters already exist; adding more violates simplicity and risks perturbing what is measured.

## D4. Adversarial probes for the subtle failure modes

**Decision**: Add targeted adversarial inputs beyond the happy-path suites: (a) **cache-key completeness** — inputs that differ *only* in a field the key might omit (e.g. font weight for the text cache, a render-affecting attr for memo/picture) must produce a miss + correct fresh result; (b) **determinism under provocation** — repeated runs and reordered-but-equivalent inputs must yield identical output, and `hashScene` must not collide across single-field render-affecting diffs; (c) **settled-animation identity** — a fully-elapsed animation must lower byte-identically to the equivalent static scene.

**Rationale**: These are the failure modes that happy-path tests structurally cannot catch and that the spec's Edge Cases call out (cache-key incompleteness, determinism violations, identity-at-rest). They are where intricate imported code most plausibly hides defects.

**Alternatives considered**: *Rely on FsCheck generators alone*: rejected — generic generators rarely target the single-field-difference and hash-collision cases; hand-built adversarial inputs complement the property tests.

## D5. Detecting "present but dead" mechanisms

**Decision**: For each mechanism, confirm it is actually reached by the live render/control path — not merely present in source. Evidence: a counter that is provably exercised in a default `step` with the mechanism enabled (e.g. `PictureCacheMisses > 0` then `PictureCacheHits > 0` across frames), or a direct call-site trace from the wired entry point. A mechanism whose counters never move on a representative scene with it enabled is reported as "present but dead."

**Rationale**: The spec's first Edge Case. An optimization that exists but is never invoked is a maintenance liability masquerading as a feature; it must be distinguished from one that works.

**Alternatives considered**: *Static reachability analysis only*: rejected as insufficient — reachability in source doesn't prove the live path enables it; a runtime counter movement is the stronger proof.

## D6. Capability-dependent claims (present-mode, frame-rate cap, live present)

**Decision**: Verify timing/pixel-faithfulness claims only through the existing R5 harness tiers (`offscreen` T1, `perf --mode paced-*` T3, `live-x11` T2) on the scheduled/manual cadence wired in spec 005. On a headless runner these degrade-and-disclose: the audit records them as "deferred — requires tier Tn" with rationale, never as passing.

**Rationale**: Constitution Principle VI and the Stage R6 no-overclaim rule. Frame pacing and the frame-rate cap are inherently timing-shaped and cannot be faithfully measured by deterministic counters or on a display-less runner. Reusing the harness avoids duplicating its tiering in test code.

**Alternatives considered**: *Fake a clock to "measure" pacing headlessly*: rejected — would be synthetic timing evidence presented as real; explicitly forbidden. Disclose-and-defer instead.

## D7. Where the audit code and artifacts live

**Decision**: Audit tests go into the existing module test projects (`Controls.Tests`, `Layout.Tests`, `Scene.Tests`, `Elmish.Tests`, `SkiaViewer.Tests`) as `Audit_*.fs` files with an `Audit:` test-name prefix. The two durable deliverables — the claims inventory and the findings report — live under `docs/audit/`. The spec's `contracts/` hold the record schemas the inventory and report conform to.

**Rationale**: The existing test projects already have `InternalsVisibleTo` and the right module references; a new project would re-plumb both for no gain (Principle III). The `Audit_*`/`Audit:` convention keeps the audit independently runnable (`--filter Audit`) without isolating it from the code it audits. Docs under `docs/audit/` sit alongside the repo's other durable docs (`docs/product/`, `docs/validation/`, `docs/ci/`).

**Alternatives considered**: *Dedicated `tests/MechanismAudit.Tests` project*: rejected — duplicates internals-visibility plumbing and module references; the filename/prefix convention plus the unifying report give the same cohesion at lower cost.

## D8. Output: verdicts and the report as the primary deliverable

**Decision**: The primary deliverable is `docs/audit/mechanism-audit.md` — one row per mechanism with a verdict from the fixed set {works-as-advertised, benefit-overstated, not-working-or-no-op, unverifiable-and-why}, a severity for any divergence (correctness defect > silent no-op > overstated benefit > cosmetic), the evidence reference (test name and/or harness run), and a recommended action (fix / simplify / remove / re-scope claim / defer-to-tier). Tests are evidence; the report is the decision surface.

**Rationale**: The audit exists to drive decisions about what to trust, fix, or simplify. A fixed verdict/severity vocabulary makes the report scannable and the coverage summary (SC-008) computable.

**Alternatives considered**: *Leave results as raw test output*: rejected — no verdict, no severity, no recommendation leaves the maintainer with the original uncertainty (the spec's User Story 4 rationale).
