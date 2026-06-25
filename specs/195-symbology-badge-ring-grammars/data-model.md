# Phase 1 Data Model: Badge & Ring Alternative Symbology Grammars

Namespace `FS.GG.UI.Symbology`, package `src/Symbology/` (pure, Scene-only). This feature adds **one new type**
(`Grammar`) and **new functions** to the existing `module Symbology`; it changes **no** existing type and edits
**no** existing function body. The `Token` channel set and the `Legibility` linter are unchanged.

---

## 1. New type

### `Grammar` — the selectable symbol form factor (FR-001/FR-002)

```fsharp
/// The selectable symbol form factor. All three consume the SAME fixed Token channel set (FR-002);
/// the choice changes only the DRAWING, never the per-game ChannelMap (Key Entity "Grammar selection").
[<RequireQualifiedAccess>]
type Grammar =
    | Token   // existing Directional Token: heading-rotated silhouette
    | Badge   // screen-aligned framed emblem / insignia
    | Ring    // centred radial gauge
```

- `[<RequireQualifiedAccess>]` so `Grammar.Token` never collides with the existing `Token` *record* in the same
  namespace. Adds exactly two surface-baseline lines: `FS.GG.UI.Symbology.Grammar`,
  `FS.GG.UI.Symbology.Grammar+Tags` (FR-011).

## 2. Unchanged types (shared input — context only)

`Token`, `Faction`, `Klass`, `Sigil`, `TokenState`, `Motion` are **unchanged** (Assumptions "One channel
vocabulary"; no new field, no §4 capacity-table change). They are the shared input to all three grammars. See
`src/Symbology/Symbology.fsi`.

## 3. Per-channel siting (the testable defaults; geometry is design-loop, not contract)

The **contract** is FR-003 (every channel observably alters output) + FR-004 (determinism) + FR-006 (screen-
aligned) + FR-007 (Ring health monotone). The exact primitives below are the v1 design-loop defaults.

### Badge (screen-aligned framed emblem)

| `Token` channel | Badge primitive | Observable variation asserted (SC-002) |
|---|---|---|
| `Faction` | frame stroke hue (palette; `Custom` honoured) | hue changes per faction incl. distinct `Custom` |
| `Threat` | frame stroke width | width grows with threat |
| `State` | frame solid vs dashed | dash appears for `Suspected` |
| `Charge` | interior radial-gradient alpha | inner alpha grows with charge |
| `Health` | bottom health bar length + green→red hue | bar length/colour tracks health |
| `Speed` | pip row (0..4) | pip count tracks speed |
| `Shield` | corner mount dot | present iff `Shield` |
| `Sigil` | centre vector sigil | shape differs per sigil |
| `Klass` | class-driven frame outline/corner profile | profile differs per class |
| `Heading` | discrete edge pip at heading direction (no body rotation, FR-006) | pip position tracks heading |

### Ring (centred radial gauge)

| `Token` channel | Ring primitive | Observable variation asserted (SC-002) |
|---|---|---|
| `Faction` | outer-ring hue | hue changes per faction incl. `Custom` |
| `Threat` | ring thickness | thickness grows with threat |
| `State` | ring solid vs dashed | dash appears for `Suspected` |
| `Charge` | radial interior gradient alpha | inner alpha grows with charge |
| `Health` | **arc sweep** (monotone↑) + green→red hue | **sweep grows monotonically with health (FR-007)** |
| `Speed` | rim beads (0..4) | bead count tracks speed |
| `Shield` | ring mount dot | present iff `Shield` |
| `Sigil` | centre vector sigil | shape differs per sigil |
| `Klass` | class-driven inner glyph | glyph differs per class |
| `Heading` | heading needle from centre (no body rotation, FR-006) | needle angle tracks heading |

## 4. New / extended public surface (added to `module Symbology`)

```fsharp
// NEW grammars (FR-001) — pure Token -> Scene; R<=0 -> visible placeholder (FR-005); never throws.
val badge: token: Token -> Scene
val ring:  token: Token -> Scene

// NEW dispatch by selected grammar (FR-002/FR-008). render Grammar.Token ≡ existing `token` output.
val render: grammar: Grammar -> token: Token -> Scene

// NEW grammar-parameterized review boards (FR-008). The *In* siblings select the grammar; the existing
// `gallery`/`filmstrip` remain byte-identical as the Grammar.Token path (FR-010).
val galleryIn:   grammar: Grammar -> cols: int -> spacing: float -> tokens: Token list -> Scene
val filmstripIn: grammar: Grammar -> samples: int -> entries: (Motion * Token) list -> Scene

// NEW grammar-aware motion (FR-014). Applies only grammar-agnostic centre/radius overlays on Badge/Ring;
// directional motions degrade to the static base symbol there. Deterministic. Token path == existing `animate`.
val animateIn: grammar: Grammar -> motion: Motion -> token: Token -> phase: float -> Scene
```

### Unchanged (existing) surface — exact signatures, **byte-identical** behaviour (FR-010)

```fsharp
val defaultToken: Token
val token:     token: Token -> Scene
val animate:   motion: Motion -> token: Token -> phase: float -> Scene
val gallery:   cols: int -> spacing: float -> tokens: Token list -> Scene
val filmstrip: samples: int -> entries: (Motion * Token) list -> Scene
```

## 5. Function semantics (contract assertions)

| Function | Semantics | Verified by |
|---|---|---|
| `badge t` / `ring t` | pure `Token -> Scene`; renders **all** channels (§3); `R<=0` → `placeholder t`; never throws | ChannelPresence, Placeholder, Determinism tests |
| `render g t` | `Grammar.Token`→`token t`; `Grammar.Badge`→`badge t`; `Grammar.Ring`→`ring t` | render-dispatch test |
| `galleryIn g …` / `filmstripIn g …` | per-grammar reproducible board; `Grammar.Token` byte-identical to `gallery`/`filmstrip`; empty/single roster OK | Gallery/Filmstrip tests |
| `animateIn g m t ph` | grammar-agnostic overlay on Badge/Ring (Pulse/Blink/Damage); directional motions → static base; `Grammar.Token` byte-identical to `animate`; deterministic | Motion/Filmstrip tests |
| Ring `Health` | `sweep = maxSweep * clamp01 Health`, **monotone non-decreasing** in `Health` | Ring health-monotonicity test (FR-007) |
| All grammars | identical `Token` ⇒ identical `Scene` ⇒ identical canonical bytes (in-proc & cross-proc) | Determinism tests (canonical bytes) |
| Linter | `Legibility.score`/`scoreAnimated` unchanged; same `Report` for a roster across all grammars | linter grammar-independence test (SC-005) |

## 6. Invariants

- **One vocabulary**: no new `Token` field, no new channel, no §4 capacity change (Assumptions).
- **Purity**: no wall-clock, randomness, or IO in any new function (FR-004).
- **Screen-aligned**: Badge/Ring do not rigidly rotate the body with heading; heading is a discrete indicator
  (FR-006).
- **No channel dropped**: every channel renders observably in every grammar (FR-003/SC-002).
- **Token zero-drift**: existing function bodies unedited; canonical bytes pinned (FR-010/SC-006).
- **Surface drift**: only `FS.GG.UI.Symbology.txt` moves (gains `Grammar`/`Grammar+Tags`); every other baseline
  unchanged (FR-011).
