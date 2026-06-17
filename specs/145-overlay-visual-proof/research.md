# Research: Overlay Visual Proof

## Decision: Treat Feature 145 as readiness proof, not overlay behavior work

**Rationale**: Feature 144 already delivered the overlay coordinator, widget metadata, host/runtime routing,
product dispatch, deterministic overlay corpus, AntShowcase date-picker flow, and unsupported-host disclosure.
The remaining report action is a real visual artifact on a capable host. Keeping this feature in the harness and
readiness layer avoids reopening P5 behavior and preserves the Tier 2 scope.

**Alternatives considered**:
- Add new overlay behavior while adding visual proof. Rejected because the spec explicitly forbids product-facing
  behavior changes.
- Redesign the overlay coordinator to make visual proof easier. Rejected because pure behavioral proof already
  exists and this feature is about host evidence.

## Decision: Use the existing Feature 144 representative flow as the proof scenario

**Rationale**: The AntShowcase date-picker flow and Feature 144 overlay corpus already connect open state,
selection, focus recovery, hit routing, dispatch, and final closed state. Reusing that scenario keeps proof tied
to the real readiness caveat and avoids an isolated screenshot fixture that proves less than the behavior tests.

**Alternatives considered**:
- Capture every transient widget category. Rejected for this feature because the proof scope is the
  representative Feature 144 readiness gap, not an exhaustive visual matrix.
- Use only a synthetic minimal scene. Rejected because the proof must correlate with the existing integrated
  overlay flow.

## Decision: Require current-run, non-empty, scenario-linked artifacts before claiming visual success

**Rationale**: Existing `FS.GG.UI.Testing.EvidenceReports.validateScreenshotEvidence` already rejects missing
fields, unsupported screenshots that claim artifacts, non-live capture sources for successful screenshots,
non-positive dimensions, and blank pixel validation. Feature 145 should build on that discipline and add
scenario identity, open/closed state, hit decision, focus, product dispatch, and current-run artifact checks.

**Alternatives considered**:
- Accept deterministic readback hashes as visual proof. Rejected because Feature 091 already documents those as
  capability hashes, not pixel artifacts.
- Accept logs plus a limitation record as a visual pass. Rejected because unsupported-host evidence must remain
  environment-limited.

## Decision: Keep native capture and host probing at the viewer/harness edge

**Rationale**: The SkiaViewer skill states that native window and render effects belong at the interpreter edge,
while scene descriptions stay in Scene and Elmish adapter behavior stays in Elmish. `Rendering.Harness.Live`
already probes display/GL/X11 capability and distinguishes live-host runs from deterministic/offscreen tiers.
Feature 145 should extend that edge path rather than placing native capture in pure overlay state or product
models.

**Alternatives considered**:
- Add GL capture calls to `OverlayState.update`. Rejected because the coordinator must remain pure and replayable.
- Add capture code to product state transitions. Rejected because product-owned state should not own framework
readiness evidence.

## Decision: Preserve unsupported-host honesty as a first-class outcome

**Rationale**: Feature 144 already records `Rendering.Harness.Live.overlayVisualLimitation` for no-display or
missing-GL hosts. Feature 145 should replace the caveat only when a capable-host run produces accepted artifacts;
otherwise readiness must keep owner, cause, next proof path, and behavioral trust rationale visible.

**Alternatives considered**:
- Skip unsupported-host runs silently. Rejected because maintainers need to know whether the proof gate is closed
  or still environment-gated.
- Mark unsupported runs as passed when deterministic behavioral tests pass. Rejected because that would convert
  non-visual evidence into a visual claim.

## Decision: Classify failures by evidence layer

**Rationale**: The spec requires maintainers to distinguish environment setup, visual capture, overlay behavior,
and artifact bookkeeping failures. The readiness record should therefore carry a diagnostic category so a blank
PNG, a missing display, an overlay/hit mismatch, and a stale path from a previous run produce different outcomes.

**Alternatives considered**:
- Use one generic failure status. Rejected because it would slow readiness triage and hide whether the next action
  is infrastructure, rendering, overlay behavior, or evidence bookkeeping.

## Decision: Do not add dependencies or public contracts in the planned path

**Rationale**: Existing harness, SkiaViewer capture, and Testing evidence validation are enough for the planned
readiness work. New dependencies or public package contracts would expand maintenance and trigger Tier 1
requirements without a demonstrated need.

**Alternatives considered**:
- Add an external screenshot diffing library. Rejected unless implementation proves existing SkiaSharp PNG
  decoding and current validators are insufficient.
- Add a new public visual-proof API. Rejected for Tier 2; if unavoidable, reclassify to Tier 1 first.
