namespace FS.GG.UI.KeyboardInput

/// Public contract type exposed by this FS.GG.UI package.
/// A structural reason a keymap document could not be decoded. `decode` returns *every* problem it
/// finds (not just the first), so a malformed binding list surfaces all offending entries at once.
type KeymapCodecError =
    /// The bytes are not well-formed JSON, or the root is not the expected object/array shape.
    | MalformedJson of detail: string
    /// The `format` discriminator is present but is not `KeymapCodec.formatId`.
    | UnsupportedFormat of format: string
    /// The `version` is a number this codec does not understand.
    | UnsupportedVersion of version: int
    /// A required envelope field (`format`, `version`, `bindings`) is absent.
    | MissingField of field: string
    /// A `bindings` entry is not an object with string `key` and `command` members.
    | InvalidBinding of detail: string

/// Public contract module exposed by this FS.GG.UI package.
/// JSON serialization/persistence for `Keymap`. JSON is the rendering contract format, so bindings
/// persist as a versioned UTF-8 JSON envelope (`{ format, version, bindings }`) — the inverse pair
/// `encode`/`decode` mirror the shape of `Scene/SceneCodec`. Encoding is deterministic: bindings are
/// emitted in key order (from `Keymap.toBindings`), so equal keymaps produce byte-identical output.
module KeymapCodec =

    /// Stable format discriminator written into, and required of, every document (`"fsgg.keymap"`).
    val formatId: string

    /// Envelope schema version this codec writes and is the only version it accepts.
    val formatVersion: int

    /// Encode a keymap to a UTF-8 JSON document with a versioned envelope. Deterministic and total.
    val encode: keymap: Keymap -> byte[]

    /// Decode a UTF-8 JSON document produced by `encode`. Returns the reconstructed keymap, or the
    /// structural errors found: an envelope-level problem (malformed JSON, unknown format, unsupported
    /// version, a missing field) short-circuits to a single error, while malformed `bindings` entries
    /// are all reported together. A duplicated key in `bindings` collapses last-wins, as `ofBindings` does.
    val decode: bytes: byte[] -> Result<Keymap, KeymapCodecError list>
