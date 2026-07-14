// See skill: fs-gg-persistence
namespace FS.GG.UI.Canvas

/// Public contract type exposed by this FS.GG.UI package.
/// Opaque, product-owned identifier naming a save location (e.g. "slot-1", "autosave"). The
/// framework does not own the slot -> path mapping (kept out of the library, like per-game stat
/// mapping in symbology); a product resolves it to a real location in its own host layer.
type SaveSlot = SaveSlot of string

/// Public contract type exposed by this FS.GG.UI package.
/// Opaque, already-serialized save data owned by the product. The framework carries it verbatim
/// and never parses, validates, or re-encodes it — the product chooses the on-disk format.
type SavePayload = SavePayload of string

/// Public contract type exposed by this FS.GG.UI package.
/// A versioned save envelope the product fills in before requesting a `Save`. `Version` is a
/// product-stamped save-format version (normalized to `>= minVersion` at the boundary) that lets a
/// future load migrate or reject old saves; the framework never interprets `Payload`.
type SaveEnvelope =
    { /// Product-stamped save-format version, normalized to `>= minVersion`.
      Version: int
      /// Target save slot.
      Slot: SaveSlot
      /// Opaque, product-serialized payload.
      Payload: SavePayload }

/// Public contract type exposed by this FS.GG.UI package.
/// A requested save/load action, expressed as a pure value from a product's `update`. Data only —
/// no case carries a filesystem handle, stream, or effectful closure. `Load`/`DeleteSlot` of an
/// unknown slot are valid request values; how a real backend reports "no such save" is a deferred
/// concern (the pure surface only requests).
type PersistenceEffect =
    | Save of envelope: SaveEnvelope
    | Load of slot: SaveSlot
    | DeleteSlot of slot: SaveSlot

/// Public contract type exposed by this FS.GG.UI package.
/// Which backend produced a `PersistenceEvidence` value. `RecordOnly` is the only case: the framework
/// owns no save location, so no evidence value it produces was ever durable.
type PersistenceBackend = RecordOnly

/// Public contract type exposed by this FS.GG.UI package.
/// Ordered evidence of what a product *requested*, produced by the record-only interpreter. It is
/// evidence of intent, NOT of durability: this fold writes no byte, so `Requested` proves your
/// `update` asked to save — never that a save happened. `Backend` says so on the type itself.
type PersistenceEvidence =
    { /// Requested effects in dispatch order, oldest first, with `Save` versions normalized and
      /// payloads carried verbatim. Recorded and dropped: no file was written, read, or deleted.
      Requested: PersistenceEffect list

      /// Which interpreter produced this value — always `RecordOnly`. Nothing was persisted, and you
      /// learn it from the type rather than from a doc comment.
      Backend: PersistenceBackend }

/// Public contract module exposed by this FS.GG.UI package.
/// The persistence request vocabulary plus a pure RECORD-ONLY interpreter. A product's `update`
/// emits `PersistenceEffect` values (it never reads or writes a file); `interpretRecordOnly` folds a
/// batch into `PersistenceEvidence` — it records the requests and drops them.
///
/// `interpretRecordOnly` writes nothing, and its name and its `Backend` mark both say so. What it is
/// NOT is a dead end: `ViewerEffect.Persist` carries a batch out to a host, and
/// `Viewer.runAppWithPersistence` hands it to a sink that does the real I/O and reports each
/// `PersistenceOutcome` back into `update` as a message — so a `Load` is answerable.
///
/// The framework still owns no save location: `SaveSlot` is an opaque, product-owned name, and the
/// sink that resolves it to a real path is yours to write. See the `fs-gg-persistence` skill.
[<RequireQualifiedAccess>]
module Persistence =

    /// Public contract value exposed by this FS.GG.UI package.
    /// The lowest save-format version the surface normalizes to.
    val minVersion: int

    /// Public contract function exposed by this FS.GG.UI package.
    /// Clamp a stamped version to `>= minVersion`. Total; never throws (Principle VI).
    val clampVersion: version: int -> int

    /// Public contract function exposed by this FS.GG.UI package.
    /// Smart constructor for a save envelope: clamps the version to `>= minVersion` and wraps the
    /// opaque payload. The payload is stored verbatim and never inspected.
    val saveEnvelope: version: int -> slot: SaveSlot -> payload: string -> SaveEnvelope

    /// Public contract function exposed by this FS.GG.UI package.
    /// Smart constructor for a save request.
    val save: envelope: SaveEnvelope -> PersistenceEffect

    /// Public contract function exposed by this FS.GG.UI package.
    /// Smart constructor for a load request. The load result is dispatched back to the model by a
    /// deferred host backend; this pure value only requests the read.
    val load: slot: SaveSlot -> PersistenceEffect

    /// Public contract function exposed by this FS.GG.UI package.
    /// Smart constructor for a delete-slot request. A no-op at the interpreter if the slot is
    /// empty/absent.
    val deleteSlot: slot: SaveSlot -> PersistenceEffect

    /// Public contract value exposed by this FS.GG.UI package.
    /// Evidence with no requests recorded yet.
    val emptyEvidence: PersistenceEvidence

    /// Public contract function exposed by this FS.GG.UI package.
    /// Record-only interpreter over a single requested effect: append it to the evidence (pure,
    /// total). A `Save` envelope's carried version is normalized so recorded evidence is in range;
    /// the payload is carried verbatim.
    val record: effect: PersistenceEffect -> evidence: PersistenceEvidence -> PersistenceEvidence

    /// Public contract function exposed by this FS.GG.UI package.
    /// Record-only interpreter over a batch, preserving dispatch order: it RECORDS the requests into
    /// `PersistenceEvidence.Requested` and DROPS them. No file is written, read, or deleted by THIS
    /// function — that is what its name says, and the `Backend` mark on what it returns says it again.
    /// Headless-safe: no filesystem access, never blocks, never throws.
    ///
    /// It is not the end of the line, though. To actually persist, send the batch to a host with
    /// `ViewerEffect.Persist` and run under `Viewer.runAppWithPersistence`, supplying the sink that
    /// does the I/O; its `PersistenceOutcome` values come back into `update` as messages. This fold is
    /// what a headless test uses to assert what the product ASKED for, which is a different question
    /// from whether anything was saved.
    ///
    /// It was called `interpret` up to FS.GG.UI 0.9.0. That name promised a downstream it did not have,
    /// and it was removed at the 0.10.0 major — this is the same function under a name that does not lie.
    val interpretRecordOnly: effects: PersistenceEffect list -> PersistenceEvidence
