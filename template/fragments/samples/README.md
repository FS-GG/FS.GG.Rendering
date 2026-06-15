# Samples Fragment

Adds optional sample content only for the sample-pack profile.

## Pointer interaction sample

The sample-pack profile can demonstrate mouse hover / click / drag /
secondary-click / wheel through the `FS.GG.UI.Controls` pointer front door,
mirroring the framework's `PointerInteractionGallery`: translate the host
`ViewerEvent.Pointer*` stream into a neutral `PointerSample`, reduce it with the
pure `Pointer.update` against the current `LayoutResult`, and lower the resulting
`PointerInteraction` values to `ControlId`-level product messages via
`FS.GG.UI.Controls.Elmish.interpretPointerOutcome`. The sample performs no
coordinate math or hit-testing of its own — the framework owns both.
