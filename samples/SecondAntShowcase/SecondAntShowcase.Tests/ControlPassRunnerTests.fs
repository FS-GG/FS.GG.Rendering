module SecondAntShowcase.Tests.ControlPassRunnerTests

open Expecto
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model
open SecondAntShowcase.Core.ControlPass
open SecondAntShowcase.App

/// T007 — shared fixture: build the full pass once (headless, deterministic) and reuse it across
/// the US1/US2 assertions. The environment is probed honestly; on a host with no renderable
/// surface the live-pixel cells degrade to environment-limited (FR-008) while the functional and
/// structural-damage dimensions stay authoritative.
module ControlPassFixtures =
    let full =
        lazy (ControlPassRunner.evaluate (ControlPassRunner.testConfig None))

    let firstCatalogPage = (List.head PageRegistry.catalogPages).Id

[<Tests>]
let runnerTests =
    let env, records, findings = ControlPassFixtures.full.Value

    testList
        "ControlPass runner"
        [
          // --- US1: classification completeness (T009) ------------------------------------
          test "every emitted record is terminal — no Unexercised/Unclassified (C-2/C-3, VR-2)" {
              for r in records do
                  match r.Classification with
                  | DisplayOnly ->
                      Expect.isFalse (System.String.IsNullOrWhiteSpace r.ClassificationReason) (sprintf "display-only '%s' carries a reason" r.ControlId)
                      Expect.equal r.FunctionalVerdict NotApplicable (sprintf "display-only '%s' is NotApplicable (VR-5)" r.ControlId)
                  | Interactive ->
                      let allowed = [ FunctionalPass; FunctionalFail; FunctionalNeedsReview; FunctionalEnvironmentLimited ]
                      Expect.contains allowed r.FunctionalVerdict (sprintf "interactive '%s' has a functional verdict in the allowed set" r.ControlId)
                      Expect.notEqual r.FunctionalVerdict NotApplicable (sprintf "interactive '%s' is not NotApplicable (VR-5)" r.ControlId)
          }

          test "completeness — one record per cataloged control, no missing/duplicate (G-2/VR-1)" {
              let missing, duplicate = ControlPass.completenessGaps records
              Expect.isEmpty missing "no missing catalog ids"
              Expect.isEmpty duplicate "no duplicate/foreign ids"
          }

          // --- US1: determinism (T010, G-4/SC-005) -----------------------------------------
          // G-4 guarantees byte-stability on the framework-deterministic surface (functional +
          // structural-damage). The live-pixel GL capture is the environment-dependent surface
          // (like time/animation) and is explicitly excluded — its CaptureStatus may vary with
          // window/GL availability, never silently passing (VR-8).
          test "two same-seed runs yield byte-stable deterministic evidence (live-pixel surface excluded)" {
              let cfg = ControlPassRunner.testConfig (Some ControlPassFixtures.firstCatalogPage)
              let _, recordsA, _ = ControlPassRunner.evaluate cfg
              let _, recordsB, _ = ControlPassRunner.evaluate cfg

              let deterministicView (r: ControlVerdictRecord) =
                  r.ControlId, r.Classification, r.ClassificationReason, r.FunctionalVerdict, r.BehaviorsExercised, r.InteractionStates, r.DamageEvidence

              Expect.equal (recordsA |> List.map deterministicView) (recordsB |> List.map deterministicView) "same-seed deterministic evidence is identical across runs"
          }

          test "functional verdicts are deterministic across runs (G-4)" {
              let cfg = ControlPassRunner.testConfig (Some ControlPassFixtures.firstCatalogPage)
              let _, recordsA, _ = ControlPassRunner.evaluate cfg
              let _, recordsB, _ = ControlPassRunner.evaluate cfg
              let verdicts rs = rs |> List.map (fun r -> r.ControlId, r.FunctionalVerdict)
              Expect.equal (verdicts recordsA) (verdicts recordsB) "functional verdicts are identical across runs"
          }

          // --- US1: fail-closed environment-limited degradation (T011, G-6/FR-008) ---------
          test "live-pixel cells never silently pass when capture is degraded/blocked (VR-8)" {
              for r in records do
                  for cell in r.VisualEvidence do
                      match cell.CaptureStatus with
                      | Degraded
                      | BlockedCapture ->
                          Expect.notEqual cell.FidelityVerdict Approved (sprintf "%s: degraded/blocked capture is never silently Approved" cell.TargetId)
                      | _ -> ()
          }

          test "with no renderable surface, live-pixel visual verdicts are EnvironmentLimited, never Approved (G-6)" {
              if not env.Renderable then
                  for r in records do
                      Expect.notEqual r.VisualVerdict VisualApproved (sprintf "%s: headless visual verdict is not a silent Approved" r.ControlId)
              else
                  skiptest "host has a renderable surface — live-pixel degradation path not exercised"
          }

          test "functional dimension stays authoritative headless — interactive controls are exercised" {
              // The Pure-backend functional path does not depend on a window; it must run regardless.
              let interactive = records |> List.filter (fun r -> r.Classification = Interactive)
              Expect.isNonEmpty interactive "there are interactive controls"

              for r in interactive do
                  Expect.isNonEmpty r.BehaviorsExercised (sprintf "interactive '%s' has exercised behaviors even headless" r.ControlId)
          }

          // --- US2: matrix completeness (T017, M-1/M-2, SC-003) ----------------------------
          test "every control has rest evidence for both appearances x both sizes (FR-003)" {
              let expected =
                  Set.ofList
                      [ AntLight, Preferred
                        AntLight, Minimum
                        AntDark, Preferred
                        AntDark, Minimum ]

              for r in records do
                  let restCells =
                      r.VisualEvidence
                      |> List.filter (fun c -> c.State = Rest)
                      |> List.map (fun c -> c.Appearance, c.Size)
                      |> Set.ofList

                  Expect.equal restCells expected (sprintf "control '%s' has all four rest cells" r.ControlId)
          }

          test "every interactive control has at least one captured interaction state (FR-004)" {
              for r in records do
                  if r.Classification = Interactive then
                      Expect.isNonEmpty r.InteractionStates (sprintf "interactive '%s' has interaction-state evidence" r.ControlId)
          }

          // --- US2: state-differs-from-rest + damage-locality (T018, M-2/M-4, FR-005) ------
          test "interaction-state verdicts are consistent with the rest-delta (no silent dead affordance)" {
              for r in records do
                  for s in r.InteractionStates do
                      if s.DiffersFromRest then
                          Expect.equal s.Verdict Pass (sprintf "%s: a real delta is a Pass" s.EvidenceRef)
                      else
                          Expect.notEqual s.Verdict Pass (sprintf "%s: no delta is never a silent Pass" s.EvidenceRef)
          }

          test "broad/full-surface damage is never silently passed (M-4, FR-005)" {
              for r in records do
                  for d in r.DamageEvidence do
                      match d.DamageStatus with
                      | Broad
                      | FullSurface ->
                          Expect.notEqual d.Verdict Pass (sprintf "%s: broad/full-surface damage carries a non-Pass verdict" d.TransitionId)
                      | _ -> ()
          }

          test "each interactive control records one damage outcome per driven behavior (G-5)" {
              for r in records do
                  if r.Classification = Interactive then
                      Expect.equal (List.length r.DamageEvidence) (List.length r.BehaviorsExercised) (sprintf "control '%s' records damage per behavior" r.ControlId)
          }

          test "continuous-input slider is interactive and discloses its continuous-feedback evidence path" {
              match records |> List.tryFind (fun r -> r.ControlId = "slider") with
              | Some slider ->
                  Expect.equal slider.Classification Interactive "slider is interactive"

                  Expect.isTrue
                      (slider.Diagnostics |> List.exists (fun d -> d.Contains "continuous-input"))
                      "slider discloses where continuous-input feedback is validated (Feature 173/174)"
              | None -> failtest "slider control is in the catalog"
          }

          // --- US2: overlay appear/dismiss (T019, M-5/FR-015) ------------------------------
          test "transient overlays appear and dismiss through the pure model (M-5, FR-015)" {
              let seed = Host.initModel

              let roundTrips =
                  [ "overlay", PageMsg(OverlayToggled true), PageMsg(OverlayToggled false), (fun (m: SecondAntShowcaseModel) -> m.PageState.OverlayOpen)
                    "dialog", PageMsg(DialogToggled true), PageMsg(DialogToggled false), (fun m -> m.PageState.DialogOpen)
                    "drawer", PageMsg(DrawerToggled true), PageMsg(DrawerToggled false), (fun m -> m.PageState.DrawerOpen) ]

              for label, openMsg, closeMsg, project in roundTrips do
                  let opened = Model.update openMsg seed
                  let dismissed = Model.update closeMsg opened
                  Expect.isTrue (project opened) (sprintf "%s appears when triggered" label)
                  Expect.isFalse (project dismissed) (sprintf "%s dismisses when triggered" label)
          }

          // --- findings are recorded, never silently dropped -------------------------------
          test "auto-detected findings are recorded with a non-terminal lifecycle for triage (FR-009)" {
              for f in findings do
                  Expect.equal f.Lifecycle Found (sprintf "finding %s starts in Found for US3 triage" f.FindingId)
                  Expect.isNonEmpty f.AffectedControls (sprintf "finding %s names affected controls" f.FindingId)
          } ]
