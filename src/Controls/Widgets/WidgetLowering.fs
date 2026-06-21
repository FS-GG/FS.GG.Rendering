namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

// Feature 105 (US1, FR-001/FR-002/FR-004): the single home for the typed-widget
// lowering helpers that were verbatim-duplicated across the per-family `*Lowering`
// modules. `module internal` with no `.fsi` — assembly-internal, off the public
// surface (the established `module internal SceneRenderer` precedent), compiled
// before every consuming widget module (see Controls.fsproj). A future fix to key
// application or the string-event/accessibility adapters now changes one body and
// every widget module inherits it.
module internal WidgetLowering =
    // Apply a stable key when the typed `Props` carried an `Id`, else pass through.
    let withKeyOpt id control =
        match id with
        | Some key -> FS.GG.UI.Controls.Control.withKey key control
        | None -> control

    // A string-event adapter: bind `eventKind`, reading the event's typed `Nav` string (free-text
    // edit or moved-selection item via `navText`), defaulting an absent value to "".
    let onString (eventKind: string) (map: string -> 'msg) : Attr<'msg> =
        Attr.onWith eventKind (fun event -> ControlEvent.navText event |> Option.defaultValue "" |> map)

    // A string-list event adapter (a single typed `Nav` string lifted to a one-element list).
    let onStringList (eventKind: string) (map: string list -> 'msg) : Attr<'msg> =
        Attr.onWith eventKind (fun event ->
            ControlEvent.navText event
            |> Option.map (fun value -> [ value ])
            |> Option.defaultValue []
            |> map)

    // The shared accessibility-metadata builder: an explicit role + accessible name carrying a
    // focusable keyboard affordance (Enter/Space activation + the given navigation keys). FR-009.
    let a11y (role: AccessibilityRole) (nameSource: string) (navigationKeys: string list) : Attr<'msg> =
        Attr.accessibility (
            Accessibility.metadata
                role
                nameSource
                [ "normal" ]
                None
                (Accessibility.keyboard true [ "Enter"; "Space" ] navigationKeys)
                None
                None)

    let private focusScope surfaceId triggerId trapMode =
        { SurfaceId = surfaceId
          Stops = [ surfaceId + "-item-1"; surfaceId + "-item-2" ]
          InitialFocus = Some(surfaceId + "-item-1")
          RecoveryTarget = Some triggerId
          TrapMode = trapMode }

    let transientMetadata
        (kind: TransientSurfaceKind)
        (surfaceId: ControlId)
        (triggerId: ControlId)
        (isOpen: bool)
        (enabled: bool)
        (layerPriority: int)
        (modal: bool)
        (dispatchKey: string option)
        : Attr<'msg> =
        let trapMode = if modal then ModalTrap else LocalScope

        TransientWidget.attribute
            { SurfaceKind = kind
              SurfaceId = surfaceId
              ParentSurfaceId = None
              TriggerId = triggerId
              AnchorId = triggerId
              LayerPriority = layerPriority
              DismissalPolicy = if modal then OverlayState.modalDismissalPolicy () else OverlayState.defaultDismissalPolicy ()
              FocusScope = focusScope surfaceId triggerId trapMode
              Modal = modal
              SelectionDispatchKey = dispatchKey
              VisibilityState = isOpen
              TriggerEnabled = enabled }
