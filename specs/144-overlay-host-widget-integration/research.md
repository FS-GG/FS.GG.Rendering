# Research: Overlay Host Widget Integration

## Decision: Treat Feature 144 as a continuation of Feature 143, not a replacement

**Rationale**: Feature 143 already shipped the pure `OverlayState` coordinator, supported surface kinds,
dismissal policies, focus scopes, diagnostics, replay log, and `ControlRuntime.attachOverlayEffects`. The
remaining report scope names widget metadata, Pointer/Focus integration, Controls.Elmish interpretation,
AntShowcase wiring, and real offscreen visual proof. Reusing the coordinator avoids churn and keeps the feature
bounded to P5 integration.

**Alternatives considered**:
- Redesign `OverlayState` before host integration. Rejected because it would reopen solved coordinator work and
  delay user-visible transient behavior.
- Implement widget-local open/close logic per control. Rejected because it would duplicate routing and dispatch
  policy across controls.

## Decision: Derive one transient metadata record per supported widget instance

**Rationale**: Runtime routing needs a stable surface identity, trigger identity, anchor evidence, layer priority,
dismissal policy, focus scope, modal flag, and selection/command dispatch mapping. The existing
`OverlaySurface` record is the correct target shape for open surfaces; widget authors should supply or derive the
metadata needed to construct it. Validation can then fail missing metadata explicitly.

**Alternatives considered**:
- Infer metadata only from control `Kind` strings. Rejected because focus, anchor, selection, and policy vary by
  instance and product-owned state.
- Require products to manually create `OverlaySurface` values for all widgets. Rejected because common controls
  should provide safe defaults while still exposing product-owned visibility.

## Decision: Keep product-owned visibility explicit

**Rationale**: Existing generated products and controls already carry open/value state. The runtime should emit
`RequestOpenStateChange`, `DispatchProductMessage`, and focus/evidence effects, then let the product update its
model. This preserves compatibility and makes state transitions audit-friendly.

**Alternatives considered**:
- Store widget open state inside `ControlRuntimeModel`. Rejected because it silently takes ownership away from
  products and violates the Feature 143 bridge design.
- Mutate widget attributes during routing. Rejected because authored views are immutable descriptions.

## Decision: Route pointer, keyboard, and focus through the host boundary

**Rationale**: `Pointer.update`, `Focus.route`, retained hit testing, and `ControlsElmish.routeFocusedKey` are
already the deterministic host seams. Overlay integration should decorate those seams with topmost decisions,
modal blocking, dismissal policy, focus scope traversal, and product dispatch interpretation. This keeps pure
reducers testable and avoids adding I/O to `OverlayState.update`.

**Alternatives considered**:
- Put native viewer event handling directly in `OverlayState`. Rejected because the coordinator must remain
  host-independent and replayable.
- Add a parallel overlay event loop. Rejected because it would split product dispatch and parity logic from the
  existing Controls.Elmish path.

## Decision: Use AntShowcase date picker as the reference live flow

**Rationale**: The radical rendering report names the AntShowcase date picker as the reference consumer. It
exercises trigger activation, calendar placement, keyboard/pointer navigation, selection, close request, focus
recovery, no-stale-overlay verification, and evidence output in one product-owned state flow.

**Alternatives considered**:
- Use a synthetic minimal fixture only. Rejected because the feature requires a live generated-product reference
  path, not only coordinator evidence.
- Use every widget as a showcase reference. Rejected for this feature because metadata coverage for all widgets is
  required, but one complete reference flow keeps the P5 integration slice focused.

## Decision: Preserve direct/retained/cache equivalence as the rendering oracle

**Rationale**: The repository already treats direct rendering, retained rendering, cache-enabled mode, and
cache-disabled mode as parity oracles. Overlay routing must not bypass those checks. Evidence should compare
visible output, hit order, focus transitions, product messages, diagnostics, and replay logs for equivalent
scripts.

**Alternatives considered**:
- Validate only `OverlayState` replay logs. Rejected because host/widget integration can regress paint order,
  retained hit targets, or cache behavior even when the pure coordinator is correct.
- Require GL visual proof on every machine. Rejected because host support is environment-sensitive; unsupported
  hosts must record owner/cause/next proof path rather than block all headless validation.

## Decision: No new runtime dependency

**Rationale**: The feature composes existing packages and test infrastructure. Adding a dependency would expand
the Tier 1 surface without solving a necessary problem.

**Alternatives considered**:
- Add an external UI overlay manager or focus library. Rejected because the repository already owns the relevant
  pure routing and Elmish boundary.
