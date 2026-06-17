module Feature144OverlayFixtures

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let bounds x y width height =
    { X = x
      Y = y
      Width = width
      Height = height }

let anchorFor (id: ControlId) =
    { AnchorId = id
      AnchorBounds = Some(bounds 10.0 20.0 120.0 32.0)
      SurfaceBounds = Some(bounds 10.0 56.0 220.0 180.0)
      Placement = "bottom-start"
      NoFit = None
      FrameFingerprint = Some 144UL }

let missingAnchor (id: ControlId) =
    { AnchorId = id
      AnchorBounds = None
      SurfaceBounds = None
      Placement = "bottom-start"
      NoFit = None
      FrameFingerprint = Some 144UL }

let metadata kind (surfaceId: ControlId) layer isOpen enabled modal : TransientWidgetMetadata =
    let triggerId = surfaceId + "-trigger"

    { SurfaceKind = kind
      SurfaceId = surfaceId
      ParentSurfaceId = None
      TriggerId = triggerId
      AnchorId = triggerId
      LayerPriority = layer
      DismissalPolicy = if modal then OverlayState.modalDismissalPolicy () else OverlayState.defaultDismissalPolicy ()
      FocusScope =
        { SurfaceId = surfaceId
          Stops = [ surfaceId + "-item-1"; surfaceId + "-item-2" ]
          InitialFocus = Some(surfaceId + "-item-1")
          RecoveryTarget = Some triggerId
          TrapMode = if modal then ModalTrap else LocalScope }
      Modal = modal
      SelectionDispatchKey = Some "selected"
      VisibilityState = isOpen
      TriggerEnabled = enabled }

let allMetadata =
    [ metadata TransientSurfaceKind.Menu "menu" 10 true true false
      metadata TransientSurfaceKind.ContextMenu "context-menu" 20 true true false
      metadata TransientSurfaceKind.SplitButtonMenu "split-button-menu" 30 true true false
      metadata TransientSurfaceKind.ComboDropdown "combo-dropdown" 40 true true false
      metadata TransientSurfaceKind.AutoCompleteSuggestions "auto-complete-suggestions" 50 true true false
      metadata TransientSurfaceKind.DatePickerCalendar "date-picker-calendar" 60 true true false
      metadata TransientSurfaceKind.ColorPickerPalette "color-picker-palette" 70 true true false
      metadata TransientSurfaceKind.DialogModal "dialog-modal" 100 true true true ]

let surfaceFrom (metadata: TransientWidgetMetadata) =
    TransientWidget.toSurface (anchorFor (metadata.AnchorId)) metadata

let openOne (metadata: TransientWidgetMetadata) =
    OverlayState.update (OpenRequested(surfaceFrom metadata)) (OverlayState.init ())

let widgetControls () =
    let menu =
        FS.GG.UI.Controls.Typed.Menu.view
            { FS.GG.UI.Controls.Typed.Menu.defaults with
                Id = Some "menu"
                Items = [ "One"; "Two" ] }
        |> Widget.toControl

    let contextMenu =
        FS.GG.UI.Controls.Typed.ContextMenu.view
            { FS.GG.UI.Controls.Typed.ContextMenu.defaults with
                Id = Some "context"
                Items = [ "Open"; "Close" ] }
        |> Widget.toControl

    let splitButton =
        FS.GG.UI.Controls.Typed.SplitButton.view
            { FS.GG.UI.Controls.Typed.SplitButton.defaults with
                Id = Some "split"
                Text = "Run"
                IsOpen = true
                Items = [ { Key = "copy"; Label = "Copy" } ] }
        |> Widget.toControl

    let comboProps =
        { FS.GG.UI.Controls.Typed.ComboBox.defaults "combo" with
            Items = [ "A"; "B" ] }

    let comboModel, _ = FS.GG.UI.Controls.Typed.ComboBox.init comboProps
    let combo = FS.GG.UI.Controls.Typed.ComboBox.view comboProps comboModel |> Widget.toControl

    let autoComplete =
        DataEntry2.AutoComplete.create
            [ DataEntry2.AutoComplete.value "a"
              DataEntry2.AutoComplete.transientMetadata "auto" true true ]

    let datePicker =
        FS.GG.UI.Controls.Typed.DatePicker.view
            { FS.GG.UI.Controls.Typed.DatePicker.defaults with
                Id = Some "date"
                Value = Some(DateOnly(2026, 6, 17))
                IsOpen = true }
        |> Widget.toControl

    let colorPicker =
        FS.GG.UI.Controls.Typed.ColorPicker.view
            { FS.GG.UI.Controls.Typed.ColorPicker.defaults with
                Id = Some "color"
                Swatches = [ { Name = "Blue"; Color = Colors.rgb 0uy 0uy 255uy } ] }
        |> Widget.toControl

    let dialog =
        FS.GG.UI.Controls.Typed.Dialog.view
            { FS.GG.UI.Controls.Typed.Dialog.defaults with
                Id = Some "dialog"
                IsOpen = true
                Children = [ FS.GG.UI.Controls.Typed.Tooltip.view { FS.GG.UI.Controls.Typed.Tooltip.defaults with Text = "body" } ] }
        |> Widget.toControl

    [ menu; contextMenu; splitButton; combo; autoComplete; datePicker; colorPicker; dialog ]
