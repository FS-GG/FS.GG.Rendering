# Data Model: Interaction Overlay State

## TransientSurfaceKind

Finite category for supported interactive transient surfaces.

**Fields**
- `Menu`
- `ContextMenu`
- `SplitButtonMenu`
- `ComboDropdown`
- `AutoCompleteSuggestions`
- `DatePickerCalendar`
- `ColorPickerPalette`
- `DialogModal`

**Validation**
- Coverage must include all eight categories.
- Tooltip/toast-like surfaces are excluded unless explicitly promoted to interactive behavior.

## OverlaySurfaceId

Stable identity for an open transient surface.

**Fields**
- `SurfaceId`: stable control or generated overlay id.
- `ParentSurfaceId`: optional parent for nested overlays.
- `TriggerId`: control that opened the surface.

**Validation**
- Must be stable across equivalent replay scripts.
- Nested surfaces must identify their parent so focus can recover without closing unrelated surfaces.

## Trigger

Control or interaction source that opens a transient surface.

**Fields**
- `ControlId`: trigger identity.
- `Enabled`: whether activation is allowed.
- `ActivationSource`: pointer, keyboard, or product-driven open state.
- `RecoveryTarget`: preferred focus target after dismissal.

**Validation**
- Disabled triggers do not open surfaces.
- Missing recovery targets fall back to a parent surface or clear focus with diagnostics.

## AnchorEvidence

Resolved relationship between a surface and the trigger or declared anchor used for placement.

**Fields**
- `AnchorId`: trigger or declared anchor identity.
- `AnchorBounds`: resolved frame-space rectangle when present.
- `SurfaceBounds`: resolved frame-space rectangle when placed.
- `Placement`: chosen placement relative to anchor.
- `NoFit`: optional no-fit disclosure.
- `FrameFingerprint`: frame evidence tying the placement to a render/layout pass.

**Validation**
- Missing anchors close the surface or prevent opening safely with diagnostics.
- Moved anchors update placement on the next frame.
- No stale hit target remains after anchor removal.

## DismissalPolicy

Rules deciding which events may close a surface and whether lower content may receive the same event.

**Fields**
- `Escape`: allow, block, or ignore.
- `OutsidePointer`: allow, block, or ignore.
- `SelectionCompletion`: close or keep open.
- `ExplicitClose`: allow or block.
- `AnchorRemoval`: close or keep with diagnostic.
- `PassThroughAfterDismissal`: whether the dismissing event may continue to lower content.

**Validation**
- Topmost eligible surface handles dismissal before lower layers.
- Blocked dismissals emit evidence when diagnostics are requested.
- Escape and outside pointer affect only one eligible surface per input.

## FocusScope

Focusable region owned by an open surface.

**Fields**
- `SurfaceId`: owning surface.
- `Stops`: focusable controls in deterministic traversal order.
- `InitialFocus`: preferred first focus target.
- `RecoveryTarget`: trigger, parent surface target, or explicit fallback.
- `TrapMode`: modal trap, non-modal local scope, or no focus capture.

**Validation**
- Modal scopes cycle Tab and Shift+Tab internally.
- Non-interactive surfaces do not capture focus.
- Dismissal recovers to a valid target or clears with diagnostics.

## OverlaySurface

Runtime description of one open surface.

**Fields**
- `Id`: `OverlaySurfaceId`.
- `Kind`: `TransientSurfaceKind`.
- `LayerPriority`: layer/portal priority.
- `Anchor`: `AnchorEvidence`.
- `DismissalPolicy`: policy for close attempts.
- `FocusScope`: scope behavior.
- `Modal`: whether lower content is blocked.
- `OpenReason`: pointer activation, keyboard activation, product open state, nested open.

**Validation**
- Layer priority participates in topmost hit-test order.
- Modal surfaces block lower content until dismissed or completed by policy.
- Closed surfaces leave no visible or hit-testable overlay content.

## OverlayState

Durable runtime model for all open surfaces.

**Fields**
- `OpenSurfaces`: ordered bottom-to-top stack of `OverlaySurface`.
- `ActiveSurface`: topmost interactive surface, if any.
- `FocusedControl`: current focus owner as understood by overlay routing.
- `RecentTransitions`: bounded deterministic transition evidence.
- `Diagnostics`: accumulated actionable diagnostics.

**Validation**
- Stack order is deterministic.
- Active surface is the topmost eligible interactive surface.
- Equivalent replay inputs produce equivalent state and evidence.

## OverlayMsg

Pure transition input to the overlay update function.

**Fields**
- `OpenRequested`: surface kind, trigger, anchor, focus scope, dismissal policy.
- `DismissRequested`: surface id or topmost, reason.
- `PointerRouted`: pointer event with topmost hit evidence.
- `KeyRouted`: normalized key with focused/active surface evidence.
- `SelectionCompleted`: selected value or command evidence.
- `AnchorChanged`: updated anchor bounds.
- `AnchorRemoved`: missing anchor evidence.
- `FocusTargetRemoved`: stale focus target evidence.
- `Reset`: close all overlay state for window/session reset.

**Validation**
- Update is pure and total.
- Invalid messages produce diagnostics rather than exceptions.
- Selection completion emits at most one product dispatch.

## OverlayEffect

Data emitted by overlay update for host/product interpretation.

**Fields**
- `DispatchProductMessage`: product message or event to emit.
- `RequestFocus`: control id to focus or clear.
- `RequestOpenStateChange`: explicit product-owned visibility change request.
- `ReportDiagnostic`: overlay diagnostic.
- `ConsumeInput`: whether lower routing should stop.
- `AllowPassThrough`: whether lower content may receive the original input.

**Validation**
- Effects are ordered.
- Consumed topmost input does not also dispatch to lower content.
- Product messages remain deterministic and exactly-once.

## TopmostHitDecision

Evidence for pointer routing through layers and overlays.

**Fields**
- `Input`: pointer sample or interaction.
- `CandidateLayers`: ordered layer/surface candidates.
- `ChosenTarget`: topmost surface/control or miss.
- `BlockedByModal`: optional blocking surface.
- `OutsideOfSurface`: surface id when the event is outside the active surface.

**Validation**
- Hit order matches paint/layer order.
- Covered content is blocked when a modal surface is open.
- Outside pointer actions route through dismissal policy first.

## InteractionReplayLog

Deterministic validation artifact for a scripted interaction.

**Fields**
- `Inputs`: ordered pointer/key/product events.
- `OverlayTransitions`: ordered state changes.
- `FocusTransitions`: focus enter, movement, recovery, or clear events.
- `ProductDispatches`: emitted messages/commands.
- `DismissalReasons`: close attempts and outcomes.
- `Diagnostics`: ordered diagnostics.
- `HitDecisions`: ordered topmost hit target decisions.
- `RenderEvidence`: direct/retained/cache parity references.

**Validation**
- Three equivalent runs produce byte-identical logs.
- Cache-enabled and cache-disabled runs produce equivalent user-visible behavior and evidence.
- Pre-existing limitations are identified separately from new overlay behavior.

## ReferenceDatePickerFlow

Showcase-level end-to-end flow proving the first complete overlay workflow.

**Fields**
- `InitialClosedState`
- `OpenAction`
- `CalendarNavigation`
- `SelectedDate`
- `Dismissal`
- `FocusRecovery`
- `NoStaleOverlayEvidence`
- `ReadinessEvidence`

**Validation**
- Date selection dispatches exactly once.
- Calendar closes and focus returns to trigger/field.
- Closed render after flow has no stale visible or hit-testable overlay content.
