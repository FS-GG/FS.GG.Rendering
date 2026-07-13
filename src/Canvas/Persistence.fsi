namespace FS.GG.UI.Canvas

open System

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
///
/// `[<RequireQualifiedAccess>]` deliberately: `Saved` / `Loaded` / `Failed` are among the most collidable
/// names in any codebase — `ViewerLifecycleState` already owns some of them, and this repo is actively
/// fighting DU cases that shadow `Result.Error` (#496/#522). An unqualified `Failed` here would be a new
/// instance of a bug class the org is mid-way through removing, so `PersistenceOutcome.Failed` it is.
///
/// What a HOST reports back after actually performing a `PersistenceEffect` — the answer half of the
/// vocabulary, which until #535 did not exist at all. A product that requested a `Load` had nowhere to
/// receive the result: `PersistenceEffect` is request-only, and the viewer's effect fold "has no channel
/// to answer on". So a `Load` could be asked and never answered, and nothing said so.
///
/// A host produces these; the pure surface never does. `Persistence.interpretRecordOnly` cannot invent one
/// — it writes no bytes, so it has nothing to report — which is exactly why a `Requested`-only test suite
/// passes green against a backend that writes nothing (see the `fs-gg-persistence` skill).
///
/// `Absent` and `Unreadable` are DELIBERATELY distinct, and collapsing them is the bug this type exists to
/// prevent: "there is no save here" is a normal answer a product handles by starting fresh, while "there
/// are bytes here and they cannot be used" is data loss the player must be told about. One `LoadFailed`
/// case would let a corrupt save be silently reported as a new game, and the corruption would be
/// overwritten by the next autosave.
[<RequireQualifiedAccess>]
type PersistenceOutcome =
    /// The save was written and is durable. Carries the slot it landed in.
    | Saved of slot: SaveSlot
    /// The slot held a readable save. Carries the envelope the host read back — the product's own bytes,
    /// verbatim, with the version it stamped.
    | Loaded of envelope: SaveEnvelope
    /// The slot holds NO save. A normal answer, not a failure: a new player has no save. Handle it by
    /// starting fresh — never by treating it as an error.
    | Absent of slot: SaveSlot
    /// The slot holds bytes that could not be turned back into a save — truncated, corrupt, or written by
    /// a version this product refuses. NOT `Absent`: something was there and is now unusable, and the
    /// player is about to lose it. Tell them, and do not silently overwrite it.
    | Unreadable of slot: SaveSlot * reason: string
    /// The slot was deleted (or was already absent — deletion is idempotent).
    | Deleted of slot: SaveSlot
    /// The host could not perform the request at all: the disk was full, the location was not writable,
    /// the process lacked permission. Distinct from `Unreadable`, which is about the SAVE's bytes; this is
    /// about the request never completing.
    | Failed of effect: PersistenceEffect * reason: string

/// Public contract type exposed by this FS.GG.UI package.
/// Ordered evidence of what a product *requested*, produced by the record-only interpreter. It is
/// evidence of intent, NOT of durability: nothing in this framework writes a byte, so `Requested`
/// proves your `update` asked to save — never that a save happened.
type PersistenceEvidence =
    { /// Requested effects in dispatch order, oldest first, with `Save` versions normalized and
      /// payloads carried verbatim. Recorded and dropped: no file was written, read, or deleted.
      Requested: PersistenceEffect list }

/// Public contract module exposed by this FS.GG.UI package.
/// The persistence request vocabulary plus a pure RECORD-ONLY interpreter. A product's `update`
/// emits `PersistenceEffect` values (it never reads or writes a file); `interpretRecordOnly` folds
/// a batch into `PersistenceEvidence` — it records the requests and drops them.
///
/// `interpretRecordOnly` STILL WRITES NOTHING, and that has not changed: it records the requests and
/// drops them, and evidence of what a product asked for is not evidence that anything was saved.
///
/// What changed with #535 is that the requests now have somewhere to GO. `ViewerEffect.Persist` carries
/// a batch to a host, and `Viewer.runAppWithPersistence` hands it to a sink that performs the real I/O
/// and reports each `PersistenceOutcome` back into `update` as a message — so a `Load` is finally
/// answerable. Before that case existed, no host could ever see a `PersistenceEffect` at all, and a
/// product could request a save that nothing in the framework could possibly perform.
///
/// The framework still owns no save location: `SaveSlot` is an opaque, product-owned name, and the sink
/// that resolves it to a real path is the product's. What it no longer does is accept the request and
/// quietly drop it.
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
    /// Record-only interpreter over a batch, preserving dispatch order: it RECORDS the requests
    /// into `PersistenceEvidence.Requested` and DROPS them. No file is written, read, or deleted by
    /// THIS function — it is the record-only interpreter, and that is all it is. A host reached through
    /// `ViewerEffect.Persist` / `Viewer.runAppWithPersistence` is what actually performs the I/O (#535);
    /// this fold is what a headless test uses to assert what the product ASKED for.
    /// Headless-safe: no filesystem access, never blocks, never throws. The evidence it returns
    /// proves what the product ASKED for, and nothing about durability.
    val interpretRecordOnly: effects: PersistenceEffect list -> PersistenceEvidence

    /// Public contract function exposed by this FS.GG.UI package.
    /// DEPRECATED — call `interpretRecordOnly`, which is this function under a name that does not
    /// lie. Behaviour is identical (this forwards to it).
    ///
    /// WHAT THE NAME ACTUALLY LIES ABOUT. It is not that `interpret*` means *perform the effect*
    /// elsewhere and this one is the exception — that is what this comment used to say, and it was
    /// false twice over (#619). It cited an `interpretEffect` on the `GlHost` module, which has never
    /// existed in any version of anything (the spelling is left un-dotted here on purpose: a doc-symbol
    /// extractor cannot tell a phantom being CORRECTED from one being TAUGHT, and #597 is closing the
    /// very hole that let it ship). And NO public `interpret*` in this framework performs anything. Every one
    /// of them is a pure fold to a VALUE: `Audio.interpret` calls itself a "record-only interpreter"
    /// with "no device access", `Layout.interpretWorkflowEffect` returns a `Msg`, the
    /// `ControlsElmish.interpret*` family returns an `AdapterCommand`. Performing an effect is a HOST
    /// call, and a host call takes a backend or an engine.
    ///
    /// The real lie is a promised DOWNSTREAM. `interpret` invites you to believe something, somewhere,
    /// carries these requests out. For persistence nothing does: no `ViewerEffect` case carries a
    /// `PersistenceEffect`, so the fold records into `PersistenceEvidence.Requested` and drops them,
    /// and the pipeline the name implies has no other end. The honest version of the point is the
    /// stronger one — `interpret*` never performs, so a reader who expects it to is misled by the
    /// convention, not by an exception to it.
    [<Obsolete("Persistence.interpret PERSISTS NOTHING: it records the requests into PersistenceEvidence.Requested and drops them. No file is written, read, or deleted, and no host in this framework will ever interpret a PersistenceEffect (no ViewerEffect case carries one). Call Persistence.interpretRecordOnly instead — identical behaviour, honest name. If you need durability you must write the backend yourself.")>]
    val interpret: effects: PersistenceEffect list -> PersistenceEvidence
