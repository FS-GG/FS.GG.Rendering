module Feature170RetainedInspectionTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.UI.Scene
open FS.GG.UI.Themes.Default

type private Msg = Noop

let private theme = Theme.light
let private size: Size = { Width = 420; Height = 280 }
let private scope: VisualInspectionScope = { ScopeId = "retained-fixture"; Title = "Retained Fixture"; Required = true }

let private text key value =
    TextBlock.create [ TextBlock.text value ] |> Control.withKey key

let private inspect transition =
    ControlInspection.inspectRetained
        { Scope = scope
          Theme = theme
          OutputSize = size
          Presentation = "light"
          RunId = Some "feature170"
          Transition = transition
          RelatedVisualEvidence = [] }

[<Tests>]
let tests =
    testList
        "Feature170 retained inspection"
        [ test "retained transition classifies reused repainted shifted added and removed nodes" {
              let prior: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ text "stable" "Stable"
                              text "repaint" "Before"
                              text "shifted" "Shifted"
                              text "removed" "Removed" ] ]

              let current: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ text "stable" "Stable"
                              text "repaint" "After"
                              text "inserted-before-shift" "Inserted"
                              text "shifted" "Shifted"
                              text "added" "Added" ] ]

              let artifact =
                  inspect
                      { TransitionId = "localized-change"
                        PriorControl = Some prior
                        CurrentControl = current
                        InteractionId = Some "hover"
                        ExpectedAffectedRegionIds = [ "content" ]
                        MaximumDirtyPercentage = Some 80.0
                        IntentionalExceptions = [] }

              let status nodeId =
                  artifact.RetainedNodes |> List.find (fun node -> node.NodeId = nodeId) |> fun node -> node.Status

              Expect.equal artifact.ReadinessStatus RetainedInspectionStatus.Accepted "retained evidence accepted"
              Expect.equal (status "stable") RetainedNodeStatus.Reused "stable node reused"
              Expect.equal (status "repaint") RetainedNodeStatus.Repainted "content change repainted"
              Expect.equal (status "shifted") RetainedNodeStatus.Shifted "sibling insertion shifted retained node"
              Expect.equal (status "removed") RetainedNodeStatus.Removed "removed node visible"
              Expect.equal (status "added") RetainedNodeStatus.Added "added node visible"
              Expect.exists artifact.RetainedNodes (fun node -> node.NodeId = "stable" && node.RetainedIdentity.IsSome) "opaque retained identity recorded"
              Expect.isSome artifact.FinalVisualArtifact "final visual inspection artifact is linked"
              match artifact.Damage with
              | Some damage -> Expect.contains damage.AffectedNodeIds "shifted" "damage lists affected node ids"
              | None -> failtest "expected retained damage evidence"
          }

          test "first retained frame reports not-inspected damage with a first-frame diagnostic" {
              let first: Control<Msg> =
                  Stack.create [ Stack.children [ text "title" "First frame" ] ]

              let artifact =
                  inspect
                      { TransitionId = "first-frame"
                        PriorControl = None
                        CurrentControl = first
                        InteractionId = None
                        ExpectedAffectedRegionIds = []
                        MaximumDirtyPercentage = None
                        IntentionalExceptions = [] }

              let damage = artifact.Damage |> Option.get
              Expect.equal damage.DamageStatus DamageInspectionStatus.NotInspected "first frame cannot inspect prior damage"
              Expect.exists damage.Diagnostics (fun diagnostic -> diagnostic.Contains("no prior")) "diagnostic names no prior frame"
              Expect.exists artifact.RetainedNodes (fun node -> node.NodeId = "title" && node.Status = RetainedNodeStatus.Added) "first-frame nodes are added"
          } ]
