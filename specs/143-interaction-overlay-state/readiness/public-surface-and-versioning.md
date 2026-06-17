# Public Surface And Versioning

Date: 2026-06-17

Status: RECORDED for coordinator slice.

Public Controls additions:

- `TransientSurfaceKind`
- `DismissalReason`
- `OverlayActivationSource`
- `DismissalRule`
- `SelectionCompletionPolicy`
- `AnchorRemovalPolicy`
- `TrapMode`
- `OverlaySurfaceId`
- `OverlayTrigger`
- `AnchorEvidence`
- `DismissalPolicy`
- `FocusScope`
- `OverlaySurface`
- `TopmostHitDecision`
- `OverlayTransition`
- `FocusTransition`
- `ProductDispatch`
- `DismissalOutcome`
- `InteractionReplayLog`
- `OverlayState`
- `OverlayEffect`
- `OverlayMsg`
- `OverlayState` module
- `OverlayRuntimeBridge`
- Overlay diagnostic code cases.

Compatibility:

- Existing product-owned visibility remains compatible.
- The runtime does not silently mutate product state; it emits explicit effects.
- New union types use `RequireQualifiedAccess` where case names would collide with existing Controls cases.

Versioning:

- This is additive public API in `FS.GG.UI.Controls`.
- A package version bump is required before publishing a package containing this surface.

Baseline:

- `dotnet fsi scripts/refresh-surface-baselines.fsx` passed and updated `tests/surface-baselines/FS.GG.UI.Controls.txt`.
