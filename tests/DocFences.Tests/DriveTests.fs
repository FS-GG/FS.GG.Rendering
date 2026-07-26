module FS.GG.DocFences.DriveTests

open Expecto
open FS.GG.DocFences

/// THE FULL-CORPUS DRIVE (spec 255, T010) — ACTIVE over the explicitly self-contained corpus.
///
/// Rendering#1050 re-audited the live published-pin corpus: 84 fences across 16 documents, and no whole
/// document compiled in isolation. Per-document concatenation still cannot supply reader-owned values and
/// leaks bindings between examples; a shared product prelude would invent Model/Msg/host contracts and can
/// manufacture false greens. The supported model therefore keeps precise isolated modules and classifies
/// the entire versioned inventory as either SelfContained or Contextual-with-reason.
///
/// Corpus drift fails in `productSkillCompilationPlan` before the build, forcing a fresh disposition. Every
/// positive member compiles against the real published pins here; contextual snippets remain guarded by the
/// retained symbol oracle rather than padded with fictional product code. The release-window waiver remains
/// limited to a genuinely unpublished pin at the restore boundary.
[<Tests>]
let tests =
    testSequenced
    <| testList
        "DocFences.Drive (explicitly self-contained product-skill corpus)"
        [ test "every self-contained skill fence compiles against the published pin" {
              let plan = Harness.productSkillCompilationPlan ()

              let fences =
                  plan
                  |> List.choose (fun (fence, disposition) ->
                      match disposition with
                      | Harness.SelfContained -> Some fence
                      | Harness.Contextual reason ->
                          printfn "docfences: CONTEXTUAL %s:%d — %s" fence.Doc fence.StartLine reason
                          None)

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
