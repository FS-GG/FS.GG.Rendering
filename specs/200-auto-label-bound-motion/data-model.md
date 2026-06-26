# Phase 1 Data Model: Symbology Auto-Label & Label-Bound Motion

The feature adds **two opt-in fields to `Token`** and **three new public types**, all consumed by the existing label-dispatch path. Nothing else on the public surface changes. Internal helpers (omitted from the `.fsi`) perform projection, resolution, and per-phase animation.

## New / extended public types (declared in `Symbology.fsi`)

### `AutoField` (new DU) — projection channel selectors

```fsharp
type AutoField =
    | FactionCode   // Ally->"ALY" | Enemy->"ENY" | Neutral->"NEU" | Custom _ ->"CUS"
    | KlassCode     // Mobile->"MOB" | Heavy->"HVY" | Scout->"SCT"
    | StateCode     // Confirmed->"CFM" | Suspected->"SUS"
    | HealthTier    // round(Health*100) -> "H" + nn   (Health: encoded [0,1] channel)
    | ThreatTier    // bucket Threat [0,1] -> "T0".."T4"
    | SpeedPips     // Speed (0..4) -> "S0".."S4"
    | ShieldFlag    // Shield=true -> "SHD"; false -> contributes nothing
```

Each selector reads **only** the named `Token` channel (FR-002). Codes are fixed, game-agnostic, compact for the tight per-grammar regions.

### `AutoLabelSpec` (new record) — the projection request

```fsharp
type AutoLabelSpec =
    { Fields: AutoField list   // ordered selectors; [] -> projects to nothing -> no label
      Separator: string }      // joins the rendered field tokens (e.g. " " or "·")
```

### `LabelMotion` (new DU) — the label-bound motion kind

```fsharp
type LabelMotion =
    | TypeOn   // whole-glyph prefix reveal driven by phase; rest = fully revealed
    | Fade     // run paint alpha ramps with phase; rest = full alpha
    | Pulse    // size/alpha oscillation (sin·2π) about rest; rest = scale 1.0
    | Scroll   // overflow ticker: overlong line offsets within the region; rest = offset 0
```

### `Token` (existing, extended) — two `None`-defaulted fields

```fsharp
type Token =
    { …                              // every existing field unchanged
      Label: LabelText option        // EXISTING explicit label (196–199) — wins over AutoLabel
      AutoLabel: AutoLabelSpec option // NEW — opt-in channel projection; None = off (default)
      LabelMotion: LabelMotion option } // NEW — opt-in label-bound motion; None = off (default)
```

`Symbology.defaultToken` gains `AutoLabel = None; LabelMotion = None`. A `Token` with both `None` is byte-identical to spec 199 (FR-008).

### Constructors (new `val`s in the `Symbology` module)

```fsharp
val autoLabel:    fields: AutoField list -> AutoLabelSpec          // Separator = " "
val autoLabelSep: separator: string -> fields: AutoField list -> AutoLabelSpec
val labelMotion:  kind: LabelMotion -> LabelMotion                  // identity helper for readable call sites
```

(A `Token` is still built via `defaultToken` + `with`-copy: `{ Symbology.defaultToken with AutoLabel = Some (Symbology.autoLabel [FactionCode; HealthTier]); LabelMotion = Some TypeOn }`.)

## Internal helpers (in `Symbology.fs`, NOT in the `.fsi`)

| Helper | Signature (conceptual) | Role |
|---|---|---|
| `projectAutoLabel` | `Token -> AutoLabelSpec -> LabelText option` | Pure fold over `spec.Fields`, each reading one `Token` channel; join with `spec.Separator`; `Some (LabelText.Plain joined)` or `None` when empty/whitespace (FR-004). |
| `resolveLabel` | `Token -> LabelText option` | `t.Label |> Option.orElseWith (fun () -> t.AutoLabel |> Option.bind (projectAutoLabel t))` — **explicit wins**; exactly one resolved label or none (FR-003). |
| `restPhase` | `float` (`= 0.0`) | The identity / rest phase; static entry points pass it. |
| `motionLabelNodes` | `LabelMotion -> float -> (unit -> Scene list) -> Scene list` | At `restPhase` returns the static node list (identity, FR-007); else applies the bound kind's per-phase transform (prefix reveal / alpha / sin-scale-capped / in-region offset), staying fitted (FR-011). |

The three per-grammar label drawers gain a `labelPhase: float` (default `restPhase`) and compute:
`if t.LabelMotion = None || labelPhase = restPhase then labelDispatch … (resolveLabel t) else motionLabelNodes kind labelPhase (fun () -> labelDispatch … (resolveLabel t))`.

## Resolution order (FR-003)

```
resolved label =
    1. t.Label            (explicit author-supplied) — if Some, wins
    2. else project t.AutoLabel  (channel projection) — if Some and yields drawable glyphs
    3. else None          (no label)
```

There is **always exactly one resolved label or none** — never two stacked.

## Phase threading (FR-005 — no public signature change)

| Entry point | `labelPhase` passed | Effect |
|---|---|---|
| `token` / `badge` / `ring` / `render` | `restPhase` (0.0) | label drawn static ⇒ byte-identical to 199 |
| `gallery` / `galleryIn` | `restPhase` | static board ⇒ byte-identical to 199 |
| `animate` / `animateIn` | the normalised `ph = phase - floor phase` it already computes | label animates at `ph` alongside the existing symbol overlay |
| `filmstrip` / `filmstripIn` | the per-sample `phase` it already computes | each frame's label animates at its sample's phase |

`restPhase` ⇒ identity ⇒ rest frame = static (FR-007). `LabelMotion = None` ⇒ `motionLabelNodes` never runs ⇒ zero drift at every phase (FR-008).

## Per-phase motion transforms (FR-006/FR-011, all rest = identity)

| Kind | Non-rest transform of the resolved/fitted label | Rest (ph=0) | Fit guarantee |
|---|---|---|---|
| `TypeOn` | reveal a measured **prefix** (whole glyphs) sized by `ph` | full label | prefix ≤ fitted line ⇒ always fits; never mid-glyph |
| `Fade` | scale run paint **alpha** by `ph` | full alpha | geometry unchanged |
| `Pulse` | size/alpha factor `1 + k·sin(ph·2π)`, **k capped so scaled ≤ region** | factor 1.0 | capped to region ⇒ never overflows |
| `Scroll` | translate an **overlong** line by an X-offset, **clipped to region extent** | offset 0 | within region; capped line count preserved |

## Per-grammar regions & budgets (existing, reused unchanged)

| Grammar | `baseSize` | `regionWidth` | `baselineY` | line budget |
|---|---|---|---|---|
| Token | `R·0.5` | `R·1.9` | `Cy + R·1.5` | 3 |
| Badge | `R·0.42` | `R·1.7` | `Cy + R·1.42` | 2 |
| Ring | `R·0.34` | `R·1.05` | `Cy + R·0.52` | 2 |

The resolved (projected and/or animated) label sites in the **same** region with the **same** budget as a hand-authored 199 label (FR-009/FR-011). Ring is the tightest — keep projections compact (skill caveat).

## Validation rules (from spec FRs)

- Projection reads **only** `Token` channels, never raw stats (FR-002); per-game mapping stays the caller's (FR-021).
- Explicit label overrides auto; exactly one resolved label (FR-003).
- Empty / all-whitespace / degenerate projection ⇒ no label, no throw, every phase (FR-004/FR-012).
- Motion is a deterministic function of phase; rest = static byte-for-byte (FR-006/FR-007).
- Fitted, capped, no mid-glyph clip, no overflow into adjacent channels, scroll within region — every phase (FR-011).
- Degenerate token (`R ≤ 0`) ⇒ visible placeholder; auto/motion suppressed; never throws (FR-014).
- Pure & deterministic under fixed measurer + fixed phase; no wall-clock/randomness/IO; measurer-optional (FR-015/FR-016).
- Auto + motion compose: project first, then animate (FR-013).
- Linter governance unchanged; label stays inspection-detail; verdict grammar-independent (FR-018).

## Contract vs design-loop split

**Contract (asserted by tests / baselines):** the `.fsi` delta (the three types, two `Token` fields, ctors); `resolveLabel` order (explicit wins); projection determinism & channel-only reading; rest-phase = static byte-identity; per-phase determinism; fit-at-every-phase; opt-out zero-drift; placeholder precedence; linter invariance; surface baseline moves only for symbology.

**Design-loop (tuned by eyeball, not asserted byte-exact):** the exact code strings per `AutoField`, the default `Separator`, the `Pulse` amplitude `k` and `TypeOn`/`Scroll` rates, which `Fields` a given roster uses, and the legibility caveats (keep projections compact, motion restrained, don't impersonate faction/state or crowd the region).
