module Feature122CustomControlTests

// Feature 122 (US3, FR-006) — `CustomControl.validate`/`create` must surface a diagnostic, never a
// NullReferenceException, when reached with a null/blank Id or a null effect string (the
// reflection/preview path hits these before any explicit guard). The malformed (null) input is the
// error path under test, so the null-input cases are disclosed synthetic-error-handling (`[SEH]`,
// approved at task generation — see tasks.md Synthetic-Evidence Inventory T023).

open Expecto
open FS.GG.UI.Controls

// SYNTHETIC: a deliberately malformed definition (null Id, a null effect string) — the error path the
// guard protects.
let private nullIdDef: CustomControlDefinition =
    { Id = Unchecked.defaultof<ControlId> // genuinely null — the NRE the guard must survive
      Effects = [ Unchecked.defaultof<string>; "" ]
      Accessibility = None
      Diagnostics = [] }

[<Tests>]
let tests =
    testList
        "Feature 122 custom control NRE guard (US3, FR-006)"
        [ test "validate with a Synthetic null Id / null effect returns diagnostics, never throws (SC-005)" {
              let diags = CustomControl.validate nullIdDef
              Expect.isNonEmpty diags "a null id/effect surfaces missing-required diagnostics instead of an NRE"
          }

          test "create with a Synthetic null Id does not throw (SC-005)" {
              let control = CustomControl.create nullIdDef ([]: Attr<unit> list)
              Expect.isNotNull (box control) "create falls back to a safe id without dereferencing the null"
          }

          test "a well-formed custom control validates cleanly" {
              let ok =
                  { nullIdDef with
                      Id = "my-widget"
                      Effects = []
                      Accessibility = Some(Accessibility.defaultFor "custom-control" "my-widget") }

              Expect.isEmpty (CustomControl.validate ok) "a well-formed custom control has no diagnostics"
          } ]
