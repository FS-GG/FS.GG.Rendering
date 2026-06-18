# Contract: Content and Placement Identity

## Scope

This contract defines how Feature 159 distinguishes unchanged retained content from changed
placement, scroll, or transform evidence. It is the safety contract for placement-only reuse.

## Content Identity Rules

Content identity records the visual content of a retained boundary in local boundary coordinates.
It includes render-affecting inputs such as local geometry, paint, text, font, visual state, local
clip content, opacity, scene node shape, and content identity algorithm version.

Content identity does not by itself accept reuse. It must be paired with host profile, run
identity, scenario identity, retained layer state, placement evidence, and parity status.

Content identity changes require content re-recording or safe fallback. A content-change scenario
must never reuse obsolete retained content just because placement identity is unchanged.

## Placement Identity Rules

Placement identity records where unchanged content is drawn. It includes absolute box, transform,
scroll or offset evidence, scale, framebuffer mapping, coverage region, and placement identity
algorithm version.

Placement-only movement can reuse recorded content only when:

- Current and prior content identities match.
- Current and prior host profile, run id, package version, and scenario definition match.
- The retained layer is resident and current.
- Old and new covered regions are present in damage evidence.
- The renderer can replay retained content in local coordinates and apply the new placement.
- Parity passes against the equivalent safe output.

If any condition is missing or unverifiable, the frame re-records content, demotes, bypasses, or
falls back with a primary reason.

## Stale and Ambiguous Identity

Reuse is rejected for:

- Missing content identity.
- Missing placement identity.
- Stale content or placement identity.
- Cross-profile, cross-run, or package-version mismatch.
- Scenario-definition mismatch.
- Retained content unavailable or evicted.
- Unsupported transform, clip, or scale mapping.
- Parity mismatch.
- Resource limitation.

Rejected identity records must not contribute to accepted Feature 159 counters.

## Damage Interaction

Placement-only reuse must include both the old coverage region and the new coverage region in
damage evidence. Content changes use content re-recording and normal damage classification.
Resize, scale changes that cannot be mapped safely, full-frame invalidation, missing retained
backing, or unsafe damage force full redraw.

## Acceptance Tests

- Stable content with changed placement reuses the prior recorded content and records old/new
  placement damage.
- Stable content with changed scroll offset reuses content only when coverage and parity pass.
- Content change with unchanged placement re-records content.
- Content and placement changing together re-records or rejects reuse unless fresh content is
  recorded before placement is applied.
- Stale, missing, cross-profile, unsupported, or parity-failing identity rejects reuse with a
  stable primary reason.
