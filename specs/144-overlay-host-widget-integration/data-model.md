# Data Model: Overlay Host Widget Integration

## TransientWidgetMetadata

Behavioral description produced by a supported transient control instance.

**Fields**
- `SurfaceKind`: menu, context menu, split-button menu, combo dropdown, auto-complete suggestions, date-picker
  calendar, color-picker palette, or dialog modal.
- `SurfaceId`: stable surface control identity.
- `TriggerId`: stable trigger identity.
- `AnchorId`: trigger or declared anchor identity.
- `LayerPriority`: ordering value used with Feature 140 layer/portal behavior.
- `DismissalPolicy`: rules for Escape, outside pointer, selection completion, explicit close, and anchor removal.
- `FocusScope`: focus stops, initial focus, recovery target, and trap mode.
- `Modal`: whether lower content is blocked.
- `SelectionDispatch`: optional command/value dispatch mapping.
- `VisibilityState`: product-owned open/closed state as observed by the rendered control.

**Validation**
- All eight supported surface categories must have complete metadata in fixtures.
- Disabled triggers must not produce an open request or selection dispatch.
- Closed metadata must not alter closed visible output or hit behavior.
- Missing required fields are readiness failures, not silent fallback behavior.

## WidgetActivationRequest

Product-visible request emitted when pointer or keyboard activation targets a transient trigger.

**Fields**
- `TriggerId`
- `SurfaceId`
- `ActivationSource`: pointer, keyboard, product-owned open, or nested open.
- `RequestedOpenState`
- `Diagnostic`: optional reason when activation is ignored.

**Validation**
- Enabled triggers request open state through product messages/effects.
- Disabled triggers emit no product state-change or selection messages.
- Equivalent activation scripts produce stable requests.

## OverlayRoutingFrame

Snapshot of the current rendered frame used to route overlay-aware input.

**Fields**
- `OverlayState`: existing Feature 143 overlay coordinator state.
- `RetainedRender`: current retained frame when available.
- `Layout`: current computed layout and hit-test boxes.
- `FocusOrder`: deterministic focus stops for the frame.
- `EventBindings`: product dispatch bindings for the rendered frame.
- `LayerEvidence`: paint/hit order evidence from layer/portal ordering.

**Validation**
- Routing never fetches live I/O inside pure reducers.
- Direct and retained paths resolve equivalent hit targets and product bindings.
- Anchor movement updates placement evidence before the next eligible interaction.

## PointerOverlayDecision

Topmost pointer-routing result for a pointer sample or folded interaction.

**Fields**
- `Input`
- `CandidateLayers`
- `ChosenTarget`
- `OutsideOfSurface`
- `BlockedByModal`
- `DismissalOutcome`
- `PassThrough`
- `RuntimeMessages`
- `ProductMessages`
- `Diagnostics`

**Validation**
- Topmost eligible surface receives pointer input before covered content.
- Modal surfaces block covered content unless policy dismisses first.
- Lower content receives input only when the topmost policy allows pass-through.

## KeyboardOverlayDecision

Routing result for a normalized key event while overlays or focused controls are active.

**Fields**
- `InputKey`
- `FocusedControl`
- `ActiveSurface`
- `KeyRouting`: activate, navigate, traverse, cancel, or fallthrough.
- `DismissalOutcome`
- `FocusRequest`
- `ProductMessages`
- `Consumed`
- `Diagnostics`

**Validation**
- Escape dismisses only the topmost eligible surface.
- Tab and Shift+Tab cycle inside modal scopes.
- Selection/command dispatch occurs exactly once.
- Fallthrough keys still reach the existing host `MapKey` path when not consumed.

## FocusRecoveryDecision

Deterministic focus transition after open, dismissal, selection completion, anchor removal, or stale target
removal.

**Fields**
- `From`
- `To`
- `Reason`
- `RecoveryTargetKind`: trigger, parent surface, fallback, or explicit no-focus.
- `Diagnostic`

**Validation**
- Dismissal recovers to trigger, parent surface, documented fallback, or explicit no-focus state.
- Missing recovery targets emit diagnostics.
- Stale focus targets are removed without leaving focus on an invalid control.

## ProductDispatchRecord

Audit record for a product-visible state change, selection, command, or focus request.

**Fields**
- `SurfaceId`
- `DispatchKey`
- `Payload`
- `DispatchKind`: open request, close request, selection, command, focus request, or diagnostic.
- `InputCorrelation`

**Validation**
- Each user action emits at most one selection or command.
- A selection that closes a surface may emit one selection plus one compatible close request.
- Duplicate dispatch attempts are diagnosed and excluded from final product messages.

## ReferenceDatePickerFlow

End-to-end AntShowcase date-picker script and evidence bundle.

**Fields**
- `InitialClosedState`
- `OpenAction`
- `NavigationActions`
- `SelectedDate`
- `DismissalReason`
- `FocusRecovery`
- `FinalClosedState`
- `NoStaleOverlayEvidence`
- `VisualProof`
- `ReplayLog`

**Validation**
- The calendar opens in association with the trigger.
- Navigation and selection dispatch exactly one date selection.
- Selection closes the calendar when policy requires it.
- Final closed render has no stale visible or hit-testable calendar content.

## OverlayReadinessEvidence

Feature readiness record for deterministic and visual proof.

**Fields**
- `MetadataCoverage`
- `PointerKeyboardFocusResults`
- `ReplayLogs`
- `DirectRetainedCacheParity`
- `ClosedStateCompatibility`
- `SurfaceBaselineImpact`
- `VisualProofArtifact`
- `UnsupportedHostLimitations`
- `ScopeReview`

**Validation**
- Three equivalent replay runs produce byte-identical logs.
- At least 100 overlay fixtures/generated scenes prove parity across direct, retained, and cache modes.
- Public surface validation reports either zero changes or documented migration/versioning rationale.
- Unsupported visual hosts record owner, cause, next proof path, and why behavioral evidence is trustworthy.

## CompatibilityChangeRecord

Documentation for any intentional contract, baseline, diagnostic, or authoring behavior change.

**Fields**
- `ChangedSurface`
- `CompatibilityImpact`
- `MigrationGuidance`
- `VersioningRationale`
- `BaselineEvidence`
- `Owner`

**Validation**
- Required for every intentional public API or baseline change.
- Must be linked from readiness before the feature is complete.
