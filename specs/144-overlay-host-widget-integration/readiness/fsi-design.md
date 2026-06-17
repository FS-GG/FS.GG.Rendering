# FSI Design

The public contract is additive.

- `Control.fsi` adds `TransientWidgetMetadata`, `WidgetActivationRequest`, and `TransientWidget` helpers for attaching, collecting, validating, and translating widget metadata into the existing `OverlaySurface` contract.
- `Pointer.fsi` adds `PointerOverlayRoutingResult` and `Pointer.routeOverlay` for topmost pointer routing through active overlay state.
- `Focus.fsi` adds `FocusRecoveryDecision` and `Focus.recoverOverlayFocus` for stale overlay focus recovery evidence.
- `ControlRuntime.fsi` adds `OverlayRuntimeDispatchRecord` and `ControlRuntime.overlayDispatchRecords`.
- `ControlsElmish.fsi` adds overlay effect/outcome interpretation functions that map coordinator effects into adapter commands.

No existing public type was removed or renamed. The type baseline delta is additive in `FS.GG.UI.Controls`; `FS.GG.UI.Controls.Elmish` added module functions but no new public type entry in the current baseline format.
