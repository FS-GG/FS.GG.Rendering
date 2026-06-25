# Contract: Symbology Grammar-Breadth Public Surface

**Package**: `FS.GG.UI.Symbology` (`src/Symbology/Symbology.fsi`) — pure, Scene-only.
**Change**: Tier 1 additive. Adds `type Grammar` + new `val`s to `module Symbology`. Existing surface unchanged.

This is the `.fsi` sketch authored **first** (constitution I/II) and FSI-exercised before any `.fs` body.

---

## New type

```fsharp
/// The selectable symbol form factor. All three consume the SAME fixed Token channel set (FR-002):
/// one `'stats -> Token` mapping drives any grammar unchanged. The choice changes the DRAWING, never
/// the per-game ChannelMap.
[<RequireQualifiedAccess>]
type Grammar =
    | Token
    | Badge
    | Ring
```

## Additions to `module Symbology` (existing vals retained, unchanged)

```fsharp
[<RequireQualifiedAccess>]
module Symbology =

    // ---- EXISTING — signatures and behaviour UNCHANGED (FR-010) ----
    val defaultToken: Token
    val token: token: Token -> Scene
    val animate: motion: Motion -> token: Token -> phase: float -> Scene
    val gallery: cols: int -> spacing: float -> tokens: Token list -> Scene
    val filmstrip: samples: int -> entries: (Motion * Token) list -> Scene

    // ---- NEW grammars (FR-001) ----

    /// The Badge element: a compact, screen-aligned framed emblem encoding EVERY channel (FR-003).
    /// Heading is a discrete edge indicator, not whole-body rotation (FR-006). Pure & deterministic
    /// (FR-004). A degenerate token (R <= 0) degrades to a visible placeholder (FR-005); never throws.
    val badge: token: Token -> Scene

    /// The Ring element: a centred radial gauge encoding EVERY channel (FR-003). Continuous channels
    /// read as radial/arc quantities; the health arc sweep is monotone in Health (FR-007). Heading is a
    /// discrete needle, not body rotation (FR-006). Pure & deterministic (FR-004); R <= 0 -> placeholder.
    val ring: token: Token -> Scene

    // ---- NEW grammar dispatch + grammar-parameterized boards (FR-008) ----

    /// Render a token in the SELECTED grammar. `render Grammar.Token` reproduces `token` byte-for-byte.
    val render: grammar: Grammar -> token: Token -> Scene

    /// Reproducible grid of symbols drawn in the selected grammar (FR-008). Empty/single roster OK.
    /// `galleryIn Grammar.Token` reproduces `gallery` byte-for-byte (FR-010).
    val galleryIn: grammar: Grammar -> cols: int -> spacing: float -> tokens: Token list -> Scene

    /// Motion filmstrip in the selected grammar; only grammar-agnostic overlays apply on Badge/Ring
    /// (FR-014). `filmstripIn Grammar.Token` reproduces `filmstrip` byte-for-byte.
    val filmstripIn: grammar: Grammar -> samples: int -> entries: (Motion * Token) list -> Scene

    /// Deterministic motion overlay in the selected grammar (FR-014). On Badge/Ring, applies only the
    /// grammar-agnostic centre/radius overlays (Pulse/Blink/Damage); directional motions degrade to the
    /// static base symbol. `animateIn Grammar.Token` reproduces `animate` byte-for-byte.
    val animateIn: grammar: Grammar -> motion: Motion -> token: Token -> phase: float -> Scene
```

## Contract guarantees

| ID | Guarantee |
|---|---|
| C-G1 | `badge`/`ring` render **every** `Token` channel so varying any one channel changes the canonical bytes (FR-003/SC-002). |
| C-G2 | All new functions are **pure**: identical input ⇒ identical `Scene` ⇒ identical canonical bytes, in-process and cross-process (FR-004/SC-003). |
| C-G3 | `R <= 0` ⇒ a **visible placeholder** in every grammar; **no exception** on degenerate or valid input (FR-005/SC-004). |
| C-G4 | Badge/Ring are **screen-aligned**; heading is a discrete indicator (FR-006). |
| C-G5 | Ring health **arc sweep is monotone non-decreasing** in `Health` over `[0,1]` (FR-007). |
| C-G6 | `render Grammar.Token`, `galleryIn Grammar.Token`, `filmstripIn Grammar.Token`, `animateIn Grammar.Token` reproduce the existing functions **byte-for-byte**; existing function bodies are unedited (FR-010/SC-006). |
| C-G7 | `animateIn` is **deterministic** for all grammars; non-agnostic motions degrade to the static base on Badge/Ring, never throw (FR-014). |
| C-G8 | The `Legibility` linter is **unchanged** and returns the **same** `Report` for a roster regardless of grammar (FR-009/SC-005). |
| C-G9 | Surface baseline gains only `Grammar` + `Grammar+Tags`; **zero drift** on every other baseline (FR-011). |

## Surface-baseline delta (expected)

```diff
+ FS.GG.UI.Symbology.Grammar
+ FS.GG.UI.Symbology.Grammar+Tags
```

(Module `val` additions do not add type-level baseline lines; only the new `Grammar` DU does.)

## FSI smoke (authored before `.fs` body — quickstart references this)

```fsharp
open FS.GG.UI.Symbology
let t = Symbology.defaultToken
Symbology.badge t |> ignore                 // non-empty Scene, no throw
Symbology.ring  t |> ignore
Symbology.render Grammar.Badge t |> ignore
Symbology.render Grammar.Ring  t |> ignore
Symbology.badge { t with R = 0.0 } |> ignore // placeholder, no throw
```
