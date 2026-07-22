module Issue535PersistenceSeamTests

// #535 — persistence could REQUEST a save and prove nothing.
//
// Before this seam existed, no `ViewerEffect` carried a `PersistenceEffect`. So a product's `update`
// could emit `Persistence.save …` and *no host could ever see it*: `interpretRecordOnly` was the only
// thing that would ever consume one, and it records and drops. Worse, `PersistenceEffect` is
// request-ONLY — there was no answer vocabulary at all, so a `Load` could be asked and never answered.
// The viewer's own effect fold said as much about its queries: "a fold returning `bool` has no channel
// to answer on — a product cannot observe a reply that has nowhere to go."
//
// The live persistent runners gate on `runtimeCapability.PersistentWindow` (false headless) and are not
// drivable here — the same limitation #365/#396 record, and the same reason #429/#438 assert the AUDIO
// sink on the shared fold directly. So the WIRING is asserted on `interpretViewerEffects`, which is
// `internal` for exactly this purpose, and the answer path is asserted on the mapper contract.

open System.Collections.Generic
open Expecto
open FS.GG.UI.Canvas
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private slot = SaveSlot "slot-1"

let private envelope =
    { Version = 1
      Slot = slot
      Payload = SavePayload "{score:42}" }

type private Msg =
    | RequestSave
    | SaveAnswered of PersistenceOutcome

type private Model = { Saves: int }

let private persistenceHost: GeneratedAppHost<Model, Msg> =
    { Init = fun () -> { Saves = 0 }, []
      Update =
        fun msg model ->
            match msg with
            | RequestSave -> model, [ Persist [ Persistence.save envelope ] ]
            | SaveAnswered _ -> { model with Saves = model.Saves + 1 }, []
      View = fun model -> Text((0.0, 0.0), $"saves {model.Saves}", { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy })
      MapKey = fun _ _ -> None
      Tick = fun _ -> None
      Diagnostics = Viewer.defaultDiagnostics }

[<Tests>]
let issue535PersistenceSeamTests =
    testList
        "Issue535 persistence host seam"
        [
          // The PUBLIC entry point, not just the internal fold above. `runAppWithPersistence` is what a
          // product actually calls and what `fs-gg-skiaviewer` documents, and nothing exercised it — a
          // documented seam no test calls is precisely the "may be dead" shape this epic exists to catch.
          // It cannot open a window headlessly (`PersistentWindow` is false), so what is assertable here
          // is the contract that holds on EVERY host: it fails exactly as `runApp` does, and a launch that
          // never happened touches nobody's disk.
          test "runAppWithPersistence never reaches the sink on a host that cannot open a window" {
              let written = List<PersistenceEffect>()

              if Viewer.runtimeCapability().PersistentWindow then
                  skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
              else
                  let options =
                      { Title = "Product"
                        InitialSize = { Width = 640; Height = 480 }
                        PresentMode = ViewerPresentMode.OffscreenReadback
                        FrameRateCap = None
                        LogicalSize = None }

                  let sink batch =
                      written.AddRange batch
                      batch |> List.map (fun e -> PersistenceOutcome.Failed(e, "no host"))

                  match Viewer.runAppWithPersistence options sink (SaveAnswered >> Some) persistenceHost with
                  | Result.Ok _ -> failtest "an unsupported host cannot report a successful launch"
                  | Result.Error failure ->
                      Expect.equal
                          failure.Classification
                          UnsupportedEnvironment
                          "runAppWithPersistence classifies an unsupported host exactly as runApp does"

                  Expect.isEmpty written "no save is written when no window ever opened"
          }

          // #979 — the four-capability corner: window behavior AND audio AND persistence at once. Mirrors
          // the sibling above (a headless host cannot open a persistent window, so the composition itself
          // is not drivable here — #365/#396), and asserts the contract that holds on EVERY host: the
          // launcher classifies an unsupported host exactly as `runApp` does, and a launch that never
          // happened hands NOTHING to either sink. Before this overload a windowed launch that also needed
          // saves had to fall back to a variant that pinned `defaultWindowBehavior`, silently dropping the
          // requested window behavior; the point of the test is that all four arguments are now threaded
          // through the one generated-app launch its siblings use.
          test "runAppWithWindowBehaviorAndAudioAndPersistence never reaches the audio or persistence sink on a host that cannot open a window" {
              let saved = List<PersistenceEffect>()
              let played = List<_>()

              if Viewer.runtimeCapability().PersistentWindow then
                  skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
              else
                  let options =
                      { Title = "Product"
                        InitialSize = { Width = 640; Height = 480 }
                        PresentMode = ViewerPresentMode.OffscreenReadback
                        FrameRateCap = None
                        LogicalSize = None }

                  // A window behavior that is NOT the default — so the argument is genuinely threaded, not
                  // defaulted away as the persistence-only fallback would have done.
                  let behavior =
                      { Viewer.defaultWindowBehavior with
                          ResizePolicy = FixedSize
                          StartupState = ViewerWindowStartupState.Normal }

                  let audioSink batch = played.AddRange batch

                  let persistenceSink batch =
                      saved.AddRange batch
                      batch |> List.map (fun e -> PersistenceOutcome.Failed(e, "no host"))

                  match
                      Viewer.runAppWithWindowBehaviorAndAudioAndPersistence
                          options
                          behavior
                          audioSink
                          persistenceSink
                          (SaveAnswered >> Some)
                          persistenceHost
                  with
                  | Result.Ok _ -> failtest "an unsupported host cannot report a successful launch"
                  | Result.Error failure ->
                      Expect.equal
                          failure.Classification
                          UnsupportedEnvironment
                          "runAppWithWindowBehaviorAndAudioAndPersistence classifies an unsupported host exactly as runApp does"

                  Expect.isEmpty saved "no save is written when no window ever opened"
                  Expect.isEmpty played "no audio is played when no window ever opened"
          }

          // THE BUG, DIRECTLY. A `Persist` batch must REACH a sink. If this fails, a product's save
          // requests are dropped on the floor by the framework and nothing anywhere says so.
          test "a Persist batch reaches the sink, in dispatch order" {
              let received = List<PersistenceEffect>()

              let effects =
                  [ Persist [ Persistence.save envelope; Persistence.load slot; Persistence.deleteSlot slot ] ]

              Viewer.interpretViewerEffects ignore received.AddRange ignore ignore ignore ignore effects
              |> ignore

              Expect.equal
                  (List.ofSeq received)
                  [ Save envelope; Load slot; DeleteSlot slot ]
                  "every requested effect reaches the host sink, in the order the product dispatched them — before #535 no ViewerEffect carried a PersistenceEffect at all, so none of them reached anything"
          }

          // Named for what it actually asserts. It says nothing about a launch: it pins that the fold only
          // routes a PERSIST batch to the persistence sink, and leaves every other effect alone. (The earlier
          // name claimed to test `runApp`'s drop behaviour and tested no such thing — the message gave it
          // away, and review called it.)
          test "the fold routes only a Persist batch to the persistence sink" {
              let mutable reached = false

              Viewer.interpretViewerEffects
                  ignore
                  (fun _ -> reached <- true)
                  ignore
                  ignore
                  ignore
                  ignore
                  [ RenderScene(Group []) ]
              |> ignore

              Expect.isFalse reached "a batch with no Persist effect must not reach the persistence sink"
          }

          // THE ANSWER PATH — driven through the REAL function, not a local re-enactment of it.
          //
          // Two earlier drafts of this test were theatre. The first defined its own sink and mapper and
          // looped over them by hand: it called no framework code and would have passed with
          // `ViewerEffect.Persist` deleted. The second re-implemented the launch body's forward-reference
          // shape locally, which pins the AUTHOR'S MODEL of the code rather than the code — and that is not a
          // hypothetical: this seam shipped an ordering bug that dropped every Init-time save, with 350 green
          // tests attached. `dispatchPersistenceBatch` is `internal` precisely so a test can reach the wiring
          // that `runAppWithPersistence` builds, because the launch itself bails at
          // `capability.PersistentWindow` and can never be driven headless.
          test "every outcome the sink returns is dispatched back as a product message" {
              let dispatched = List<string>()

              let sink (effects: PersistenceEffect list) =
                  effects
                  |> List.map (fun effect ->
                      match effect with
                      | Save e -> PersistenceOutcome.Saved e.Slot
                      | Load s -> PersistenceOutcome.Loaded { envelope with Slot = s }
                      | DeleteSlot s -> PersistenceOutcome.Deleted s)

              let mapOutcome outcome =
                  match outcome with
                  | PersistenceOutcome.Saved _ -> Some "saved"
                  | PersistenceOutcome.Loaded _ -> Some "loaded"
                  | PersistenceOutcome.Deleted _ -> Some "deleted"
                  | PersistenceOutcome.Absent _
                  | PersistenceOutcome.Unreadable _
                  | PersistenceOutcome.Failed _ -> None

              let dispatch msg =
                  dispatched.Add msg
                  false // no message asked to close

              let closeRequested =
                  Viewer.dispatchPersistenceBatch
                      sink
                      mapOutcome
                      dispatch
                      [ Persistence.save envelope; Persistence.load slot; Persistence.deleteSlot slot ]

              Expect.equal
                  (List.ofSeq dispatched)
                  [ "saved"; "loaded"; "deleted" ]
                  "a Load is finally ANSWERABLE: the sink performs the batch and every outcome comes back as a message the product's update handles"

              Expect.isFalse closeRequested "no dispatched message asked to close"
          }

          // An outcome the product has no message for is DROPPED — the host has still answered, and inventing
          // a message would be the framework deciding what "no save" means to a game.
          test "an outcome the product does not map is dropped, not invented" {
              let dispatched = List<string>()

              let closeRequested =
                  Viewer.dispatchPersistenceBatch
                      (fun _ -> [ PersistenceOutcome.Absent slot ])
                      (fun _ -> None) // the product maps nothing
                      (fun msg ->
                          dispatched.Add msg
                          false)
                      [ Persistence.load slot ]

              Expect.isEmpty dispatched "an unmapped outcome must not become a message the product never asked for"
              Expect.isFalse closeRequested "and it cannot request a close"
          }

          // A close asked for by an outcome-driven message must be honoured. Losing it would leave a product
          // that quits on a failed load stuck in a window it asked to shut.
          test "a close requested by an outcome-driven message is propagated" {
              let closeRequested =
                  Viewer.dispatchPersistenceBatch
                      (fun _ -> [ PersistenceOutcome.Unreadable(slot, "truncated") ])
                      (fun _ -> Some "quit")
                      (fun _ -> true) // this message asks to close
                      [ Persistence.load slot ]

              Expect.isTrue closeRequested "the close a product asked for on a corrupt save must reach the loop"
          }

          // THE CORRUPTION BUG THIS TYPE EXISTS TO PREVENT. `Absent` and `Unreadable` are different
          // answers, and a product must be able to tell them apart. Collapse them into one `LoadFailed`
          // and a corrupt save reads as "new game" — then the next autosave overwrites it, and the
          // player's save is gone with nothing having reported an error.
          test "PersistenceOutcome.Absent and PersistenceOutcome.Unreadable are distinct answers — collapsing them silently eats corruption" {
              let startedFresh = List<SaveSlot>()
              let warnedPlayer = List<SaveSlot * string>()

              let handle outcome =
                  match outcome with
                  | PersistenceOutcome.Absent s -> startedFresh.Add s
                  | PersistenceOutcome.Unreadable(s, reason) -> warnedPlayer.Add(s, reason)
                  | PersistenceOutcome.Saved _
                  | PersistenceOutcome.Loaded _
                  | PersistenceOutcome.Deleted _
                  | PersistenceOutcome.Failed _ -> ()

              handle (PersistenceOutcome.Absent slot)
              handle (PersistenceOutcome.Unreadable(slot, "truncated at byte 12"))

              Expect.equal (List.ofSeq startedFresh) [ slot ] "no save here is a NORMAL answer: start fresh"

              Expect.equal
                  (warnedPlayer |> Seq.map fst |> List.ofSeq)
                  [ slot ]
                  "bytes that cannot be read are NOT 'no save': the player is about to lose them, and must be told rather than silently handed a new game"

              Expect.notEqual
                  (box (PersistenceOutcome.Absent slot))
                  (box (PersistenceOutcome.Unreadable(slot, "truncated at byte 12")))
                  "the two answers must not be the same value — one type for both is the whole defect"
          }

          // A host that cannot perform the request at all is a THIRD thing: the disk was full, the location
          // was not writable. That is not "the save is corrupt" and not "there is no save".
          test "PersistenceOutcome.Failed is distinct from PersistenceOutcome.Unreadable — the request never completed" {
              let failure = PersistenceOutcome.Failed(Save envelope, "disk full")

              Expect.notEqual
                  (box failure)
                  (box (PersistenceOutcome.Unreadable(slot, "disk full")))
                  "`Failed` is about the REQUEST not completing; `Unreadable` is about the SAVE's bytes. A product retries one and warns about the other."
          }
        ]
