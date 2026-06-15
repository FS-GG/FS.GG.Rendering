namespace FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

module Accessibility =
    let keyboard focusable activationKeys navigationKeys =
        { Focusable = focusable
          ActivationKeys = activationKeys
          NavigationKeys = navigationKeys }

    let contrast foreground background ratio requiredRatio =
        { Foreground = foreground
          Background = background
          Ratio = ratio
          RequiredRatio = requiredRatio }

    let metadata role nameSource state focusOrder keyboard contrast navRange =
        { Role = role
          NameSource = nameSource
          State = state
          FocusOrder = focusOrder
          Keyboard = keyboard
          Contrast = contrast
          Navigation = navRange
          // Feature 114 (FR-012): the default metadata carries no collection total/position; a
          // virtualized control (DataGrid) sets it explicitly from its logical model at the build site.
          Collection = None }

    let roleFor kind =
        match kind with
        | "text-block"
        | "label"
        | "badge"
        | "validation-message" -> StaticText
        | "button"
        | "icon-button" -> Button
        | "text-box"
        | "text-area"
        | "numeric-input" -> TextBox
        | "check-box"
        | "switch" -> CheckBox
        | "radio-group" -> RadioGroup
        | "slider" -> Slider
        | "list-view"
        | "list-box"
        | "multi-select-list"
        | "combo-box"
        | "tree-view" -> List
        | "data-grid"
        | "table" -> Grid
        | "menu"
        | "context-menu"
        | "toolbar" -> Menu
        | "tabs" -> Tab
        | "dialog"
        | "overlay" -> Dialog
        | "progress-bar"
        | "spinner" -> Progress
        | "image"
        | "icon" -> Image
        | "line-chart"
        | "bar-chart"
        | "pie-chart"
        | "scatter-plot" -> Chart
        | "graph-view" -> Graph
        // Qualified: `Custom` now also names `StyleClass.Custom` (feature 093). This match
        // returns an `AccessibilityRole`.
        | _ -> AccessibilityRole.Custom

    // Feature 094 (E4), Research R1: traversal keys (Tab / Shift+Tab) are ENGINE-level — derived
    // from the computed tab order, NOT per-control `NavigationKeys`. Seeding every focusable control
    // with `["Tab"; "Shift+Tab"]` (the prior default) made every control consume Tab under FR-007, so
    // global traversal could never fire. `NavigationKeys` is now reserved for INTRA-control arrows
    // (slider / radio / menu / list / tab / grid); activation-only controls (Button) carry empty
    // navigation. Every focusable role keeps at least one operable key set so `validate` stays honest.
    let keyboardFor role focusable =
        if not focusable then
            keyboard false [] []
        else
            let activation, navigation =
                match role with
                | Button -> [ "Enter"; "Space" ], []
                | CheckBox -> [ "Enter"; "Space" ], []
                | Slider -> [], [ "ArrowLeft"; "ArrowRight" ]
                | RadioGroup -> [], [ "ArrowUp"; "ArrowDown"; "ArrowLeft"; "ArrowRight" ]
                | Tab -> [], [ "ArrowLeft"; "ArrowRight" ]
                | Menu -> [ "Enter"; "Space" ], [ "ArrowUp"; "ArrowDown" ]
                | List -> [ "Enter"; "Space" ], [ "ArrowUp"; "ArrowDown" ]
                | Grid -> [ "Enter"; "Space" ], [ "ArrowUp"; "ArrowDown"; "ArrowLeft"; "ArrowRight" ]
                // Text controls: printable keys are handled by the E1 text seam BEFORE Focus.route;
                // Enter is the commit affordance that keeps the control operable (and `validate`-valid).
                | TextBox -> [ "Enter" ], []
                | Dialog -> [ "Enter"; "Space" ], []
                | Chart
                | Graph -> [ "Enter" ], [ "ArrowLeft"; "ArrowRight" ]
                // Custom / any other focusable role: a generic activation affordance.
                | StaticText
                | Image
                | Progress
                | AccessibilityRole.Custom -> [ "Enter"; "Space" ], []

            keyboard true activation navigation

    // Structural / decorative container kinds carry no role of their own (they lower to
    // `AccessibilityRole.Custom`), but they are NOT interactive — a layout container must not be a
    // focus stop, or `Focus.order` would treat the wrapper as a single tab stop and never reach the
    // focusable controls inside it (feature 094). They are explicitly non-focusable.
    let private structuralKinds =
        [ "stack"; "grid"; "dock"; "wrap"; "panel"; "separator"; "column"; "row"; "container"; "group"; "scroll"; "spacer" ]

    let defaultFor kind label =
        let role = roleFor kind
        let focusable =
            if List.contains kind structuralKinds then
                false
            else
                match role with
                | StaticText
                | Image
                | Progress -> false
                | _ -> true

        // Feature 100 (R5), FR-007: a slider declares the DEFAULT-step NavRange { Step = 0.1;
        // Min = 0.0; Max = 1.0 } so the pre-R5 hardcoded host constant (navStep = 0.1, clamp 0..1)
        // is reproduced byte-identically. Non-range roles carry Navigation = None (they cannot
        // value-step; FR-008) — a consumer authoring a non-default-step slider supplies its own
        // NavRange through the typed metadata.
        let navRange =
            match role with
            | Slider -> Some { Step = 0.1; Min = 0.0; Max = 1.0 }
            | _ -> None

        metadata
            role
            label
            [ "normal" ]
            None
            (keyboardFor role focusable)
            (Some(contrast FS.GG.UI.Scene.Colors.black FS.GG.UI.Scene.Colors.white 7.0 4.5))
            navRange

    let validate control =
        match control.Accessibility with
        | None -> [ Diagnostics.missingAccessibility control.Key control.Kind ]
        | Some metadata ->
            // Feature 094 (E4), R1: relax the over-strict "focusable => non-empty NavigationKeys"
            // rule. Traversal (Tab) is engine-level, so an activation-only control (a Button) carries
            // NO NavigationKeys and is still valid. A focusable control is only flagged when it has
            // NEITHER an activation NOR a navigation affordance (genuinely no operable key set).
            [ if
                  metadata.Keyboard.Focusable
                  && metadata.Keyboard.ActivationKeys.IsEmpty
                  && metadata.Keyboard.NavigationKeys.IsEmpty
              then
                  yield FS.GG.UI.Controls.Diagnostics.create control.Key control.Kind MissingAccessibilityMetadata ControlDiagnosticSeverity.Error "Focusable control is missing keyboard navigation metadata."
              match metadata.Contrast with
              | Some evidence when evidence.Ratio < evidence.RequiredRatio ->
                  yield FS.GG.UI.Controls.Diagnostics.create control.Key control.Kind ContrastFailure ControlDiagnosticSeverity.Error "Contrast evidence is below the required ratio."
              | _ -> () ]
