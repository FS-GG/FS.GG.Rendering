module Canvas.Tests.PersistenceTests

// Feature 244 (US1 + US2): pure persistence (save/load) request surface + record-only interpreter.
// US1 — a product's `update` emits PersistenceEffect values with zero IO.
// US2 — the record-only interpreter folds requests into ordered PersistenceEvidence, headless-safe:
//        never touches a filesystem, never blocks, never throws, normalizes carried Save versions,
//        carries the opaque payload verbatim.
// All real pure computation — no synthetic evidence (nothing is faked; the recorded requests ARE
// the evidence).

open Expecto
open FsCheck
open FS.GG.UI.Canvas

// --- US1: a tiny pure product model whose `update` maps game events to requested persistence
//     effects. This stands in for a real product's update: pure, returns PersistenceEffect values,
//     does no IO. The product serializes its own Model into the opaque payload.

type private GameEvent =
    | CheckpointReached of score: int
    | ContinueGame
    | EraseSave

// The whole point: `update` is a pure function to a PersistenceEffect — no filesystem, no IO, no
// state. The product owns the payload format (here a trivial string stand-in for a serialized Model).
let private update (event: GameEvent) : PersistenceEffect =
    match event with
    | CheckpointReached score -> Persistence.save (Persistence.saveEnvelope 1 (SaveSlot "slot-1") (sprintf "{score:%d}" score))
    | ContinueGame -> Persistence.load (SaveSlot "slot-1")
    | EraseSave -> Persistence.deleteSlot (SaveSlot "slot-1")

[<Tests>]
let tests =
    testList "Feature 244 Persistence request surface + record-only interpreter (US1, US2)" [

        // ---- US1: pure request surface ----

        test "update maps game events to the exact requested persistence effects (US1)" {
            let requested = [ CheckpointReached 42; ContinueGame; EraseSave ] |> List.map update
            Expect.equal
                requested
                [ Save { Version = 1; Slot = SaveSlot "slot-1"; Payload = SavePayload "{score:42}" }
                  Load(SaveSlot "slot-1")
                  DeleteSlot(SaveSlot "slot-1") ]
                "each event yields its requested effect, as a plain value"
        }

        test "saveEnvelope carries the opaque payload verbatim (US1)" {
            let env = Persistence.saveEnvelope 3 (SaveSlot "autosave") "<opaque\tbytes\n{not:parsed}>"
            Expect.equal env.Payload (SavePayload "<opaque\tbytes\n{not:parsed}>") "payload stored byte-for-byte, never parsed"
            Expect.equal env.Slot (SaveSlot "autosave") "slot carried through"
            Expect.equal env.Version 3 "in-range version carried through"
        }

        test "saveEnvelope clamps a negative version to minVersion (US1, Principle VI)" {
            let env = Persistence.saveEnvelope -5 (SaveSlot "s") "p"
            Expect.equal env.Version Persistence.minVersion "negative version clamps to minVersion"
        }

        test "clampVersion is total (US1, Principle VI)" {
            Expect.equal (Persistence.clampVersion 7) 7 "in-range passes through"
            Expect.equal (Persistence.clampVersion 0) Persistence.minVersion "minVersion passes through"
            Expect.equal (Persistence.clampVersion -100) Persistence.minVersion "under-min -> minVersion, no exception"
        }

        testCase "clampVersion result is always >= minVersion (FsCheck >=1000 cases)" <| fun () ->
            let prop (version: int) =
                Persistence.clampVersion version >= Persistence.minVersion
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, prop)

        // ---- US2: record-only interpreter ----

        test "interpret records requests in dispatch order (US2)" {
            let ev =
                Persistence.interpret
                    [ Persistence.save (Persistence.saveEnvelope 1 (SaveSlot "slot-1") "{score:42}")
                      Persistence.load (SaveSlot "slot-1")
                      Persistence.deleteSlot (SaveSlot "old") ]
            Expect.equal
                ev.Requested
                [ Save { Version = 1; Slot = SaveSlot "slot-1"; Payload = SavePayload "{score:42}" }
                  Load(SaveSlot "slot-1")
                  DeleteSlot(SaveSlot "old") ]
                "recorded oldest-first, faithfully"
        }

        test "interpret normalizes an out-of-range Save version in recorded evidence (US2)" {
            // A raw Save built WITHOUT the smart ctor still gets its version normalized at the boundary;
            // the payload is carried verbatim (never re-encoded).
            let ev =
                Persistence.interpret
                    [ Save { Version = -9; Slot = SaveSlot "s"; Payload = SavePayload "raw" } ]
            Expect.equal
                ev.Requested
                [ Save { Version = Persistence.minVersion; Slot = SaveSlot "s"; Payload = SavePayload "raw" } ]
                "boundary clamps the carried version; payload untouched"
        }

        test "record appends to evidence oldest-first (US2)" {
            let ev =
                Persistence.emptyEvidence
                |> Persistence.record (Persistence.save (Persistence.saveEnvelope 1 (SaveSlot "a") "p"))
                |> Persistence.record (Persistence.load (SaveSlot "a"))
            Expect.equal
                ev.Requested
                [ Save { Version = 1; Slot = SaveSlot "a"; Payload = SavePayload "p" }; Load(SaveSlot "a") ]
                "second record lands after the first"
        }

        test "emptyEvidence and interpret [] are both empty (US2)" {
            Expect.equal Persistence.emptyEvidence.Requested [] "emptyEvidence has no requests"
            Expect.equal (Persistence.interpret []).Requested [] "interpreting no effects records nothing"
        }

        test "Load/DeleteSlot of an unknown slot is a well-defined recorded no-op, not an error (US2)" {
            // The interpreter does not throw or check "does this slot exist"; that is a deferred-backend
            // concern. It just records the requested read/delete faithfully.
            let ev = Persistence.interpret [ Persistence.load (SaveSlot "never-saved"); Persistence.deleteSlot (SaveSlot "empty") ]
            Expect.equal ev.Requested [ Load(SaveSlot "never-saved"); DeleteSlot(SaveSlot "empty") ] "unknown-slot load/delete records without error"
        }

        // Random-input coverage using FsCheck's auto-generation: every recorded Save version is
        // normalized, payloads are carried verbatim, and the batch count is preserved, over arbitrary
        // versions including negatives, and arbitrary payload strings.
        testCase "interpret preserves count, normalizes versions, carries payloads verbatim (FsCheck >=500 cases)" <| fun () ->
            let prop (items: (int * string) list) =
                let effects =
                    items |> List.map (fun (v, p) -> Save { Version = v; Slot = SaveSlot "s"; Payload = SavePayload p })
                let ev = Persistence.interpret effects
                List.length ev.Requested = List.length effects
                && List.forall2
                    (fun original recorded ->
                        match original, recorded with
                        | Save o, Save r -> r.Version >= Persistence.minVersion && r.Payload = o.Payload
                        | _ -> false)
                    effects
                    ev.Requested
            Check.One(Config.QuickThrowOnFailure.WithMaxTest 500, prop)
    ]
