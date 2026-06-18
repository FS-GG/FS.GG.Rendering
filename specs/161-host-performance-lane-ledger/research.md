# Research: Host Performance Lane Ledger

## Decision: Extend the existing compositor performance/readiness command family

**Rationale**: Features 156, 158, 159, and 160 already establish the P7 evidence vocabulary,
scenario routing, host profile identity, unsupported-host behavior, readiness rendering, and
package validation pattern. Feature 161 should add lane-fact collection and claim-scope rendering to
that path rather than creating a separate validation framework. This keeps the lane ledger close to
the timing runs it qualifies and keeps reviewer evidence in the same readiness package style.

**Alternatives considered**:

- Add a standalone `host-lane-ledger` tool: rejected because it would duplicate command parsing and
  artifact publication, and it could drift from the timing evidence it is supposed to scope.
- Treat lane facts as free-form notes in readiness only: rejected because missing or contradictory
  facts need testable acceptance and rejection rules.
- Fold lane scoping into Feature 160 without a new feature identity: rejected because the report
  names Feature 161 as a separate performance claim gate.

## Decision: Verify the accepted lane from collected facts

**Rationale**: The report names the current lane as X11 `:1` with direct OpenGL on AMD
Radeon/Mesa and stable profile `probe-08a47c01`, but Feature 161 must not accept that label by
memory. Accepted readiness confirms that the timing run's collected facts match the lane before it
uses that lane in claim scope. If facts are missing, stale, contradictory, or collected for a
different run identity, the evidence is excluded.

**Alternatives considered**:

- Trust the prior profile id alone: rejected because the feature exists to expose host-specific
  display, renderer, driver, refresh, package, and load facts beyond the profile label.
- Accept manual reviewer notes as lane proof: rejected because the acceptance rules need automated
  rejection and package-visible diagnostics.
- Re-probe only during readiness assembly: rejected because host facts must be tied to the timing
  run identity, not only the later review environment.

## Decision: Require a complete host fact set for accepted lane-scoped evidence

**Rationale**: The specification requires display server, display identity, renderer identity,
direct rendering status, refresh rate or reason unavailable, driver identity, package version set,
CPU/GPU load notes, known environment limits, host profile, run identity, scenario identity, timing
policy identity, collection time, and artifact locations. This set is large enough to prevent
generalizing across Wayland, indirect GL, missing-display, software-raster, stale-package,
ambiguous-GPU, or noisy-load lanes while staying focused on evidence needed for performance claim
review.

**Alternatives considered**:

- Require only display server and renderer: rejected because package, load, refresh, and run
  identity mismatches can still make timing evidence non-comparable.
- Require exhaustive machine inventory: rejected because unrelated hardware facts would add noise
  without improving P7 claim scoping.
- Allow incomplete facts with warnings: rejected because the spec requires incomplete entries to
  contribute zero accepted lane-scoped performance artifacts.

## Decision: Never aggregate cross-lane timing artifacts

**Rationale**: A scoped performance claim is valid only for the lane that produced the accepted
evidence. Runs from different display servers, renderers, direct-rendering modes, driver strings,
package versions, host profiles, scenario definitions, policies, or run identities must be split or
rejected. The readiness summary may list contextual evidence from other lanes, but it cannot count
those records toward the accepted result for the current lane.

**Alternatives considered**:

- Average cross-lane results and report a broad result: rejected because the report explicitly
  forbids generalizing the X11 AMD/Mesa lane to other lanes.
- Accept cross-lane evidence with a warning: rejected because it would still create a misleading
  performance claim.
- Keep only the best lane result: rejected because discarded contextual failures would reduce
  observability and make future lane work harder.

## Decision: Preserve noisy lane facts without accepting the performance claim

**Rationale**: Complete host facts are useful even when timing remains noisy. Feature 161 should
record those facts and explain why the lane did not accept a shipped claim. This separates lane
completeness from speedup acceptance: a complete lane ledger can satisfy the host-scoping gate, but
the final claim still requires non-noisy timing, Feature 159 net-positive reuse/promotion counters,
and Feature 160 accepted throughput.

**Alternatives considered**:

- Reject noisy entries entirely: rejected because future work needs auditable lane context for noisy
  runs.
- Accept performance when lane facts are complete: rejected because host facts do not prove a
  speedup.
- Hide noisy entries from the summary: rejected because reviewers need to understand why the claim
  remains `performance-not-accepted`.

## Decision: Keep unsupported and unavailable hosts fail-closed

**Rationale**: Feature 161 must preserve the P7 safety boundary. Missing display, indirect
rendering, software rasterization, unknown renderer facts, virtualized or ambiguous presentation,
and other environment limits produce zero accepted lane-scoped performance artifacts while still
writing reviewer-visible diagnostics.

**Alternatives considered**:

- Skip unsupported-host validation for a lane-ledger feature: rejected because unsupported-host
  behavior is part of every P7 readiness closeout.
- Accept unsupported-host environment facts as a performance lane: rejected because such facts
  explain why performance evidence is unavailable, not accepted.
- Fail the whole feature when unsupported-host checks run: rejected because the correct behavior is
  environment-limited/fallback-only evidence with zero accepted performance artifacts.
