# Research: Fix Render Lag

## Decision: Optimize retained frame preparation instead of adding another runner

**Rationale**: Feature 173 already added the accepted live responsiveness command, and `SecondAntShowcase.App.RenderLagProbe` already isolates `button-click` and `page-change` scenarios with `FS_GG_RENDER_LAG_TRACE`. The new feature needs to reduce the measured frame-preparation cost beneath those commands, not fork measurement infrastructure.

**Alternatives considered**: Adding a new benchmark harness was rejected because it would duplicate the live runner and risk producing a second evidence contract. Treating probe traces alone as acceptance evidence was rejected because the spec requires latency budgets, parity, and environment-limited classification.

## Decision: Keep the work Tier 2 with no public API change

**Rationale**: The likely hot path is internal retained rendering and metadata assembly (`RetainedRender.step`, `RetainedRender.init`, `ControlsElmish` frame metrics, and sample evidence commands). Existing public metrics already expose phase facts and timing contributions. Preserving public surface keeps the change scoped to performance and parity.

**Alternatives considered**: Adding new public timing fields was rejected unless implementation proves the existing `FrameMetrics`, `ResponsivenessTimingContribution`, live summary JSON, and probe trace are insufficient. Adding a new dependency was rejected because existing `System.Diagnostics`, viewer timing, and JSON writers cover the need.

## Decision: Make metadata work proportional to changed or required visual work

**Rationale**: The spec points at second-scale stalls even when little visible work changes. The retained renderer already carries node identity, render fragments, layout results, bounds, event bindings, dirty regions, caches, and work-reduction counters. The fix should preserve correctness while avoiding repeated full-tree collection of metadata or assembly for unchanged parents on every affected frame.

**Alternatives considered**: Disabling diagnostics or skipping metadata was rejected because interaction routing, accessibility-facing data, and evidence must remain equivalent. Caching everything without invalidation facts was rejected because stale hit targets or diagnostics would break parity.

## Decision: Preserve current render, routing, and accessibility semantics as the oracle

**Rationale**: FR-003 and SC-005 require behavior-neutral performance improvements. Deterministic tests can compare retained optimized output, bounds, event bindings, bound ids, diagnostics, hit testing, and sample interaction outcomes against the existing full/rendered result for the representative scenarios and edge cases.

**Alternatives considered**: Accepting small visual deltas for speed was rejected because the feature explicitly allows only improved response time. Limiting parity to screenshots was rejected because routing and accessibility metadata can regress without an obvious pixel difference.

## Decision: Use two complementary evidence paths

**Rationale**: Deterministic tests are reliable for work scaling and parity but do not prove desktop presentation latency. Visible desktop runs prove input-to-visible budgets but can be unavailable in CI. The feature therefore requires both: automated deterministic regression tests plus live evidence or an explicit environment-limited result.

**Alternatives considered**: Using only live runs was rejected because they are environment-dependent and weaker for exact work attribution. Using only headless deterministic `Perf.runScript` was rejected because it cannot support accepted live responsiveness claims.

## Decision: Compare against the documented 2026-06-19 baseline in readiness artifacts

**Rationale**: The success criteria require percentage reductions from the 2026-06-19 traces. The implementation phase should preserve those baseline values in readiness notes, then record optimized measurements for the same scenarios and phase names so reviewers can verify the >= 80% non-paint preparation reduction and first-frame improvement.

**Alternatives considered**: Re-baselining after the fix was rejected because it would hide the required improvement claim. Using unrelated pages or synthetic interactions was rejected because the spec names button activation and page navigation.

## Decision: Keep environment-limited reporting fail-closed

**Rationale**: Unsupported or headless environments cannot support accepted input-to-visible claims. Existing Feature 173 behavior already writes `summary.json`, `summary.md`, `records.jsonl`, and `environment.md` with non-accepted readiness. This feature should preserve and test that behavior.

**Alternatives considered**: Treating missing presentation timing as zero or accepting substitute runs was rejected as false evidence. Skipping live evidence without a classified limitation was rejected by FR-007 and SC-006.

## Decision: Defer paint/backend optimization unless the same repeated preparation work causes it

**Rationale**: The spec distinguishes paint cost from frame-preparation cost. The retained renderer should reduce repeated pre-paint work first and report paint/presentation separately. If large paint cost remains after preparation is fixed, that is a follow-up unless it is caused by the same repeated retained metadata work.

**Alternatives considered**: Broad compositor or Skia backend changes were rejected for this feature because they would expand risk, evidence scope, and public behavior without first proving the known retained-preparation bottleneck is fixed.
