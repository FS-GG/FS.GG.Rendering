namespace FS.GG.UI.Canvas

// Feature 244: pure persistence (save/load) request surface + record-only interpreter. A product's
// `update` emits PersistenceEffect values (never reads or writes a file); the interpreter folds a
// batch into ordered evidence. Scene-only, dependency-light. The payload is opaque: the product
// serializes its own Model and this surface carries the bytes verbatim, never parsing them.
//
// Issue #445 (epic .github#416, the silent no-op family): the record-only status lives in the NAME
// (`interpretRecordOnly`) and, since #537, in the TYPE (`PersistenceEvidence.Backend`), not only in
// this comment. Candor in a comment is not a mechanism — it does not survive being called from
// another file. The deprecated `interpret` forwarder that carried the third channel (an `[<Obsolete>]`
// diagnostic) was removed at the 0.10.0 major; the name and the type-mark outlive it.
//
// Issue #535: the requests now have somewhere to go. `ViewerEffect.Persist` carries a batch to a host and
// `Viewer.runAppWithPersistence` hands it to a caller-supplied sink, whose `PersistenceOutcome` values are
// dispatched back into `update` — so a Load is answerable. This module still writes no bytes, and that is
// the point of its name: it records intent. The BACKEND is the product's, because the framework does not
// own the SaveSlot -> path mapping.

type SaveSlot = SaveSlot of string

type SavePayload = SavePayload of string

type SaveEnvelope =
    { Version: int
      Slot: SaveSlot
      Payload: SavePayload }

type PersistenceEffect =
    | Save of envelope: SaveEnvelope
    | Load of slot: SaveSlot
    | DeleteSlot of slot: SaveSlot

// #535 — the ANSWER half of the vocabulary. `Absent` and `Unreadable` are deliberately distinct: one
// `LoadFailed` case would let a corrupt save be reported as a new game, and the next autosave would
// overwrite it. See the .fsi for the full rationale.
[<RequireQualifiedAccess>]
type PersistenceOutcome =
    | Saved of slot: SaveSlot
    | Loaded of envelope: SaveEnvelope
    | Absent of slot: SaveSlot
    | Unreadable of slot: SaveSlot * reason: string
    | Deleted of slot: SaveSlot
    | Failed of effect: PersistenceEffect * reason: string

type PersistenceBackend = RecordOnly

type PersistenceEvidence =
    { Requested: PersistenceEffect list
      Backend: PersistenceBackend }

[<RequireQualifiedAccess>]
module Persistence =

    let minVersion = 0

    // Total clamp to the >= minVersion floor. A negative (nonsensical) version becomes minVersion —
    // a defined, non-throwing floor (Principle VI: safe failure, no surprise on bad input).
    let clampVersion (version: int) : int =
        if version < minVersion then minVersion else version

    let saveEnvelope (version: int) (slot: SaveSlot) (payload: string) : SaveEnvelope =
        { Version = clampVersion version
          Slot = slot
          Payload = SavePayload payload }

    let save (envelope: SaveEnvelope) : PersistenceEffect = Save envelope

    let load (slot: SaveSlot) : PersistenceEffect = Load slot

    let deleteSlot (slot: SaveSlot) : PersistenceEffect = DeleteSlot slot

    // Normalize the version carried by a Save envelope so recorded evidence is always in range,
    // regardless of whether the caller went through the smart constructor. The payload is never
    // touched — the framework does not own the format. Load/DeleteSlot pass through unchanged.
    let private normalize (effect: PersistenceEffect) : PersistenceEffect =
        match effect with
        | Save envelope -> Save { envelope with Version = clampVersion envelope.Version }
        | Load _
        | DeleteSlot _ -> effect

    let emptyEvidence: PersistenceEvidence =
        { Requested = []
          Backend = RecordOnly }

    // Append to the tail so Requested stays oldest-first without a reverse per call. Requested is a
    // small per-frame batch, so the O(n) append is not a hot path.
    let record (effect: PersistenceEffect) (evidence: PersistenceEvidence) : PersistenceEvidence =
        { evidence with Requested = evidence.Requested @ [ normalize effect ] }

    let interpretRecordOnly (effects: PersistenceEffect list) : PersistenceEvidence =
        { Requested = List.map normalize effects
          Backend = RecordOnly }
