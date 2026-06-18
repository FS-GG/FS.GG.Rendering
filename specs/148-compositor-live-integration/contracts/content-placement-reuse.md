# Contract: Content and Placement Reuse

## Scope

This contract defines how retained compositor boundaries distinguish visual content from placement
so stable moving content can be reused without serving stale pixels.

## Boundary Identity

Each candidate boundary records:

- Stable boundary identity from the retained tree.
- Content identity over render-affecting inputs.
- Placement identity over bounds, transform, clip, scale, and layer placement.
- Previous placement identity when movement occurs.
- Observation window and stability evidence.
- Candidate size/work estimate.
- Current tier and last parity verdict.

Content identity and placement identity are separate. Content changes require fresh output;
placement-only changes may reuse content only when old and new covered regions are damaged.

## Promotion Rules

A boundary may promote or reuse only when:

- Content identity is stable for the configured observation window.
- The boundary is expensive or large enough to save meaningful work.
- Reuse passes full-frame oracle parity.
- Bookkeeping overhead stays within accepted threshold.
- The target tier is supported by the active proof/host/resource state.

Promotion decisions must record reason, observed frames, expected saved work, measured overhead,
target tier, and diagnostics.

## Movement Rules

Placement-only movement must:

- Reuse visual content only when content identity is unchanged.
- Damage both the old covered region and the new covered region.
- Update placement diagnostics so reviewers can trace the old/new regions.
- Fall back to fresh paint or full redraw when old/new coverage cannot be represented safely.

## Invalidation Rules

Reuse is invalid when any render-affecting input changes, including:

- Content fingerprint.
- Theme or design token values.
- Text shaping/provider/version bucket.
- Image/resource identity or load status.
- Host profile when the reuse artifact is host-specific.
- Snapshot resource validity or budget state.

## Demotion Rules

A boundary must demote or remain unpromoted when:

- Content churns repeatedly.
- Parity fails.
- It produces no measured benefit.
- Simple/churning corpus overhead exceeds threshold.
- Resource budgets are exceeded.
- Proof or host capability is missing.

Demotion decisions must be visible in diagnostics and readiness evidence.

## Acceptance Tests

- Stable content with unchanged placement promotes only after observation and parity evidence.
- Placement-only movement reuses content at the new placement and damages old/new regions.
- Content change with similar bounds rejects stale content and produces fresh output.
- Churning boundaries demote or remain unpromoted with recorded reason.
- Same-seed reuse decisions produce stable scenario ids, decision reasons, and artifact references.
