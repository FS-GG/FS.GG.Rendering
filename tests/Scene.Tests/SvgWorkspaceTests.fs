module SvgWorkspaceTests

open System.Text
open Expecto
open FS.GG.UI.Scene

let private panel id state =
    state.Layout.Panels |> List.find (fun value -> value.Id = id)

let private emptyDocument =
    {
        Schema = SvgDocument.schema
        Id = "workspace-fixture"
        ViewBox =
            {
                X = 0.0
                Y = 0.0
                Width = 100.0
                Height = 100.0
            }
        Definitions = []
        Children = []
    }

[<Tests>]
let tests =
    testList
        "SVG workspace"
        [
            test "responsive collapse preserves preferred layout and authoring owner state" {
                let author =
                    SvgAuthoring.tryCreate
                        0
                        emptyDocument
                        {
                            Schema = SvgAsset.catalogSchema
                            Assets = []
                        }
                        []
                    |> Result.defaultWith (failtestf "%A")

                let art =
                    { SvgArt.initialState with
                        Selection = []
                    }

                let workspace = SvgWorkspace.init SvgWorkspaceMode.Arrange 1280.0

                let updateWorkspace message (ownedAuthor, ownedArt, ownedWorkspace) =
                    let next, effects = SvgWorkspace.update message ownedWorkspace
                    (ownedAuthor, ownedArt, next), effects

                let (authorAfterNarrow, artAfterNarrow, narrow), _ =
                    updateWorkspace (SvgWorkspaceMessage.SetViewportWidth 600.0) (author, art, workspace)

                Expect.equal (panel "tools" narrow).Effective SvgPanelPlacement.Collapsed "side dock collapses"

                Expect.equal
                    (panel "timeline" narrow).Effective
                    (SvgPanelPlacement.Docked(SvgWorkspaceDock.Bottom, 0))
                    "bottom dock remains"

                let (authorAfterWide, artAfterWide, wide), _ =
                    updateWorkspace
                        (SvgWorkspaceMessage.SetViewportWidth 1280.0)
                        (authorAfterNarrow, artAfterNarrow, narrow)

                Expect.equal
                    (panel "tools" wide).Effective
                    (panel "tools" workspace).Preferred
                    "preferred dock restores"

                Expect.equal authorAfterWide author "scene and history owner is unchanged"
                Expect.equal artAfterWide SvgArt.initialState "camera and selection owner is unchanged"
                Expect.equal wide.FocusTarget "scene" "focus stays meaningful"
            }

            test "layout codec is deterministic bounded and rejects malformed state" {
                let layout =
                    { SvgWorkspace.defaultLayout with
                        Revision = 4
                        Panels =
                            [
                                {
                                    Id = "floating"
                                    Preferred = SvgPanelPlacement.Floating(1.5, 2.5, 320.0, 200.0)
                                    Effective = SvgPanelPlacement.Collapsed
                                }
                                {
                                    Id = "dock"
                                    Preferred = SvgPanelPlacement.Docked(SvgWorkspaceDock.Right, 3)
                                    Effective = SvgPanelPlacement.Collapsed
                                }
                            ]
                    }

                let bytes = SvgWorkspace.encodeLayout layout
                let decoded = SvgWorkspace.decodeLayout bytes |> Result.defaultWith (failtestf "%A")
                Expect.equal decoded.Panels.[0].Preferred layout.Panels.[0].Preferred "floating coordinates round-trip"
                Expect.equal (SvgWorkspace.encodeLayout decoded) bytes "canonical bytes round-trip"

                let duplicate =
                    Encoding.UTF8.GetString bytes
                    |> fun value -> value.Replace("end\n", "panel\tZG9jaw==\tc\nend\n") |> Encoding.UTF8.GetBytes

                Expect.isError (SvgWorkspace.decodeLayout duplicate) "duplicate panel is rejected before construction"

                let malformed =
                    Encoding.UTF8.GetBytes
                        "fsgg.svg-workspace-layout\t1\nschema\t!!!\nrevision\t0\npanel\teA==\tf,NaN,0,1,1\nend\n"

                Expect.isError (SvgWorkspace.decodeLayout malformed) "invalid base64 and non-finite placement refuse"

                Expect.equal
                    (SvgWorkspace.decodeLayout (Array.zeroCreate 65537))
                    (Error [ "layout-byte-limit" ])
                    "byte budget is enforced"
            }

            test "invalid updates and imports preserve the previous workspace" {
                let initial = SvgWorkspace.init SvgWorkspaceMode.Create 900.0

                let invalidPlacement, _ =
                    SvgWorkspace.update
                        (SvgWorkspaceMessage.SetPanelPlacement(
                            "tools",
                            SvgPanelPlacement.Floating(0.0, 0.0, -1.0, 20.0)
                        ))
                        initial

                Expect.equal invalidPlacement initial "invalid placement is atomic"

                let imported, effects =
                    SvgWorkspace.update (SvgWorkspaceMessage.ImportLayout(Encoding.UTF8.GetBytes "bad")) initial

                Expect.equal imported initial "invalid import preserves previous state"
                Expect.isNonEmpty effects "invalid import is diagnosed"
            }

            test "modes and overlays derive contexts and restore stable focus" {
                let initial = SvgWorkspace.init SvgWorkspaceMode.Play 900.0

                Expect.equal
                    (SvgWorkspace.activeContexts initial)
                    [ "workspace"; "workspace.play" ]
                    "mode is projected into context"

                let opened, _ =
                    SvgWorkspace.update (SvgWorkspaceMessage.OpenPalette "toolbar.play") initial

                Expect.equal
                    (SvgWorkspace.activeContexts opened)
                    [ "workspace"; "workspace.play"; "workspace.palette" ]
                    "overlay context is innermost"

                Expect.equal opened.FocusTarget "workspace.command-palette" "overlay receives a stable focus target"
                let closed, effects = SvgWorkspace.update SvgWorkspaceMessage.CloseOverlay opened
                Expect.equal closed.FocusTarget "toolbar.play" "focus returns to the stable owner"
                Expect.contains effects (SvgWorkspaceEffect.RequestFocus "toolbar.play") "host receives focus request"
                let invalid, _ = SvgWorkspace.update (SvgWorkspaceMessage.OpenHelp "") closed
                Expect.equal invalid closed "invalid modal target cannot steal focus"
            }

            test "accepted placement revisions persist while viewport changes remain preferences-free" {
                let initial = SvgWorkspace.init SvgWorkspaceMode.Review 500.0

                let moved, effects =
                    SvgWorkspace.update
                        (SvgWorkspaceMessage.SetPanelPlacement(
                            "tools",
                            SvgPanelPlacement.Docked(SvgWorkspaceDock.Right, 2)
                        ))
                        initial

                Expect.equal moved.Layout.Revision 1 "accepted preference advances revision"

                Expect.equal
                    (panel "tools" moved).Preferred
                    (SvgPanelPlacement.Docked(SvgWorkspaceDock.Right, 2))
                    "preferred edge changes"

                Expect.equal
                    (panel "tools" moved).Effective
                    SvgPanelPlacement.Collapsed
                    "responsive effective placement remains collapsed"

                Expect.exists
                    effects
                    (function
                    | SvgWorkspaceEffect.PersistLayout bytes -> SvgWorkspace.decodeLayout bytes |> Result.isOk
                    | _ -> false)
                    "canonical preference bytes are persisted"

                let resized, _ =
                    SvgWorkspace.update (SvgWorkspaceMessage.SetViewportWidth 1000.0) moved

                Expect.equal resized.Layout.Revision 1 "responsive projection is not a preference edit"

                Expect.equal
                    (panel "tools" resized).Effective
                    moved.Layout.Panels.Head.Preferred
                    "wide projection restores the accepted dock"
            }
        ]
