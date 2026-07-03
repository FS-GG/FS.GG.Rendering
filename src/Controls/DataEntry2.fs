namespace FS.GG.UI.Controls

module DataEntry2 =

    let private onPayload eventKind (map: string -> 'msg) : Attr<'msg> =
        Attr.onWith eventKind (fun ev -> map (ControlEvent.navText ev |> Option.defaultValue ""))

    module Cascader =
        let create attrs = Control.create "cascader" attrs
        let onChange map = onPayload "onChange" map

    module AutoComplete =
        let create attrs = Control.create "auto-complete" attrs
        let value value = Attr.value value
        let transientMetadata controlId isOpen enabled =
            let surfaceId = controlId + "-suggestions"
            let triggerId = controlId + "-trigger"

            { SurfaceKind = TransientSurfaceKind.AutoCompleteSuggestions
              SurfaceId = surfaceId
              ParentSurfaceId = None
              TriggerId = triggerId
              AnchorId = triggerId
              LayerPriority = 50
              DismissalPolicy = OverlayState.defaultDismissalPolicy ()
              // Issue #56: this helper lowers no keyed suggestion controls, so it can name no real
              // focus stop — an honestly empty scope replaces the fabricated `surfaceId + "-item-N"`
              // phantoms. Suggestions are product-supplied; keyed content would carry its own ids.
              FocusScope =
                { SurfaceId = surfaceId
                  Stops = []
                  InitialFocus = None
                  RecoveryTarget = Some triggerId
                  TrapMode = LocalScope }
              Modal = false
              SelectionDispatchKey = Some "onChange"
              VisibilityState = isOpen
              TriggerEnabled = enabled }
            |> TransientWidget.attribute
        let onChange map = onPayload "onChange" map

    module Upload =
        let create attrs = Control.create "upload" attrs
        let text value = Attr.text value
        let onChange map = onPayload "onChange" map
