module SecondAntShowcase.Tests.ControlPassCoverageTests

open Expecto
open SecondAntShowcase.Core
open SecondAntShowcase.Core.ControlPass

/// T004 / T008 — the catalog is the completeness oracle (C-1/G-2/VR-1) and the interaction
/// contract is the behavior oracle (C-4/G-3, SC-002). These tests anchor "every control" and
/// "every documented behavior" to the two single-sources-of-truth so neither can silently drift.
[<Tests>]
let coverageTests =
    testList
        "ControlPass coverage"
        [ test "the plan emits exactly one record per cataloged control (set equality, no missing/dup)" {
              let records = ControlPass.planRecords ()
              let recordIds = records |> List.map (fun r -> r.ControlId)
              let catalog = ControlPass.catalogControlIds ()

              Expect.equal (List.length recordIds) (List.length catalog) "one record per catalog id"
              Expect.equal (List.length (List.distinct recordIds)) (List.length recordIds) "record ids are distinct"
              Expect.equal (Set.ofList recordIds) (Set.ofList catalog) "record-id set equals the catalog set"
              Expect.isTrue (ControlPass.completenessHolds records) "completeness holds (G-2)"
          }

          test "template pages introduce no new control ids — template-reachable maps onto the catalog" {
              let catalog = Set.ofList (ControlPass.catalogControlIds ())
              let reachable = ControlPass.templateReachable ()

              for id in reachable do
                  Expect.isTrue (catalog.Contains id) (sprintf "template-reachable '%s' is a catalog id" id)
          }

          test "completenessGaps reports a missing id when a record is dropped" {
              let records = ControlPass.planRecords () |> List.tail
              let missing, _ = ControlPass.completenessGaps records
              Expect.isNonEmpty missing "dropping a record surfaces a missing id"
              Expect.isFalse (ControlPass.completenessHolds records) "completeness fails when a record is missing"
          }

          test "every control classifies as Interactive or DisplayOnly with a reason (D2, VR-2)" {
              for id in ControlPass.catalogControlIds () do
                  let classification, reason = ControlPass.classify id

                  match classification with
                  | DisplayOnly -> Expect.isFalse (System.String.IsNullOrWhiteSpace reason) (sprintf "display-only '%s' has a reason" id)
                  | Interactive -> Expect.isNonEmpty (ControlPass.behaviorsFor id) (sprintf "interactive '%s' has >=1 documented behavior" id)
          }

          test "behavior coverage: every Interactive control exercises every documented behavior (C-4/G-3, SC-002)" {
              // The oracle is the interaction contract: BehaviorsExercised must cover every contract
              // that references the control — not one representative action. (Caveat per T008: this
              // proves exercised==declared; contract completeness is reviewed in T004's fold.)
              let seed = Host.initModel

              for id in ControlPass.catalogControlIds () do
                  match fst (ControlPass.classify id) with
                  | Interactive ->
                      let declared = ControlPass.behaviorsFor id |> List.map (fun c -> c.ContractId) |> Set.ofList
                      let exercised = ControlPass.exerciseControl seed id |> List.map (fun b -> b.BehaviorId) |> Set.ofList
                      Expect.equal exercised declared (sprintf "control '%s' exercises every documented behavior" id)
                  | DisplayOnly -> ()
          } ]
