namespace FS.GG.UI.Controls

module Diagnostics =
    let create controlId kind code severity message =
        { ControlId = controlId
          ControlKind = kind
          Code = code
          Severity = severity
          Message = message
          EvidencePath = None }

    let missingRequired controlId kind (name: string) =
        create controlId kind MissingRequiredAttribute Error $"Missing required attribute `{name}`."

    let duplicateAttribute controlId kind (name: string) =
        create controlId kind DuplicateAttribute Warning $"Duplicate attribute `{name}` uses last-value-wins precedence."

    let missingAccessibility controlId kind =
        create controlId kind MissingAccessibilityMetadata Error "Supported interactive control is missing accessibility metadata."

    let keyCollision key kind =
        create (Some key) kind KeyCollision Error $"Duplicate stable key `{key}` in the control tree."

    let unsupportedEnvironment kind (capability: string) =
        create None kind UnsupportedEnvironment Warning $"Host environment does not expose {capability}; operation reports diagnostics instead."

    let unsupportedStandardAttribute (kind: StandardControlKind) (name: StandardAttributeName) =
        create None $"{kind}" UnsupportedStateCombination Error $"Unsupported standard attribute `{name}` for `{kind}`."

    let unsupportedStandardEvent (kind: StandardControlKind) (eventKind: StandardEventKind) =
        create None $"{kind}" UnsupportedStateCombination Error $"Unsupported standard event `{eventKind}` for `{kind}`."

    let missingStandardAttribute (kind: StandardControlKind) (name: StandardAttributeName) =
        create None $"{kind}" MissingRequiredAttribute Error $"Missing required standard attribute `{name}` for `{kind}`."

    let customExtension (kind: string) (extensionName: string) =
        create None kind UnsupportedEnvironment Info $"Custom extension `{extensionName}` is accepted for custom control `{kind}`."

    let stalePackageReference (packageId: string) (path: string) =
        create None packageId StaleGeneratedReference Error $"Stale package reference `{packageId}` found in `{path}`."

    let dependencyLeak (packageId: string) (dependencyPath: string) =
        create None packageId StaleGeneratedReference Error $"Package `{packageId}` leaks dependency `{dependencyPath}` across the declared boundary."

    let catalogOmission (controlId: string) (requiredField: string) =
        create (Some controlId) "catalog" MissingRequiredAttribute Error $"Catalog entry `{controlId}` is missing required field `{requiredField}`."

    let duplicateRuntimeDefinition (runtimeName: string) (path: string) =
        create None runtimeName DuplicateAttribute Error $"Duplicate runtime definition `{runtimeName}` found in `{path}`."

    let staleEventTarget (controlId: ControlId) (eventKind: string) =
        create (Some controlId) "control-runtime" StaleGeneratedReference Warning $"Stale event target `{controlId}` for event `{eventKind}` was ignored."

    let unsupportedScopeExpansion (scopeName: string) (owner: string) =
        create None scopeName UnsupportedEnvironment Error $"Unsupported scope expansion `{scopeName}` requested by `{owner}`."

    // Feature 113 (Phase 5): is an attribute/event value STABLE across two builds of the same model?
    // Stability is exactly the reuse condition: a structurally-equal scalar value is stable; a
    // reference-fresh closure (a per-frame lambda) or a rebuilt `UntypedValue` that is not structurally
    // equal is UNSTABLE. Never compares a function with `=` (that would throw) — closures use reference
    // identity, so a shared handler is stable and a fresh lambda each build is not. Sub-tree/slot values
    // are covered by the parallel child walk, so they are treated as stable here (no false flag).
    let private attrValueStable (a: AttrValue<'msg>) (b: AttrValue<'msg>) : bool =
        match a, b with
        | TextValue x, TextValue y -> x = y
        | BoolValue x, BoolValue y -> x = y
        | FloatValue x, FloatValue y -> x = y
        | StringListValue x, StringListValue y -> x = y
        | ValidationValue x, ValidationValue y -> x = y
        | StyleClassesValue x, StyleClassesValue y -> x = y
        | VisualStateValue x, VisualStateValue y -> x = y
        | AccessibilityValue x, AccessibilityValue y -> x = y
        | ThemeValue x, ThemeValue y -> x = y
        | UntypedValue x, UntypedValue y -> System.Object.Equals(x, y)
        | EventValue x, EventValue y -> System.Object.ReferenceEquals(x, y)
        | MessageValue x, MessageValue y -> System.Object.Equals(box x, box y)
        | ChildValue _, ChildValue _ -> true
        | ChildrenValue _, ChildrenValue _ -> true
        | SlotFillsValue _, SlotFillsValue _ -> true
        // A changed value SHAPE (different constructor) across two builds of the same model is itself
        // an instability.
        | _ -> false

    let stabilityReport (first: Control<'msg>) (second: Control<'msg>) : ControlDiagnostic list =
        let findings = System.Collections.Generic.List<ControlDiagnostic>()

        let rec walk (path: string) (a: Control<'msg>) (b: Control<'msg>) =
            let id = a.Key |> Option.defaultValue path

            // Unstable key: the same logical node carries a different `Key` across builds — itself a
            // reuse-breaking instability (re-keys the retained identity).
            if a.Key <> b.Key then
                findings.Add(create (Some id) a.Kind UnstableReuseInput Info $"Unstable key on `{a.Kind}` node `{id}`: the key changed across two builds of the same model, defeating memoized reuse.")

            // Attributes/events matched by name (last-writer-wins, mirroring lowering); for a name present
            // in BOTH builds, an unstable value is an always-new input that defeats reuse.
            let bByName =
                b.Attributes
                |> List.fold (fun (m: Map<string, Attr<'msg>>) at -> Map.add at.Name at m) Map.empty

            for at in a.Attributes do
                match Map.tryFind at.Name bByName with
                | Some bt when not (attrValueStable at.Value bt.Value) ->
                    let inputKind = if at.Category = Event then "event" else "attribute"
                    findings.Add(create (Some id) a.Kind UnstableReuseInput Info $"Always-new {inputKind} `{at.Name}` on `{a.Kind}` node `{id}` compared unequal across two builds of the same model; it defeats memoized reuse.")
                | _ -> ()

            // Parallel child walk over the shared prefix (the same logical tree ⇒ aligned children).
            let n = min (List.length a.Children) (List.length b.Children)

            List.zip (List.truncate n a.Children) (List.truncate n b.Children)
            |> List.iteri (fun i (ca, cb) -> walk (path + "." + string i) ca cb)

        walk "0" first second
        List.ofSeq findings
