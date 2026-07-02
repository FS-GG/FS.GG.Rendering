namespace FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

type FocusStop =
    { Control: ControlId
      Role: AccessibilityRole
      Keyboard: KeyboardOperation
      FocusOrder: int option }

type TabOrder =
    { Stops: FocusStop list }

type FocusMove =
    | Next
    | Previous

[<RequireQualifiedAccess>]
type Direction =
    | Previous
    | Next
    | First
    | Last

type NavIntent =
    | ValueStep of delta: float
    | SelectionMove of Direction
    | GridMove of rowDelta: int * colDelta: int

type KeyRouting =
    | Activate
    | Navigate of NavIntent
    | Traverse of FocusMove
    | Fallthrough

type FocusRecoveryTargetKind =
    | Trigger
    | ParentSurface
    | Fallback
    | NoFocus

type FocusRecoveryDecision =
    { From: ControlId option
      To: ControlId option
      Reason: string
      RecoveryTargetKind: FocusRecoveryTargetKind
      Diagnostic: ControlDiagnostic option }

module Focus =

    // Feature 232 (#44): the id scheme is the SINGLE unified `Key ?? structural-path` (root "0",
    // child i -> parent + "." + i) — the same id `Control.eventBindingsOf`/`boundIdsOf`/`collectBoundsWith`
    // and `Focus.markFocused` use — replacing the old divergent `Key ?? Kind`. Keyed nodes are unchanged;
    // unkeyed ids shift `Kind -> path`, so unkeyed same-kind focusable siblings no longer collapse onto
    // one stop and a focused unkeyed control's id matches its bindings for keyboard dispatch.
    let private controlId (path: string) (c: Control<'msg>) : ControlId =
        c.Key |> Option.defaultValue path

    // FR-001: pre-order walk that emits a FocusStop for each focusable control and does NOT descend
    // into a focusable control's subtree (a composite is a single tab stop, clarified). A
    // non-focusable container is descended so its focusable descendants are found. `docIndex` is the
    // pre-order visit index, threaded so the sort tiebreak is stable document order. `path` is the
    // positional structural path (feature 232); it advances by child index even across a focusable
    // subtree the walk does not descend, so it matches the path every other seam computes.
    let order (control: Control<'msg>) : TabOrder =
        let stops = System.Collections.Generic.List<int * FocusStop>()
        let mutable docIndex = 0

        let rec walk (path: string) (c: Control<'msg>) =
            let here = docIndex
            docIndex <- docIndex + 1

            match c.Accessibility with
            | Some metadata when metadata.Keyboard.Focusable ->
                stops.Add(
                    here,
                    { Control = controlId path c
                      Role = metadata.Role
                      Keyboard = metadata.Keyboard
                      FocusOrder = metadata.FocusOrder }
                )
            // Focusable -> single stop; do not descend into its subtree.
            | _ ->
                // Non-focusable (or no metadata) -> descend to find focusable descendants, threading
                // the child index into `path` (root "0", child i -> path + "." + i).
                c.Children
                |> List.iteri (fun index child -> walk (path + "." + string index) child)

        walk "0" control

        // Stable sort by (FocusOrder ?? +inf, docIndex). List.sortBy is stable, and the docIndex
        // component makes the order fully deterministic even within an equal FocusOrder bucket.
        let ordered =
            stops
            |> List.ofSeq
            |> List.sortBy (fun (docIndex, stop) ->
                (match stop.FocusOrder with
                 | Some n -> n
                 | None -> System.Int32.MaxValue),
                docIndex)
            |> List.map snd

        { Stops = ordered }

    // FR-002: cyclic traversal reduction. Total/deterministic over the closed FocusMove set.
    let traverse (order: TabOrder) (current: ControlId option) (move: FocusMove) : ControlId option =
        let stops = order.Stops
        let n = List.length stops

        if n = 0 then
            None
        else
            let idOf (s: FocusStop) = s.Control
            let first () = Some(idOf stops.[0])
            let last () = Some(idOf stops.[n - 1])

            match current with
            | None ->
                match move with
                | Next -> first ()
                | Previous -> last ()
            | Some id ->
                match stops |> List.tryFindIndex (fun s -> idOf s = id) with
                | Some i ->
                    let j =
                        match move with
                        | Next -> (i + 1) % n
                        | Previous -> (i - 1 + n) % n

                    Some(idOf stops.[j])
                // Stale target: the current id is absent from the order (it was removed between
                // frames). Recover to the first stop on Next / last stop on Previous (the next stop
                // at the former start position) — never throws.
                | None ->
                    match move with
                    | Next -> first ()
                    | Previous -> last ()

    // FR-001/FR-006 (R5): the SINGLE role-specific branch. Classify a navigation key (already
    // confirmed to be in the role's `NavigationKeys`) into a closed `NavIntent` by role. A value
    // role needs a declared `NavRange` to form a `ValueStep` (the delta is the declared step x key
    // sign; Home/End fold to a delta that clamps to Min/Max at the host); without one it cannot
    // step -> None (Fallthrough, FR-008). A role with no navigation semantics -> None. Pure, total.
    let private navIntentFor (role: AccessibilityRole) (navRange: NavRange option) (key: string) : NavIntent option =
        match role with
        // Value / range roles: arrows step by the declared step; Home/End jump to Min/Max.
        // Feature 102 (R8): of these, only `Slider` routes by default. `Accessibility.defaultFor` gives
        // `Progress`/`Chart`/`Graph` no `NavRange` (and `Progress` is non-focusable), so for them `navRange`
        // is `None` and this arm falls through to `None` — they are classed-but-not-routed by default. They
        // share the arm so a consumer that supplies a `NavRange` opts them into the same value-step
        // semantics; enabling default routing for them is out of scope (it would be a behavior change).
        | Slider
        | Progress
        | Chart
        | Graph ->
            match navRange with
            | Some range ->
                match key with
                | "ArrowRight"
                | "ArrowUp" -> Some(ValueStep range.Step)
                | "ArrowLeft"
                | "ArrowDown" -> Some(ValueStep(-range.Step))
                // A delta guaranteed to clamp to the bound from any current value in [Min, Max].
                | "Home" -> Some(ValueStep(range.Min - range.Max))
                | "End" -> Some(ValueStep(range.Max - range.Min))
                | _ -> None
            | None -> None
        // Linear selection roles: prev/next/first/last over the existing selection model.
        | RadioGroup
        | Tab
        | Menu
        | List ->
            match key with
            | "ArrowUp"
            | "ArrowLeft" -> Some(SelectionMove Direction.Previous)
            | "ArrowDown"
            | "ArrowRight" -> Some(SelectionMove Direction.Next)
            | "Home" -> Some(SelectionMove Direction.First)
            | "End" -> Some(SelectionMove Direction.Last)
            | _ -> None
        // Grid roles: a 2-D unit delta (row by Up/Down, column by Left/Right).
        | Grid ->
            match key with
            | "ArrowUp" -> Some(GridMove(-1, 0))
            | "ArrowDown" -> Some(GridMove(1, 0))
            | "ArrowLeft" -> Some(GridMove(0, -1))
            | "ArrowRight" -> Some(GridMove(0, 1))
            | _ -> None
        // Non-navigable roles (Button, CheckBox, TextBox, Dialog, StaticText, Image, Custom):
        // no intra-control navigation intent.
        | _ -> None

    // FR-003/FR-007: classify a normalized key against the focused control's role + KeyboardOperation.
    // The control's own consumption (Activate/Navigate) is tested BEFORE the Tab test, so a control
    // that lists a traversal key in its own keys consumes it (never Traverse). A navigation key the
    // role/range cannot classify -> Fallthrough (FR-008 no-op). Pure, total.
    let route
        (role: AccessibilityRole)
        (keyboard: KeyboardOperation)
        (navRange: NavRange option)
        (key: string)
        (isTab: bool)
        (shift: bool)
        : KeyRouting =
        if List.contains key keyboard.ActivationKeys then
            Activate
        elif List.contains key keyboard.NavigationKeys then
            match navIntentFor role navRange key with
            | Some intent -> Navigate intent
            | None -> Fallthrough
        elif isTab then
            Traverse(if shift then Previous else Next)
        else
            Fallthrough

    let private classifyRecovery (prior: OverlayState) (target: ControlId option) =
        match target with
        | None -> NoFocus
        | Some id ->
            let priorSurface =
                prior.OpenSurfaces
                |> List.tryFind (fun surface ->
                    surface.Trigger.ControlId = id
                    || surface.Trigger.RecoveryTarget = Some id
                    || surface.FocusScope.RecoveryTarget = Some id)

            match priorSurface with
            | Some _ -> Trigger
            | None ->
                let parent =
                    prior.OpenSurfaces
                    |> List.exists (fun surface ->
                        surface.Id.ParentSurfaceId = Some id
                        || surface.FocusScope.InitialFocus = Some id)

                if parent then ParentSurface else Fallback

    let recoverOverlayFocus (overlay: OverlayState) (removedTarget: ControlId) =
        let fromFocus = overlay.FocusedControl
        let next, effects = OverlayState.update (FocusTargetRemoved removedTarget) overlay

        let focus =
            effects
            |> List.tryPick (function
                | RequestFocus value -> Some value
                | _ -> None)
            |> Option.defaultValue next.FocusedControl

        let diagnostic =
            effects
            |> List.tryPick (function
                | ReportOverlayDiagnostic diagnostic -> Some diagnostic
                | _ -> None)

        let decision =
            { From = fromFocus
              To = focus
              Reason = "focus-target-removed"
              RecoveryTargetKind = classifyRecovery overlay focus
              Diagnostic = diagnostic }

        next, effects, decision

    // FR-004: the same focusability predicate `order` uses — a control is focusable iff its
    // AccessibilityMetadata declares `Keyboard.Focusable`.
    let private isFocusable (c: Control<'msg>) : bool =
        match c.Accessibility with
        | Some metadata -> metadata.Keyboard.Focusable
        | None -> false

    // FR-001/SC-012: stamp `Focused` only when the control is at `Normal` (no consumer-set state to
    // preserve). Appending the `visualState` attribute makes `Focused` the last-writer the renderer
    // reads; a control already at a non-`Normal` state is returned verbatim so `Disabled` wins.
    let private stampFocused (c: Control<'msg>) : Control<'msg> =
        if ControlInternals.visualStateOf c.Attributes = Normal then
            { c with Attributes = c.Attributes @ [ Attr.visualState Focused ] }
        else
            c

    // FR-001..005: walk the tree minting the unified `Key ?? structural path` id (root "0", child
    // `path + "." + index`, descending ALL children exactly as `collectBoundsWith`/dispatch do, so
    // the stamped id agrees with the id a consumer's focus model holds), stamping `Focused` on the
    // one focusable control whose id equals `focused`. `None` short-circuits to the input tree
    // (byte-identical, no allocation of a rewritten tree).
    let markFocused (focused: ControlId option) (control: Control<'msg>) : Control<'msg> =
        match focused with
        | None -> control
        | Some target ->
            let rec go (path: string) (c: Control<'msg>) : Control<'msg> =
                let id = c.Key |> Option.defaultValue path

                let c =
                    { c with
                        Children = c.Children |> List.mapi (fun index child -> go (path + "." + string index) child) }

                if id = target && isFocusable c then stampFocused c else c

            go "0" control
