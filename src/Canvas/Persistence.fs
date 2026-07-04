namespace FS.GG.UI.Canvas

// Feature 244: pure persistence (save/load) request surface + record-only interpreter. A product's
// `update` emits PersistenceEffect values (never reads or writes a file); the interpreter folds a
// batch into ordered evidence. Scene-only, dependency-light — the real file-backed backend (and the
// load-result Msg it dispatches back to the model) is a deferred follow-up and will consume the same
// values without changing this surface. The payload is opaque: the product serializes its own Model
// and this surface carries the bytes verbatim, never parsing them.

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

type PersistenceEvidence = { Requested: PersistenceEffect list }

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

    let emptyEvidence: PersistenceEvidence = { Requested = [] }

    // Append to the tail so Requested stays oldest-first without a reverse per call. Requested is a
    // small per-frame batch, so the O(n) append is not a hot path.
    let record (effect: PersistenceEffect) (evidence: PersistenceEvidence) : PersistenceEvidence =
        { evidence with Requested = evidence.Requested @ [ normalize effect ] }

    let interpret (effects: PersistenceEffect list) : PersistenceEvidence =
        { Requested = List.map normalize effects }
