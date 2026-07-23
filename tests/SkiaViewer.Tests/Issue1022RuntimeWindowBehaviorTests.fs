module Issue1022RuntimeWindowBehaviorTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host
open Silk.NET.Maths
open Silk.NET.Windowing

let private request state =
    { Viewer.defaultWindowBehavior with
        StartupState = state
        BackendPreference = Some ViewerBackendPreference.OpenGL }

let private native mode border position size token : RuntimeWindowBehavior =
    { Mode = mode
      Border = border
      Position = position
      Size = size
      Token = token }

type private FakeWindow =
    { mutable State: WindowState
      mutable Border: WindowBorder
      mutable Position: Vector2D<int>
      mutable Size: Vector2D<int>
      Writes: ResizeArray<string>
      mutable ThrowOnSizeOnce: bool }

let private target fake : GlHost.RuntimeWindowTarget =
    { GetState = fun () -> fake.State
      SetState = fun value ->
          fake.Writes.Add $"state:{value}"
          fake.State <- value
      GetBorder = fun () -> fake.Border
      SetBorder = fun value ->
          fake.Writes.Add $"border:{value}"
          fake.Border <- value
      GetPosition = fun () -> fake.Position
      SetPosition = fun value ->
          fake.Writes.Add $"position:{value.X},{value.Y}"
          fake.Position <- value
      GetSize = fun () -> fake.Size
      SetSize = fun value ->
          if fake.ThrowOnSizeOnce then
              fake.ThrowOnSizeOnce <- false
              failwith "synthetic native size failure"

          fake.Writes.Add $"size:{value.X}x{value.Y}"
          fake.Size <- value }

let private initialFake () =
    { State = WindowState.Normal
      Border = WindowBorder.Resizable
      Position = Vector2D<int>(100, 80)
      Size = Vector2D<int>(1280, 720)
      Writes = ResizeArray()
      ThrowOnSizeOnce = false }

[<Tests>]
let runtimeWindowBehaviorTests =
    testList "Issue 1022 runtime window behavior" [
        test "the new host effect preserves the existing comparison contract" {
            let behavior =
                native RuntimeWindowMode.Normal WindowBorder.Resizable
                    (Some(100, 80)) (Some(1280, 720)) "windowed"
            let effect: Host.ViewerEffect<unit> = Host.ViewerEffect.ApplyWindowBehavior behavior
            let nongeneric: System.IComparable = effect :> System.IComparable
            let generic: System.IComparable<Host.ViewerEffect<unit>> =
                effect :> System.IComparable<Host.ViewerEffect<unit>>

            Expect.equal (nongeneric.CompareTo(box effect)) 0 "the non-generic comparison surface is retained"
            Expect.equal (generic.CompareTo effect) 0 "the generic comparison surface is retained"
        }

        test "the shared persistent interpreter routes every mode request in effect order" {
            let routed = ResizeArray<ViewerWindowStartupState>()
            let canvases = ResizeArray<Size>()
            let logical: Size = { Width = 1920; Height = 1080 }

            let effects =
                [ ApplyWindowOptions(request ViewerWindowStartupState.Normal)
                  ApplyLogicalCanvas logical
                  ApplyWindowOptions(request ViewerWindowStartupState.WindowedFullscreen)
                  ApplyWindowOptions(request ViewerWindowStartupState.Fullscreen) ]

            let closed =
                Viewer.interpretViewerEffectsWithRuntimeWindow
                    ignore ignore ignore ignore ignore ignore
                    (fun behavior -> routed.Add behavior.StartupState)
                    canvases.Add
                    effects

            Expect.isFalse closed "presentation changes do not close the persistent window"
            Expect.sequenceEqual
                routed
                [ ViewerWindowStartupState.Normal
                  ViewerWindowStartupState.WindowedFullscreen
                  ViewerWindowStartupState.Fullscreen ]
                "windowed, borderless and fullscreen all reach the live mutation sink"
            Expect.sequenceEqual canvases [ logical ] "logical-canvas ownership remains independent"
        }

        test "windowed borderless and fullscreen requests all produce native plans" {
            [ ViewerWindowStartupState.Normal, RuntimeWindowMode.Normal, "windowed"
              ViewerWindowStartupState.WindowedFullscreen, RuntimeWindowMode.WindowedFullscreen, "borderless"
              ViewerWindowStartupState.Fullscreen, RuntimeWindowMode.Fullscreen, "fullscreen" ]
            |> List.iter (fun (requested, expectedMode, expectedToken) ->
                let plan, diagnostics = Viewer.planRuntimeWindowBehavior (request requested)
                Expect.isSome plan $"{requested} has a native loop-thread plan"
                Expect.equal plan.Value.Mode expectedMode $"{requested} maps to the intended native mode"
                Expect.equal plan.Value.Token expectedToken $"{requested} has a stable observable token"
                Expect.isFalse
                    (diagnostics |> List.exists (fun diagnostic -> diagnostic.Level = ViewerDiagnosticLevel.Error))
                    $"{requested} has no validation failure")
        }

        test "native transitions are idempotent and restore prior windowed geometry" {
            let fake = initialFake ()
            let controller = GlHost.createRuntimeWindowController { Width = 1280; Height = 720 }
            let borderless =
                native RuntimeWindowMode.WindowedFullscreen WindowBorder.Hidden
                    (Some(0, 0)) (Some(1920, 1080)) "borderless"
            let fullscreen = native RuntimeWindowMode.Fullscreen WindowBorder.Hidden None None "fullscreen"
            let windowed = native RuntimeWindowMode.Normal WindowBorder.Resizable None None "windowed"

            Expect.equal (GlHost.applyRuntimeWindowBehavior controller (target fake) borderless) (Ok true) "borderless mutates the target"
            let writesAfterFirst = fake.Writes.Count
            Expect.equal (GlHost.applyRuntimeWindowBehavior controller (target fake) borderless) (Ok false) "the same request is a no-op"
            Expect.equal fake.Writes.Count writesAfterFirst "repetition performs no native writes"

            Expect.equal (GlHost.applyRuntimeWindowBehavior controller (target fake) fullscreen) (Ok true) "fullscreen reaches WindowState.Fullscreen"
            Expect.equal fake.State WindowState.Fullscreen "exclusive fullscreen is active"

            Expect.equal (GlHost.applyRuntimeWindowBehavior controller (target fake) windowed) (Ok true) "windowed exits fullscreen"
            Expect.equal fake.State WindowState.Normal "normal state is restored"
            Expect.equal fake.Border WindowBorder.Resizable "window chrome/resize policy is restored"
            Expect.equal fake.Position (Vector2D<int>(100, 80)) "pre-presentation position is restored"
            Expect.equal fake.Size (Vector2D<int>(1280, 720)) "pre-presentation size is restored"
        }

        test "unsupported backend is diagnosed and never receives a native plan" {
            let unsupported =
                { request ViewerWindowStartupState.Fullscreen with
                    BackendPreference = Some ViewerBackendPreference.Vulkan }

            let plan, diagnostics = Viewer.planRuntimeWindowBehavior unsupported
            Expect.isNone plan "an initialized OpenGL host cannot switch backend in place"
            Expect.exists diagnostics
                (fun diagnostic ->
                    diagnostic.Category = ViewerDiagnosticCategory.Window
                    && diagnostic.Level = ViewerDiagnosticLevel.Error
                    && diagnostic.Message.Contains "cannot switch backend")
                "the existing diagnostics channel carries the unsupported outcome"
        }

        test "native mutation failure restores the captured state and returns a Window diagnostic" {
            let fake = initialFake ()
            fake.ThrowOnSizeOnce <- true
            let controller = GlHost.createRuntimeWindowController { Width = 1280; Height = 720 }
            let borderless =
                native RuntimeWindowMode.WindowedFullscreen WindowBorder.Hidden
                    (Some(0, 0)) (Some(1920, 1080)) "borderless"

            match GlHost.applyRuntimeWindowBehavior controller (target fake) borderless with
            | Ok _ -> failtest "the synthetic native failure must escape as a diagnostic result"
            | Result.Error diagnostic ->
                Expect.equal diagnostic.Stage DiagnosticStage.Window "failure is classified at the native window boundary"
                Expect.stringContains diagnostic.Message "previous native-window state was restored" "rollback is explicit"

            Expect.equal fake.State WindowState.Normal "state rolled back"
            Expect.equal fake.Border WindowBorder.Resizable "border rolled back"
            Expect.equal fake.Position (Vector2D<int>(100, 80)) "position rolled back"
            Expect.equal fake.Size (Vector2D<int>(1280, 720)) "size rolled back"
        }
    ]
