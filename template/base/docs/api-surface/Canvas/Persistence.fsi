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
/// Ordered evidence of what a product requested, produced by the record-only interpreter. This is
/// the primary, filesystem-free evidence for the headless path: the recorded requests ARE the
/// evidence (no real save I/O is involved).
type PersistenceEvidence =
    { /// Requested effects in dispatch order, oldest first, with `Save` versions normalized and
      /// payloads carried verbatim.
      Requested: PersistenceEffect list }

/// Public contract module exposed by this FS.GG.UI package.
/// The persistence request vocabulary plus a pure record-only interpreter. A product's `update`
/// emits `PersistenceEffect` values (it never reads or writes a file); the interpreter folds a
/// batch into `PersistenceEvidence`. A real file-backed backend — including the load *result* it
/// dispatches back to the model — is deferred and will consume the same values without changing
/// this surface.
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
    /// Record-only interpreter over a batch, preserving dispatch order. This is the headless-safe
    /// host boundary: no filesystem access, never blocks, never throws. Returns the accumulated
    /// evidence.
    val interpret: effects: PersistenceEffect list -> PersistenceEvidence
