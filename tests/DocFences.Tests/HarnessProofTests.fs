module FS.GG.DocFences.HarnessProofTests

open Expecto
open FS.GG.DocFences
open FS.GG.DocFences.Corpus

/// THE EARLY LIVE PROOF (spec 255, T005) — the mechanism, end to end, GREEN and RED, before any old
/// machinery is deleted. These tests do a REAL nuget.org restore of the published pin and a REAL
/// `dotnet build`, so they are network-bound and slower than the deterministic corpus tests. They are the
/// gate the plan requires: "the compiler catches what the regex caught" stays an UNVERIFIED assumption
/// until this passes.
///
/// The pin is `FS.GG.UI.Scene 0.12.0`; the green case binds the real record `FS.GG.UI.Scene.Point`, the red
/// case names a member the package does not export. If the pin is not yet published (release window), the
/// restore fails for a DIFFERENT reason (NU1101/NU1102) and the test is skipped rather than red — the
/// `PinPending` waiver at the harness boundary (FR-012).

let private pin = Pins.uiVersion.Value
let private scenePackage = [ "FS.GG.UI.Scene", pin ]

/// A fake origin so a diagnostic has a doc+line to map back to.
let private origin doc line body : FenceBlock =
    { Kind = ProductSkill; Doc = doc; StartLine = line; Body = body; Skip = None; ExtraOpens = [] }

[<Tests>]
let tests =
    testSequenced
    <| testList
        "DocFences.Harness (live proof)"
        [ test "GREEN: a fence that binds a real pinned symbol compiles" {
              let body = [ "let p : Point = { X = 1.0; Y = 2.0 }"; "ignore p" ]

              let unit =
                  { Harness.ModuleName = "Fence_green"
                    Harness.Origin = origin "template/product-skills/fixture/SKILL.md" 10 body
                    Harness.Opens = [ "FS.GG.UI.Scene" ]
                    Harness.Body = body }

              let outcome = Harness.compile scenePackage [ unit ]

              if Harness.pinUnpublished outcome then
                  skiptestf "pin %s not yet published to nuget.org (release window) — waiver applies" pin

              Expect.isTrue
                  outcome.Succeeded
                  (sprintf "a fence using the real Point record must compile against the pin.\n%s" outcome.RawOutput)
          }

          test "RED: a fence naming an unreleased symbol fails, mapped to its doc+line" {
              let body = [ "Point.thisMemberDoesNotExistInThePin () |> ignore" ]

              let unit =
                  { Harness.ModuleName = "Fence_red"
                    Harness.Origin = origin "template/product-skills/fixture/SKILL.md" 42 body
                    Harness.Opens = [ "FS.GG.UI.Scene" ]
                    Harness.Body = body }

              let outcome = Harness.compile scenePackage [ unit ]

              if Harness.pinUnpublished outcome then
                  skiptestf "pin %s not yet published to nuget.org (release window) — waiver applies" pin

              Expect.isFalse
                  outcome.Succeeded
                  "a fence naming a symbol the pin does not export MUST fail to compile"

              let mapped =
                  outcome.Diagnostics
                  |> List.exists (fun d ->
                      d.Doc = "template/product-skills/fixture/SKILL.md" && d.Line = 42)

              Expect.isTrue
                  mapped
                  (sprintf "the failure must map back to the fixture doc at line 42; got %A" outcome.Diagnostics)
          } ]
