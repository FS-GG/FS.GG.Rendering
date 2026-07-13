module ControlsIssue663DocumentedSeamTests

// Issue #663 — the two seams the skill-parity gate found UNEXERCISED the moment a skill named them.
//
// #663 documented `diagnostics` and `validate` (S-DOC `tracked` lines, #507 / epic FS-GG/.github#423).
// Documenting them is what made the parity gate GRADE them, and it immediately reported both of these as
// `Unexercised` — *"Skill documents `X`, but no test calls it — the seam may be dead."* It was right:
// `ControlRuntime.diagnostics` had ZERO callers anywhere in `tests/` or `src/`, and `Catalog.validate` had
// none either — only its namesake `Catalog.validateStandardControl`, a DIFFERENT function, was tested.
//
// That is the #507 shape one level down, and it is worth naming: the parity gate only grades symbols a
// skill DOCUMENTS, so an undocumented seam is one it never looks at. These two were public, shipped, and
// unexamined — and the only thing that changed is that a skill finally cited them. Expect more of these as
// the epic's remaining `tracked` lines get written. That is the gate working, not the gate complaining.
//
// So these tests are the other half of documenting a surface. The repo's rule (Feature168:
// "repository parity has no unresolved findings") is that a skill may only document a symbol some test
// exercises, which is the right rule: a seam nothing calls is a seam nothing holds.
//
// They assert BEHAVIOUR, not merely the symbol's existence. `SkillParity` itself concedes that being
// "called by at least one test source ... says a test names the API in code — not that the test asserts
// anything meaningful about it", and a test written only to turn the gate green would be exactly that.

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let issue663DocumentedSeamTests =
    testList "Issue #663 — the seams the skills now document are exercised" [

        // `ControlRuntime.diagnostics` — the query fs-gg-elmish teaches as "what the focus/hover/press/drag
        // runtime has accumulated". Removing a control that is currently focused is the real way to make it
        // say something: the runtime clears the target and records a stale-target diagnostic.
        test "ControlRuntime.diagnostics reports the stale target left by removing a focused control" {
            let model, _ = ControlRuntime.init ()

            let focused, _ = ControlRuntime.update (FocusControl(Some "btn")) model
            Expect.isEmpty (ControlRuntime.diagnostics focused) "a clean runtime has accumulated nothing to report"

            let removed, effects = ControlRuntime.update (RemoveControl "btn") focused
            let reported = ControlRuntime.diagnostics removed

            Expect.isNonEmpty reported "removing a focused control leaves a diagnostic the runtime can hand back"

            Expect.exists
                reported
                (fun diagnostic -> diagnostic.Message.Contains "btn")
                "the diagnostic names the control that went stale — otherwise a product cannot act on it"

            // The query and the effect must agree. They are two views of one fact, and a product that
            // routes only the effect (or only the query) must not see a different world from one that
            // routes the other.
            Expect.exists
                effects
                (fun effect ->
                    match effect with
                    | ReportControlRuntimeDiagnostic diagnostic -> diagnostic.Message.Contains "btn"
                    | _ -> false)
                "the same diagnostic is raised as an effect, so the query and the effect stream agree"

            Expect.isNone removed.FocusedControl "...and the stale target really was cleared, not merely reported"
        }

        // `Catalog.validate` — the catalog's own self-check, which fs-gg-ui-widgets teaches beside
        // `Accessibility.validate` / `CustomControl.validate`. Nothing called it, so nothing was holding the
        // shipped catalog to its own schema. Asserting it is empty makes this a live regression guard on
        // catalog/schema drift — which is the job it exists to do, and the job nothing was doing.
        test "Catalog.validate finds no inconsistency in the shipped catalog" {
            let findings = Catalog.validate ()

            Expect.isEmpty
                findings
                "the shipped catalog is self-consistent: every definition agrees with the schema. A finding \
                 here is real drift between a control's definition and its declared schema — the thing this \
                 self-check exists to catch, and which nothing called it to catch until #663."
        }

        // The floor. Both assertions above are `isEmpty`-shaped in their happy path, and an `isEmpty` that
        // runs against an empty *input* passes by checking nothing (FS-GG/.github#266). So prove the catalog
        // is really there to be validated.
        test "the catalog self-check is not vacuous (it has controls to validate)" {
            Expect.isGreaterThan
                (Catalog.supportedCount ())
                0
                "Catalog.validate () above is checking a populated catalog, not an empty one — an empty \
                 catalog would validate clean while asserting nothing at all."
        }
    ]
