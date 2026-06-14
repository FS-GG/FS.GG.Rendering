module Feature113StabilityDiagTests

// Feature 113 (US4, FR-011/FR-012) — the public stability-diagnostic report `Diagnostics.stabilityReport`.
// Given TWO builds of the same logical (sub)tree, it returns one `UnstableReuseInput` finding per
// attribute/event that compared unequal despite no semantic change (a per-frame event closure, a rebuilt
// `UntypedValue`, an unstable key), naming the control + input. A stable tree (equal attributes, a
// reference-shared handler) reports nothing. Report-only, asserted here in tests (precedent: 101's
// `layoutDriftReport`).

open Expecto
open FS.Skia.UI.Controls

// Event handlers are first-class function values: F# re-wraps a function in a fresh closure adapter at
// each use site, so an event value is effectively always reference-fresh (and functions have no
// structural equality). The report therefore treats every per-frame event closure as a reuse-breaking
// instability — exactly the intended detection, and why the report is advisory rather than a gate. A
// genuinely STABLE fixture carries only stable VALUE attributes (no per-frame closures).

let private attr name cat value : Attr<int> = { Name = name; Category = cat; Value = value }

let private button (extra: Attr<int> list) : Control<int> =
    { Kind = "button"
      Key = Some "b"
      Attributes = attr "text" AttrCategory.Content (TextValue "Go") :: extra
      Children = []
      Content = None
      Accessibility = None }

let private treeWith (extra: Attr<int> list) : Control<int> =
    { Kind = "stack"
      Key = Some "root"
      Attributes = []
      Children = [ button extra ]
      Content = None
      Accessibility = None }

[<Tests>]
let tests =
    testList "Feature 113 stability diagnostic (US4, FR-011/FR-012)" [

        test "a tree built twice with stable value attributes reports NO findings (FR-012)" {
            let a = treeWith [ attr "value" AttrCategory.Data (TextValue "42") ]
            let b = treeWith [ attr "value" AttrCategory.Data (TextValue "42") ]
            Expect.isEmpty (Diagnostics.stabilityReport a b) "stable value attributes produce no instability findings"
        }

        test "an injected per-frame event closure is FLAGGED as a reuse-breaking instability (FR-011)" {
            // a FRESH lambda each build (the always-new input that defeats reference-stable reuse)
            let a = treeWith [ attr "onClick" AttrCategory.Event (EventValue(fun _ -> 0)) ]
            let b = treeWith [ attr "onClick" AttrCategory.Event (EventValue(fun _ -> 0)) ]
            let findings = Diagnostics.stabilityReport a b
            Expect.equal (List.length findings) 1 "exactly one instability finding"
            let f = List.head findings
            Expect.equal f.Code UnstableReuseInput "the finding uses the instability code"
            Expect.equal f.ControlId (Some "b") "the finding names the control (ControlId)"
            Expect.equal f.ControlKind "button" "the finding names the control kind"
            Expect.stringContains f.Message "onClick" "the finding names the offending event"
        }

        test "an injected always-new UntypedValue attribute is flagged (rebuilt value)" {
            let a = treeWith [ attr "payload" AttrCategory.Data (UntypedValue(System.Object())) ]
            let b = treeWith [ attr "payload" AttrCategory.Data (UntypedValue(System.Object())) ]
            let findings = Diagnostics.stabilityReport a b
            Expect.isTrue (findings |> List.exists (fun f -> f.Code = UnstableReuseInput)) "a rebuilt UntypedValue is an instability"
        }

        test "a structurally-equal rebuilt list does NOT flag (structural equality is stable)" {
            // each build reconstructs the list, but it is structurally equal -> stable under structural =
            let a = treeWith [ attr "items" AttrCategory.Data (StringListValue(List.init 3 (sprintf "i%d"))) ]
            let b = treeWith [ attr "items" AttrCategory.Data (StringListValue(List.init 3 (sprintf "i%d"))) ]
            Expect.isEmpty (Diagnostics.stabilityReport a b) "a structurally-equal rebuilt list is stable (no false positive)"
        }

        test "an unstable key on the same logical node is flagged" {
            let a = treeWith []
            let b = { (treeWith []) with Children = [ { (button []) with Key = Some "b-rebuilt" } ] }
            let findings = Diagnostics.stabilityReport a b
            Expect.isTrue (findings |> List.exists (fun f -> f.Code = UnstableReuseInput)) "a changed key across builds is an instability"
        }
    ]
