module AdapterCmdTests

open Expecto
open FsCheck
open FsCheck.FSharp
open Elmish
open FS.Skia.UI.Controls
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.Controls.Elmish

/// Run an Elmish `Cmd<'msg>` through a recording dispatcher and return the
/// messages it delivered, in dispatch order.
let dispatchedMessages (cmd: Cmd<'msg>) : 'msg list =
    let recorded = System.Collections.Generic.List<'msg>()
    cmd |> List.iter (fun effect -> effect (fun msg -> recorded.Add msg))
    List.ofSeq recorded

/// Total route used by the round-trip oracle: each effect maps to a distinct
/// integer tag, so a dispatched-vs-mapped comparison proves order preservation
/// and that no `AdapterEffect` case is dropped.
let private tag (effect: AdapterEffect<int>) : int =
    match effect with
    | DispatchProductMessage m -> m
    | DispatchControlRuntimeMessage _ -> -1
    | DispatchKeyboardMessage _ -> -2
    | DispatchHostCommand _ -> -3
    | ReportAdapterDiagnostic _ -> -4

/// Generator over every `AdapterEffect` case (product and non-product), so the
/// totality/order property exercises the whole union, not just product messages.
let private effectGen : Gen<AdapterEffect<int>> =
    Gen.oneof
        [ Gen.map DispatchProductMessage (Gen.choose (0, 100000))
          Gen.constant (DispatchControlRuntimeMessage ControlRuntimeMsg.Reset)
          Gen.constant (DispatchKeyboardMessage KeyboardMsg.Reset)
          Gen.map DispatchHostCommand (Gen.elements [ "a"; "b"; "c" ])
          Gen.constant (ReportAdapterDiagnostic { Code = "c"; Message = "m"; Source = "s" }) ]

let private commandGen : Gen<AdapterCommand<int>> = Gen.listOf effectGen

[<Tests>]
let adapterCmdTests =
    testList "AdapterCmd bridge (AdapterCommand <-> Elmish Cmd)" [
        test "empty command maps to the Elmish no-op command (FR-003 empty edge)" {
            // Cmd<'msg> is a list of effect functions (no equality); assert structurally
            // that both are the empty effect list and dispatch nothing.
            let cmd = AdapterCmd.toCmd tag []
            Expect.isEmpty cmd "toCmd route [] is the empty effect list"
            Expect.isEmpty (AdapterCmd.none: Cmd<int>) "none is the empty effect list (= Cmd.none)"
            Expect.isEmpty (dispatchedMessages cmd) "empty command dispatches nothing"
        }

        test "ofMessage lifts exactly one product message" {
            Expect.equal
                (AdapterCmd.productMessages (AdapterCmd.ofMessage 7))
                [ 7 ]
                "productMessages (ofMessage m) = [ m ]"
        }

        test "productMessages projects only DispatchProductMessage payloads in order" {
            let command =
                [ DispatchProductMessage 1
                  DispatchHostCommand "host"
                  DispatchProductMessage 2
                  DispatchControlRuntimeMessage ControlRuntimeMsg.Reset
                  DispatchProductMessage 3 ]

            Expect.equal (AdapterCmd.productMessages command) [ 1; 2; 3 ] "non-product effects do not contribute"
        }

        test "two product effects dispatch exactly those messages, in order, none dropped or duplicated (US2 acceptance 1)" {
            let command = [ DispatchProductMessage "first"; DispatchProductMessage "second" ]
            let projectProduct =
                function
                | DispatchProductMessage m -> m
                | _ -> "unexpected"

            let delivered = dispatchedMessages (AdapterCmd.toCmd projectProduct command)
            Expect.equal delivered [ "first"; "second" ] "exactly the carried product messages, in order"
        }

        test "non-product effects are carried, not dropped — bridge is total (edge: non-product effects)" {
            let command =
                [ DispatchControlRuntimeMessage ControlRuntimeMsg.Reset
                  DispatchKeyboardMessage KeyboardMsg.Reset
                  DispatchHostCommand "h"
                  ReportAdapterDiagnostic { Code = "x"; Message = "y"; Source = "z" } ]

            let delivered = dispatchedMessages (AdapterCmd.toCmd tag command)
            Expect.equal delivered [ -1; -2; -3; -4 ] "every non-product case routed, in order"
        }

        test "FsCheck round-trip over real commands: product-message dispatch equals productMessages (>=1000 cases, SC-003/FR-008)" {
            // Property over real generated commands — no mocks, no canned fixtures.
            let productRoundTrip (msgs: int list) =
                let command = msgs |> List.map DispatchProductMessage
                let projectProduct =
                    function
                    | DispatchProductMessage m -> m
                    | _ -> System.Int32.MinValue

                dispatchedMessages (AdapterCmd.toCmd projectProduct command) = AdapterCmd.productMessages command

            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, productRoundTrip)
        }

        test "FsCheck totality/order: dispatch order equals List.map route over every effect case (>=1000 cases, FR-003)" {
            let dispatchPreservesOrder (command: AdapterCommand<int>) =
                dispatchedMessages (AdapterCmd.toCmd tag command) = List.map tag command

            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen commandGen) dispatchPreservesOrder)
        }
    ]
