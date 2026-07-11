namespace FS.GG.UI.Canvas

// Feature 244: pure persistence (save/load) request surface + record-only interpreter. A product's
// `update` emits PersistenceEffect values (never reads or writes a file); the interpreter folds a
// batch into ordered evidence. Scene-only, dependency-light. The payload is opaque: the product
// serializes its own Model and this surface carries the bytes verbatim, never parsing them.
//
// Issue #445 (epic .github#416, the silent no-op family): the record-only status now lives in the
// NAME (`interpretRecordOnly`) and on the compiler's diagnostic channel (`[<Obsolete>]` on the old
// `interpret`), not only in this comment. Candor in a comment is not a mechanism — it does not
// survive being called from another file. The file-backed backend is NOT "a deferred follow-up"
// that exists somewhere: it does not exist at all, and no ViewerEffect case carries a
// PersistenceEffect, so no host can route these requests to one.

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

    let interpretRecordOnly (effects: PersistenceEffect list) : PersistenceEvidence =
        { Requested = List.map normalize effects }

    // A forwarder kept at its exact published signature: removing or reshaping it is an ApiCompat
    // break, so it retires with the next framework major (#537), not before. The `[<Obsolete>]` sits
    // on the val in the .fsi (Principle II), where it is a hard build error at every call site.
    let interpret (effects: PersistenceEffect list) : PersistenceEvidence = interpretRecordOnly effects
