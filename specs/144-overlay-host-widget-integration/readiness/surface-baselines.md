# Surface Baselines

`dotnet fsi scripts/refresh-surface-baselines.fsx` passed on 2026-06-17.

Changed baseline:

- `tests/surface-baselines/FS.GG.UI.Controls.txt`

Additive public type entries include:

- `FS.GG.UI.Controls.TransientWidgetMetadata`
- `FS.GG.UI.Controls.WidgetActivationRequest`
- `FS.GG.UI.Controls.TransientWidget`
- `FS.GG.UI.Controls.PointerOverlayRoutingResult`
- `FS.GG.UI.Controls.FocusRecoveryDecision`
- `FS.GG.UI.Controls.FocusRecoveryTargetKind`
- `FS.GG.UI.Controls.OverlayRuntimeDispatchRecord`

`FS.GG.UI.Controls.Elmish` stayed at 25 public types because the new API is module-function surface, not a new public type entry in this baseline format.
