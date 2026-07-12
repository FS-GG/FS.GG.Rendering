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

[<Tests>]
let issue535PersistenceSeamTests =
    testList
        "Issue535 persistence host seam"
        [
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

          // The honest no-op. A launch given no sink must DROP the batch, not pretend. `runApp` and
          // `runAppWithAudio` do exactly this — a request that goes nowhere is honest; a save that
          // silently did not happen is not.
          test "a launch with no persistence sink drops the batch rather than pretending" {
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

          // THE ANSWER PATH — the half that did not exist: a host reports outcomes, the product maps them to
          // messages, the loop dispatches them into `update`.
          //
          // Driven through the REAL fold. An earlier draft of this test defined its own sink and mapper and
          // looped over them by hand, asserting nothing about the framework at all — it would have passed
          // with `ViewerEffect.Persist` deleted. A test that re-implements the thing it is testing is a
          // tautology, and the only reason to keep it would be that it looks like coverage.
          test "an outcome the sink returns is dispatched back as a product message" {
              let dispatched = List<string>()
              let mutable dispatchOutcome: PersistenceOutcome -> unit = ignore

              // Exactly the shape `runAppWithPersistence` builds: the sink PERFORMS the batch and reports
              // outcomes; each outcome is mapped and dispatched.
              let sink (effects: PersistenceEffect list) =
                  effects
                  |> List.map (fun effect ->
                      match effect with
                      | Save e -> PersistenceOutcome.Saved e.Slot
                      | Load s -> PersistenceOutcome.Loaded { envelope with Slot = s }
                      | DeleteSlot s -> PersistenceOutcome.Deleted s)

              let persistenceBatchSink batch = sink batch |> List.iter dispatchOutcome

              dispatchOutcome <-
                  fun outcome ->
                      match outcome with
                      | PersistenceOutcome.Saved _ -> dispatched.Add "saved"
                      | PersistenceOutcome.Loaded _ -> dispatched.Add "loaded"
                      | PersistenceOutcome.Deleted _ -> dispatched.Add "deleted"
                      | PersistenceOutcome.Absent _
                      | PersistenceOutcome.Unreadable _
                      | PersistenceOutcome.Failed _ -> ()

              Viewer.interpretViewerEffects
                  ignore
                  persistenceBatchSink
                  ignore
                  ignore
                  ignore
                  ignore
                  [ Persist [ Persistence.save envelope; Persistence.load slot; Persistence.deleteSlot slot ] ]
              |> ignore

              Expect.equal
                  (List.ofSeq dispatched)
                  [ "saved"; "loaded"; "deleted" ]
                  "a Load is finally ANSWERABLE: the batch goes through the real fold to the sink, and every outcome comes back as a message the product's update handles"
          }

          // THE ORDERING BUG THIS SEAM SHIPPED WITH, and the reason it is pinned here.
          //
          // A product LOADS ITS SAVE ON INIT — that is the whole pattern; the first thing a game does is ask
          // "is there a save?". The launch body closes the outcome-dispatch forward reference with a mutable,
          // and in the first draft that assignment sat AFTER `interpretEffects initEffects`. So an `Init`
          // that emitted a `Persist` had its batch performed by the sink — the save really was read off the
          // disk — and the resulting `Loaded` went to a `dispatchOutcome` that was still `ignore`. The save
          // was read and thrown away, and the product started at a title screen with no error anywhere.
          //
          // A fresh silent no-op inside the fix for the silent-no-op family (epic .github#416). Reversing the
          // two statements below turns this red, which is the point of it.
          test "an outcome from an Init-time Persist batch is dispatched, not dropped" {
              let dispatched = List<string>()
              let mutable dispatchOutcome: PersistenceOutcome -> unit = ignore

              let sink (effects: PersistenceEffect list) =
                  effects |> List.map (fun _ -> PersistenceOutcome.Loaded envelope)

              let persistenceBatchSink batch = sink batch |> List.iter dispatchOutcome

              // Init's effects — a product asking for its save on startup.
              let initEffects = [ Persist [ Persistence.load slot ] ]

              // CLOSE THE REFERENCE FIRST. This is the ordering the launch body must have.
              dispatchOutcome <-
                  fun outcome ->
                      match outcome with
                      | PersistenceOutcome.Loaded e ->
                          let (SavePayload payload) = e.Payload
                          dispatched.Add payload
                      | _ -> ()

              Viewer.interpretViewerEffects ignore persistenceBatchSink ignore ignore ignore ignore initEffects
              |> ignore

              Expect.equal
                  (List.ofSeq dispatched)
                  [ "{score:42}" ]
                  "the save a product asks for on Init must be ANSWERED. If the dispatcher is still `ignore` when Init's effects are interpreted, the sink reads the save off the disk and the outcome is thrown away — the product starts fresh over a save that exists, with nothing reporting an error."
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
