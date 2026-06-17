# Research: Modifier Layer IR Foundation

## Decision: Use an Internal-First Modifier and Layer Model

**Decision**: Prove modifier, local z-order, portal, and layer-host semantics behind the existing Controls
assembly boundary before exposing broad public Scene IR changes.

**Rationale**: The active architecture report explicitly recommends P2 as "internal modifier/layer model +
glyph-run type spike" and warns that public Scene churn should follow proof, not precede it. Feature 139 now
provides a single current-node assembly owner, so P2 can add cleaner semantics in one place and lower to the
existing `Scene` output while compatibility is proven.

**Alternatives considered**:
- Add public `SceneNode.Modified`, `Scene.Layers`, and `SceneNode.Portal` immediately. Rejected as the default
  because downstream consumers can exhaustively match `SceneNode`; broad public DU churn needs proof and
  migration evidence first.
- Keep the feature as documentation only. Rejected because acceptance requires executable modifier, portal,
  ordering, invalidation, and glyph-run proof evidence.
- Wait for full retained-renderer unification. Rejected because P3 depends on P2 semantics being stable.

## Decision: Fold Ordered Modifiers as Immutable Values

**Decision**: Represent supported effects as immutable DU/record values with an explicit fold order,
classification, and normalization pass.

**Rationale**: Immutable values give structural equality, deterministic fingerprints, cheap comparison, and
plain F# tests. The report's prior-art summary favors value modifiers over closures or per-update factories
because order is semantic and equality is the reuse signal. A pure fold also keeps the feature within the
constitution's simplicity rule.

**Alternatives considered**:
- Model modifiers as functions from `Scene -> Scene`. Rejected because functions are not structurally
  comparable, cannot be fingerprinted directly, and make diagnostics/order evidence opaque.
- Add ad hoc fields for each effect to `Control`. Rejected because it recreates the scattered special cases
  P2 is meant to remove.
- Normalize only during rendering. Rejected because tests, diagnostics, invalidation, and fingerprints need the
  same normalized representation.

## Decision: Classify Effects for Layout, Paint, and Order Invalidation

**Decision**: Each supported effect carries a classification: layout-affecting, paint-affecting,
order-affecting, or a combination. The invalidation evidence and metrics must read the same classification
table used by the composition fold.

**Rationale**: The spec requires paint-only and order-only changes to avoid false layout invalidation while
layout-affecting effects invalidate both layout and paint. A single table prevents drift between rendering,
retained metrics, and tests.

**Alternatives considered**:
- Infer classification from attribute names in multiple call sites. Rejected because feature 097 already proved
  dirty-set drift is a risk.
- Treat every modifier as layout-affecting. Rejected because it would pass visual tests while violating the
  work-reduction and invalidation requirements.
- Treat classification as documentation only. Rejected because SC-001 and US1 require executable evidence.

## Decision: Use One Ordering Function for Paint and Hit Testing

**Decision**: Build one internal ordered contribution stream for in-flow children, local z-order, and portal
layers. Final paint order and hit-test priority must be derived from that stream.

**Rationale**: Portal layers must paint above normal content and receive hits before lower layers. Local z-order
must remain scoped to siblings within the same parent. Deriving hit order from paint order prevents separate
branching rules from drifting and directly satisfies FR-007.

**Alternatives considered**:
- Keep `Control.hitTest` as layout-tree-only and special-case overlays separately. Rejected because it cannot
  prove cross-layer hit order and preserves the old overlay special case.
- Add a second hit-order implementation in RetainedRender. Rejected because P2 exists to remove duplicated
  composition semantics.
- Make z-order global. Rejected because it breaks common UI semantics; only portals escape ancestor stacking
  and clipping.

## Decision: Replace Overlay Semantics with Portal Lowering, Not Overlay Interaction State

**Decision**: Re-express existing overlay-like output as portal content targeting ordered layer hosts. Keep
open/close/dismiss/focus-trap behavior out of this feature.

**Rationale**: Feature 137 introduced a deferred overlay render pass, but P2's goal is to generalize the
composition model, not implement the later interaction manager. Portal lowering can preserve visible overlay
compatibility now and give feature R4 a stable target later.

**Alternatives considered**:
- Implement dropdown/menu/modal state in this feature. Rejected because overlay interaction state is explicitly
  out of scope.
- Keep `isOverlayNode` as the primary mechanism. Rejected because it is a hardcoded kind string and cannot
  express multiple layer classes.
- Make `ZIndex` escape clipping. Rejected because local z-order and out-of-tree portals are separate concepts.

## Decision: Keep Legacy Scene Forms Compatible Through Lowering

**Decision**: Existing clip, translate, perspective, cached subtree, text, and overlay forms must lower into the
new composition foundation and remain compatible by default.

**Rationale**: The public Scene and Controls surfaces already have committed callers and tests. P2 reduces risk
only if old forms continue to work while the implementation gains a cleaner internal representation.

**Alternatives considered**:
- Remove legacy node forms immediately. Rejected because it would force a broad migration before proof.
- Keep old and new forms as independent render paths. Rejected because this repeats the duplicated-builder bug
  class the radical plan is trying to eliminate.
- Preserve only visual pixels, not diagnostics/fingerprints. Rejected because the spec requires diagnostics and
  cache fingerprints to remain equivalent where behavior is equivalent.

## Decision: Make Glyph-Run Proof Minimal and Deterministic

**Decision**: Add the smallest public Scene glyph-run proof surface needed to prove deterministic measurement,
drawing, diagnostics, and fingerprinting for representative text. Do not add HarfBuzz, bidi, full font
fallback expansion, or line breaking.

**Rationale**: Future text shaping and portable rendering need a stable shaped-text representation, but the
current feature is a type/proof spike. The proof must cross the Scene/SkiaViewer package boundary, so the
data shape belongs in `Scene.fsi` with a curated surface baseline and the drawable proof belongs in the
exhaustive Skia painter. The repository already has bundled-font measurement/drawing seams, text cache keys,
and Skia text rendering tests that can prove shape consistency without taking on full R7.

**Alternatives considered**:
- Add full HarfBuzz shaping now. Rejected as explicitly out of scope and dependency-expanding.
- Keep glyph-run data internal or only in test fixtures. Rejected because future cache/protocol work needs a
  package-owned Scene vocabulary that SkiaViewer can draw and fingerprint.
- Change all text rendering to glyph runs immediately. Rejected because existing deterministic fallback behavior
  must remain compatible unless a public migration is documented.

## Decision: Use Disclosure Ledgers for Public or Pixel Changes

**Decision**: Any public surface change or pixel baseline change must be listed with compatibility impact,
migration guidance, and versioning recommendation before the feature is considered ready.

**Rationale**: This is a Tier 1 architecture feature. Surface baselines and goldens are not incidental; they
are the evidence that tells downstream maintainers whether a change is compatible, deprecated, or intentionally
breaking.

**Alternatives considered**:
- Treat preview package status as permission for silent public churn. Rejected because the constitution still
  requires curated `.fsi`, surface baselines, and migration notes.
- Rebaseline pixels without rationale. Rejected by FR-014 and the constitution's test evidence principle.
- Defer compatibility notes to implementation review. Rejected because tasks need concrete artifacts to target.
