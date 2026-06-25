# Contract: Symbology Identity-Label Public Surface

This is the public-surface (Tier 1) contract for the label/glyph-text channel. The only public change is **one optional field** on the existing `Token` record in `FS.GG.UI.Symbology`. No new public type, no new `val`, no signature change to any existing function.

## `.fsi` sketch (the contract)

Edit in `src/Symbology/Symbology.fsi` — the `Token` record gains a final optional field; everything else is unchanged:

```fsharp
namespace FS.GG.UI.Symbology

open FS.GG.UI.Scene

// Faction / Klass / Sigil / TokenState / Motion / Grammar — UNCHANGED

/// The symbol description: the full fixed channel set as typed fields (FR-002).
/// Pure over this value: equal Token => equal Scene => equal SceneCodec canonical bytes
/// (under a fixed text-measurement provider; FR-008).
type Token =
    { Cx: float
      Cy: float
      R: float
      Heading: float
      Faction: Faction
      Klass: Klass
      Sigil: Sigil
      State: TokenState
      Threat: float
      Charge: float
      Speed: int
      Health: float
      Shield: bool
      /// Optional short identity string (name / callsign / code). `None` = no label (default) and
      /// renders byte-identically to the pre-feature symbol (FR-002). An empty/whitespace `Some`
      /// is treated as no label (FR-006). When present it is drawn screen-aligned in the grammar's
      /// label region, fitted to that region via real text measurement (FR-005), and tofu-free when
      /// rendered through the headless render bridge's real measurer (FR-004). Inspection-detail:
      /// it does NOT enter the legibility capacity table (FR-011).
      Label: string option }

[<RequireQualifiedAccess>]
module Symbology =

    /// Fully-populated baseline; `Label = None` so existing tokens render byte-identically (FR-002).
    val defaultToken: Token

    // token / animate / gallery / filmstrip / badge / ring / render / galleryIn / filmstripIn /
    // animateIn — ALL UNCHANGED. They already thread the whole Token, so they carry the label
    // through to each grammar with no signature change.
```

> The fit/siting logic (`fitLabel`, per-grammar `labelNode`) is **internal** — omitted from the `.fsi`, no access modifiers in `.fs` (constitution II). It is verified through the public `token`/`badge`/`ring`/`render` surface, never called directly by tests.

## Behavioural contract

| ID | Requirement | Observable guarantee |
|---|---|---|
| **C-01** | One mapping, all grammars (FR-001) | A `Token` carrying `Some label` renders the label in `token`, `badge`, `ring`, and `render g` for every `g` — no per-grammar mapping. |
| **C-02** | Opt-in, zero drift (FR-002/SC-003) | `Label = None` ⇒ the emitted `Scene` and its `SceneCodec.export(...).CanonicalBytes` are byte-identical to the pre-feature symbol for the same channels. The `token`/`gallery`/`filmstrip` golden SHAs stay green. |
| **C-03** | Sited, observable, non-overlapping (FR-003) | When present, the label observably alters the canonical bytes and occupies a per-grammar region that does not overlap the sigil, health, or other channels. |
| **C-04** | Tofu-free at the render edge (FR-004) | Rasterised through `Symbology.Render.toPng` under the installed real measurer/shaper, the label's glyph run is non-tofu (`Missing = false` for covered glyphs); any uncovered glyph is *disclosed* as tofu, never drawn as a plausible-wrong glyph. |
| **C-05** | Fitted, no clip/overflow (FR-005/SC-005) | An overlong label is shrunk and/or ellipsis-truncated at a measured glyph boundary so the drawn label stays within the region width, is never cut mid-glyph, and never overflows into an adjacent channel. |
| **C-06** | Empty/whitespace ⇒ no label (FR-006) | A `Some s` with `s.Trim() = ""` emits no label node and raises no exception — equivalent to `None`. |
| **C-07** | Degenerate wins (FR-007) | `R <= 0` with a label ⇒ the existing visible placeholder; the placeholder rule takes precedence over the label; no exception. |
| **C-08** | Provider-relative determinism (FR-008/SC-004) | Under a fixed measurement provider, identical labelled `Token` ⇒ identical scene ⇒ identical canonical bytes, in-process and across processes. No wall-clock, randomness, or IO in the library. |
| **C-09** | Measurer-optional pure library (FR-009) | With no measurer installed, the library still emits a deterministic scene including the label's text node and does not throw; tofu-free rendering is a render-edge property, not a library precondition. |
| **C-10** | Boards reproducible (FR-010) | A labelled roster renders on `galleryIn`/`filmstripIn` reproducibly per grammar under a fixed provider, with no signature change. |
| **C-11** | Linter unchanged (FR-011/SC-006) | `Legibility.score`/`scoreAnimated` return a grammar-independent `Report` that is unchanged by the presence of labels; the label is not added to the capacity table. |
| **C-12** | Existing grammars unchanged (FR-012) | Token/Badge/Ring and their motion/gallery/filmstrip behaviour are byte-unchanged for any `Token` without a label. |
| **C-13** | Surface baseline (FR-013/SC-007) | Only `readiness/surface-baselines/FS.GG.UI.Symbology.txt` moves (gains the `Label` field on `Token`); zero drift on every other baseline. |
| **C-14** | Pure scene-only layer (FR-014/FR-016) | The label adds no rendering/raster/GL/IO dependency and no new font file; it consumes only the already-referenced `FS.GG.UI.Scene` text/measurement vocabulary. |

## Out of scope (FR-016)

Multi-line/paragraph text; rich-text styling (per-run colour/weight beyond a single label style); automatic label generation from stats without a human in the loop; label-bound motion; any new GPU/compute path; bundling or shipping new font files.

## Surface-baseline expectation

Regenerating `readiness/surface-baselines/FS.GG.UI.Symbology.txt` after the change must show **exactly** the addition of the `Label` field on `FS.GG.UI.Symbology.Token` (e.g. `+ ... Token ... Label : Microsoft.FSharp.Core.FSharpOption<System.String>` in the recorded shape), with **zero drift** on every other line and every other baseline file.
