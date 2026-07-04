// Contract draft (Phase 1) for feature 244 — the FSI-first sketch of the public persistence
// request surface. This is the design surface exercised in FSI before any .fs exists
// (Constitution Principle I). The shipped file will be src/Canvas/Persistence.fsi.
//
// Design intent (mirrors feature 243 Audio.fsi):
//   * PersistenceEffect is a PURE value: a requested save/load action, carrying data only —
//     never a filesystem handle, stream, or IO. Product `update` returns these; it never
//     reads or writes a file.
//   * The save payload is OPAQUE to the framework: the product author serializes their own
//     Model into a SavePayload and stamps a version. The framework carries it but never
//     parses, validates, or re-encodes it (the product owns the on-disk format — kept out of
//     the library, like per-game stat mapping in symbology).
//   * The record-only interpreter folds a batch of requests into ordered evidence. This is
//     the headless / no-writable-location path: the recorded requests ARE the evidence.
//   * A real file-backed backend (SkiaViewer host) — including dispatching a load *result*
//     back to the model — is deferred; it will consume the same PersistenceEffect values
//     without changing this surface (FR-007).

namespace FS.GG.UI.Canvas

/// Opaque, product-owned identifier for a save location (e.g. "slot-1", "autosave").
/// The framework does not own the slot -> path mapping.
type SaveSlot = SaveSlot of string

/// Opaque, already-serialized save data owned by the product. The framework carries it
/// verbatim and never parses, validates, or re-encodes it — the product chooses the format.
type SavePayload = SavePayload of string

/// A versioned save envelope the product fills in before requesting a Save. The Version is a
/// product-stamped save-format version enabling a future load to migrate or reject old saves;
/// the framework never interprets the Payload.
type SaveEnvelope =
    { /// Product-stamped save-format version (>= minVersion; normalized at the boundary).
      Version: int
      /// Target save slot.
      Slot: SaveSlot
      /// Opaque, product-serialized payload.
      Payload: SavePayload }

/// A requested persistence action, expressed as a pure value from product `update`.
type PersistenceEffect =
    | Save of envelope: SaveEnvelope
    | Load of slot: SaveSlot
    | DeleteSlot of slot: SaveSlot

/// Ordered evidence of what a product requested, produced by the record-only interpreter.
/// This is the primary, filesystem-free evidence for the headless path (US2).
type PersistenceEvidence =
    { /// Requested effects in dispatch order (oldest first).
      Requested: PersistenceEffect list }

[<RequireQualifiedAccess>]
module Persistence =

    /// The lowest save-format version the surface normalizes to.
    val minVersion: int

    /// Clamp a stamped version to >= minVersion. Total; never throws (Principle VI).
    val clampVersion: version: int -> int

    /// Smart constructor for a save envelope: clamps the version and wraps the payload.
    val saveEnvelope: version: int -> slot: SaveSlot -> payload: string -> SaveEnvelope

    /// Smart constructors (return plain request values).
    val save: envelope: SaveEnvelope -> PersistenceEffect
    val load: slot: SaveSlot -> PersistenceEffect
    val deleteSlot: slot: SaveSlot -> PersistenceEffect

    /// Empty evidence (no requests yet).
    val emptyEvidence: PersistenceEvidence

    /// Record-only interpreter: append one requested effect to evidence (pure, total).
    /// Normalizes a Save envelope's carried version so recorded evidence is normalized.
    /// A Load/DeleteSlot of an unknown slot is recorded faithfully — never an error here
    /// (how a real backend reports "no such save" is a deferred-backend concern).
    val record: effect: PersistenceEffect -> evidence: PersistenceEvidence -> PersistenceEvidence

    /// Record-only interpreter over a batch, preserving dispatch order. This is the
    /// headless-safe "host boundary" for the minimal slice: no filesystem access, never
    /// blocks, never throws (FR-006). Returns the accumulated evidence.
    val interpret: effects: PersistenceEffect list -> PersistenceEvidence
