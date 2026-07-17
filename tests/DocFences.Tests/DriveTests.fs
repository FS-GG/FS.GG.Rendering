module FS.GG.DocFences.DriveTests

open Expecto
open FS.GG.DocFences

/// THE FULL-CORPUS DRIVE (spec 255, T010) — PENDING a design decision, deliberately not a gate yet.
///
/// The harness mechanism is proven (see HarnessProofTests: green + red, live). But driving the WHOLE skill
/// corpus (measured during spec-255 exploration) revealed that the naive "one fence = one isolated
/// compilation unit" does NOT hold for the pedagogical skill docs: even after excluding the ~4 parse-error
/// fences, 7 of ~13 skill docs still fail — with `FS0039 'X is not defined'` on:
///   * reader-owned identifiers a doc never defines (`Model`, `Msg`, `update`, `view`, `AppRoot`, `host`, ...);
///   * symbols defined in an EARLIER fence of the same doc (cross-fence continuity);
///   * modules needing a qualified open the preamble does not carry (`ControlsElmish`, `Audio`, `Expect`).
///
/// None of these are pin defects — they are the shape of teaching snippets. Making the corpus compile needs
/// a design decision that reopens research.md D2 (per-doc concatenation / a shared product-type prelude /
/// compile only self-contained corpora / keep the oracle for skills). Until that decision lands, this drive
/// is marked PENDING (`ptestList`) — disclosed, NOT vacuously green — and the harness's proven green+red
/// gate (HarnessProofTests) is what actually guards.
///
/// The body below is kept and current so that, once the design decision is made, this becomes the gate by
/// changing `ptestList` to `testList`.
[<Tests>]
let tests =
    testSequenced
    <| ptestList
        "DocFences.Drive (PENDING — corpus not yet all-compilable, design decision open)"
        [ test "every non-skipped skill fence compiles against the published pin" {
              let fences = Corpus.productSkillFences ()
              let units, skipped = Harness.unitsFor fences

              for f, reason in skipped do
                  printfn "docfences: SKIP %s:%d — %s" f.Doc f.StartLine reason

              let outcome = Harness.compile Pins.pinnedPackages units

              if Harness.pinUnpublished outcome then
                  skiptestf
                      "pin %s not yet published to nuget.org (release window) — waiver applies"
                      Pins.uiVersion.Value

              let report =
                  outcome.Diagnostics
                  |> List.map (fun d -> sprintf "  %s:%d  %s" d.Doc d.Line d.Message)
                  |> String.concat "\n"

              Expect.isTrue
                  outcome.Succeeded
                  (sprintf "shipped skill fences failed to compile against the pin:\n%s" report)
          } ]
