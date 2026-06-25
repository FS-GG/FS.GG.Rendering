namespace FS.GG.UI.Controls

open System.Collections.Generic
open FS.GG.UI.DesignSystem

module internal Reconcile =

    type FieldChange<'a> =
        | Unchanged
        | ChangedTo of 'a

    type AttrChange<'msg> =
        | AttrSet of Attr<'msg>
        | AttrRemoved of name: string

    [<RequireQualifiedAccess>]
    type NodePatch<'msg> =
        | Keep
        | Replace of Control<'msg>
        | Update of UpdatePatch<'msg>

    and UpdatePatch<'msg> =
        { AttrChanges: AttrChange<'msg> list
          ContentChange: FieldChange<string option>
          AccessibilityChange: FieldChange<AccessibilityMetadata option>
          Children: ChildOp<'msg> list }

    and ChildOp<'msg> =
        | ChildKeep of index: int * patch: NodePatch<'msg>
        | ChildMove of fromIndex: int * toIndex: int * patch: NodePatch<'msg>
        | ChildInsert of index: int * node: Control<'msg>
        | ChildRemove of key: ControlId option * index: int

    type ReconcileResult<'msg> =
        { Patch: NodePatch<'msg>
          Diagnostics: ControlDiagnostic list }

    // `AttrValue<'msg>` carries a function case (`EventValue`) and an opaque
    // `obj` case (`UntypedValue`), so it does not satisfy F#'s `equality`
    // constraint — `prev.Value = next.Value` would not compile. This total
    // comparator compares the structural cases by value and the opaque/function/
    // `'msg`-bearing cases by reference (or boxed `Object.Equals`). It is
    // deliberately conservative: if it reports "changed" for two structurally
    // equal opaque values, the diff merely emits a redundant `AttrSet` carrying
    // the next value, which `apply` writes verbatim — the round-trip invariant
    // (FR-008) still holds. It never throws (SC-007).
    let attrValueEqual (x: AttrValue<'msg>) (y: AttrValue<'msg>) : bool =
        match x, y with
        | TextValue a, TextValue b -> a = b
        | BoolValue a, BoolValue b -> a = b
        | FloatValue a, FloatValue b -> a = b
        | StringListValue a, StringListValue b -> a = b
        | ValidationValue a, ValidationValue b -> a = b
        | AccessibilityValue a, AccessibilityValue b -> a = b
        | ThemeValue a, ThemeValue b -> a = b
        // Feature 099 (R4): a held VisualState must compare equal so a steadily hovered/focused
        // control does NOT re-diff (and thus re-paint) every frame — otherwise R1's per-frame stamp
        // would defeat the at-rest fast path and the scoped-repaint guarantee (SC-006/FR-010).
        // VisualState is an equatable DU, so this is a value comparison, not a reference one.
        | VisualStateValue a, VisualStateValue b -> a = b
        // Feature 191 (US1/US2): an immutable canvas Scene compares structurally, so an unchanged scene
        // does NOT re-diff (and re-paint) every frame — the at-rest reuse fast path holds for a static
        // canvas, and `volatile'` (not this equality) is what forces a per-frame repaint.
        | SceneValue a, SceneValue b -> a = b
        | ChildValue a, ChildValue b -> obj.ReferenceEquals(a, b)
        | ChildrenValue a, ChildrenValue b -> obj.ReferenceEquals(a, b)
        | MessageValue a, MessageValue b -> obj.Equals(box a, box b)
        | EventValue a, EventValue b -> obj.ReferenceEquals(a, b)
        | UntypedValue a, UntypedValue b -> obj.Equals(a, b)
        | _ -> false

    /// Attribute diff by `Name` (FR-007), independent of list order; the emitted
    /// list is sorted by `Name` for deterministic output (FR-009).
    let diffAttrs (prevAttrs: Attr<'msg> list) (nextAttrs: Attr<'msg> list) : AttrChange<'msg> list =
        let prevMap = Dictionary<string, Attr<'msg>>()
        for a in prevAttrs do
            prevMap.[a.Name] <- a

        let nextMap = Dictionary<string, Attr<'msg>>()
        for a in nextAttrs do
            nextMap.[a.Name] <- a

        let names =
            (prevAttrs |> List.map (fun a -> a.Name)) @ (nextAttrs |> List.map (fun a -> a.Name))
            |> List.distinct
            |> List.sort

        [ for name in names do
            match prevMap.TryGetValue name, nextMap.TryGetValue name with
            | (true, pa), (true, na) -> if not (attrValueEqual pa.Value na.Value) then yield AttrSet na
            | (false, _), (true, na) -> yield AttrSet na
            | (true, _), (false, _) -> yield AttrRemoved name
            | (false, _), (false, _) -> () ]

    let isKeepOp (op: ChildOp<'msg>) : bool =
        match op with
        | ChildKeep (_, NodePatch.Keep) -> true
        | _ -> false

    let diff (prev: Control<'msg>) (next: Control<'msg>) : ReconcileResult<'msg> =
        // Local mutable diagnostics accumulator (Principle III: a `ResizeArray`
        // reads plainer than threading a list through the recursion). The function
        // remains pure from the outside — `diags` is a fresh local per call.
        let diags = ResizeArray<ControlDiagnostic>()

        let recordCollision (parentKind: ControlKind) (key: ControlId) =
            diags.Add
                { ControlId = Some key
                  ControlKind = parentKind
                  Code = KeyCollision
                  Severity = Warning
                  Message =
                    sprintf "Duplicate key '%s' within the children of a '%s' node; first occurrence wins." key parentKind
                  EvidencePath = None }

        let rec nodePatch (p: Control<'msg>) (n: Control<'msg>) : NodePatch<'msg> =
            if p.Kind <> n.Kind || p.Key <> n.Key then
                // FR-006: a matched pair with a differing Kind is a whole-subtree
                // replace. A differing Key is the same case — identity is keyed, so a
                // key change is a different node, not an in-place update (and an
                // `Update` patch has no channel to carry a new Key). Child matches
                // always share their key, so this only ever fires at the root.
                NodePatch.Replace n
            else
                let attrChanges = diffAttrs p.Attributes n.Attributes
                let contentChange = if p.Content = n.Content then Unchanged else ChangedTo n.Content

                let accessibilityChange =
                    if p.Accessibility = n.Accessibility then
                        Unchanged
                    else
                        ChangedTo n.Accessibility

                let childOps = diffChildren n.Kind p.Children n.Children

                let isNoop =
                    List.isEmpty attrChanges
                    && contentChange = Unchanged
                    && accessibilityChange = Unchanged
                    && List.forall isKeepOp childOps

                if isNoop then
                    NodePatch.Keep
                else
                    NodePatch.Update
                        { AttrChanges = attrChanges
                          ContentChange = contentChange
                          AccessibilityChange = accessibilityChange
                          Children = childOps }

        and diffChildren (parentKind: ControlKind) (prevC: Control<'msg> list) (nextC: Control<'msg> list) : ChildOp<'msg> list =
            let prevArr = List.toArray prevC
            let nextArr = List.toArray nextC

            // (1) prev key buckets, first-occurrence wins; later dups are collisions.
            let prevKeyToIndex = Dictionary<ControlId, int>()

            for i in 0 .. prevArr.Length - 1 do
                match prevArr.[i].Key with
                | Some k ->
                    if prevKeyToIndex.ContainsKey k then
                        recordCollision parentKind k
                    else
                        prevKeyToIndex.[k] <- i
                | None -> ()

            // residual unkeyed prev positions, in order, for positional fallback.
            let prevUnkeyed =
                [ for i in 0 .. prevArr.Length - 1 do
                    if prevArr.[i].Key.IsNone then
                        yield i ]

            let claimed = HashSet<int>()
            let nextSeenKeys = HashSet<ControlId>()
            let mutable unkeyedCursor = 0

            // (2)-(3) for each next child resolve its matched prev index: key first,
            // then positional among unkeyed residuals.
            let perNext =
                [ for t in 0 .. nextArr.Length - 1 ->
                    match nextArr.[t].Key with
                    | Some k ->
                        if nextSeenKeys.Contains k then
                            recordCollision parentKind k
                            (t, None)
                        else
                            nextSeenKeys.Add k |> ignore

                            match prevKeyToIndex.TryGetValue k with
                            | true, pi when not (claimed.Contains pi) ->
                                claimed.Add pi |> ignore
                                (t, Some pi)
                            | _ -> (t, None)
                    | None ->
                        if unkeyedCursor < prevUnkeyed.Length then
                            let pi = prevUnkeyed.[unkeyedCursor]
                            unkeyedCursor <- unkeyedCursor + 1
                            claimed.Add pi |> ignore
                            (t, Some pi)
                        else
                            unkeyedCursor <- unkeyedCursor + 1
                            (t, None) ]

            // (5) producing ops in next order; a forward scan keeps the first
            // in-order match and flags out-of-order matches as moves (a simple,
            // deterministic scheme — not LIS minimization, which is deferred).
            let mutable lastPrev = -1

            let producing =
                perNext
                |> List.map (fun (t, matchedPrev) ->
                    match matchedPrev with
                    | Some pi ->
                        let childPatch = nodePatch prevArr.[pi] nextArr.[t]

                        if pi >= lastPrev then
                            lastPrev <- pi
                            ChildKeep(pi, childPatch)
                        else
                            ChildMove(pi, t, childPatch)
                    | None -> ChildInsert(t, nextArr.[t]))

            // (4) prev-only nodes (never matched) become removes, in prev order.
            let removals =
                [ for i in 0 .. prevArr.Length - 1 do
                    if not (claimed.Contains i) then
                        yield ChildRemove(prevArr.[i].Key, i) ]

            producing @ removals

        { Patch = nodePatch prev next
          Diagnostics = List.ofSeq diags }

    let private applyAttrChanges (prevAttrs: Attr<'msg> list) (changes: AttrChange<'msg> list) : Attr<'msg> list =
        // Removed names, then set/added names (replace existing by Name, append the rest).
        let removed =
            changes
            |> List.choose (function
                | AttrRemoved n -> Some n
                | AttrSet _ -> None)
            |> Set.ofList

        let sets =
            changes
            |> List.choose (function
                | AttrSet a -> Some a
                | AttrRemoved _ -> None)

        let setByName = Dictionary<string, Attr<'msg>>()
        for a in sets do
            setByName.[a.Name] <- a

        let kept =
            prevAttrs
            |> List.filter (fun a -> not (removed.Contains a.Name))
            |> List.map (fun a ->
                match setByName.TryGetValue a.Name with
                | true, replacement -> replacement
                | _ -> a)

        let existingNames = kept |> List.map (fun a -> a.Name) |> Set.ofList
        let added = sets |> List.filter (fun a -> not (existingNames.Contains a.Name))
        kept @ added

    let rec apply (prev: Control<'msg>) (patch: NodePatch<'msg>) : Control<'msg> =
        match patch with
        | NodePatch.Keep -> prev
        | NodePatch.Replace n -> n
        | NodePatch.Update u ->
            let prevChildren = List.toArray prev.Children

            // Producing ops (every op except ChildRemove) appear in next order;
            // fold them in list order to reconstruct the next children list.
            let children =
                u.Children
                |> List.choose (fun op ->
                    match op with
                    | ChildKeep (i, p) -> Some(apply prevChildren.[i] p)
                    | ChildMove (f, _, p) -> Some(apply prevChildren.[f] p)
                    | ChildInsert (_, node) -> Some node
                    | ChildRemove _ -> None)

            let content =
                match u.ContentChange with
                | Unchanged -> prev.Content
                | ChangedTo v -> v

            let accessibility =
                match u.AccessibilityChange with
                | Unchanged -> prev.Accessibility
                | ChangedTo v -> v

            { prev with
                Attributes = applyAttrChanges prev.Attributes u.AttrChanges
                Children = children
                Content = content
                Accessibility = accessibility }
