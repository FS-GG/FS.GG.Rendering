# Phase 0 Research: Symbology Auto-Label & Label-Bound Motion

All open questions from the spec's Assumptions ("the exact field shapes … are implementation / design-loop details resolved at planning") resolved below. Each entry: **Decision / Rationale / Alternatives considered**. No `NEEDS CLARIFICATION` remain.

Grounded against the post-199 tree (2026-06-26): `src/Symbology/Symbology.fs` (`labelDispatch`, `tokenLabelNodes`/`badgeLabelNodes`/`ringLabelNodes`, `drawSymbol`/`drawBadge`/`drawRing`, `animate`/`filmstrip`/`animateIn`/`filmstripIn`, `placeholder`, `defaultToken`), `src/Symbology/Symbology.fsi` (the `Token`/`LabelText`/`LabelRun`/`Motion` surface), `src/Symbology/Legibility.fs` (no `Token.Label` reference), `src/Symbology.Render/` (the `Render.toPng` bridge + real measurer).

---

## D1 — How a `Token` opts into auto-derivation, and how explicit overrides it

**Decision.** Add **two `None`-defaulted optional fields to `Token`**:
- `AutoLabel: AutoLabelSpec option` — the opt-in projection request.
- keep the existing `Label: LabelText option` — the explicit author-supplied label.

A pure `resolveLabel : Token -> LabelText option` collapses them with **explicit-wins** semantics:
`t.Label |> Option.orElseWith (fun () -> t.AutoLabel |> Option.bind (projectAutoLabel t))`.
The three per-grammar label helpers feed `resolveLabel t` (not `t.Label`) into the unchanged `labelDispatch`.

**Rationale.** FR-003 requires that a `Token` can **both** opt into auto-derivation **and** carry an explicit label, with the explicit one winning. That demands the projection request and the explicit label **coexist** on the `Token` — a single `LabelText` slot cannot hold both. Two sibling fields model it directly; `resolveLabel` guarantees **exactly one resolved label (or none)**, never two stacked. The resolved value flows through the **same single** `labelDispatch`, so there is one shared label channel and no second per-grammar mapping (FR-001).

**Alternatives considered.**
- *A new `LabelText.Auto of AutoLabelSpec` case.* Rejected: it occupies the single `Label` slot, so a `Token` cannot carry both an auto request and an explicit override — FR-003 becomes inexpressible. It would also force every match on `LabelText` to handle a case that resolves to *another* `LabelText`, muddying the dispatch.
- *Auto-label as a separate render channel.* Rejected by FR-001 ("no second label channel") and FR-009 (it must site in the **same** per-grammar region) — a projection that produces a `LabelText` consumed by the existing dispatch is not a second channel.
- *A bool `AutoLabel: bool` with a fixed projection.* Rejected: gives the designer no control over which channels appear or the compact form, and the spec calls for a "projection descriptor."

---

## D2 — What the projection reads and the compact form it emits

**Decision.** `AutoLabelSpec = { Fields: AutoField list; Separator: string }`, where

```fsharp
type AutoField =
    | FactionCode   // Ally -> "ALY" | Enemy -> "ENY" | Neutral -> "NEU" | Custom _ -> "CUS"
    | KlassCode     // Mobile -> "MOB" | Heavy -> "HVY" | Scout -> "SCT"
    | StateCode     // Confirmed -> "CFM" | Suspected -> "SUS"
    | HealthTier    // round(Health*100) -> "H87"   (Health is the encoded [0,1] channel)
    | ThreatTier    // bucket Threat [0,1] into T0..T4 -> "T3"
    | SpeedPips     // Speed (0..4) -> "S2"
    | ShieldFlag    // Shield=true -> "SHD"; Shield=false -> contributes nothing
```

`projectAutoLabel : Token -> AutoLabelSpec -> LabelText option` maps each requested `AutoField` to its short token by reading **only** the corresponding `Token` channel, drops fields that contribute nothing (e.g. `ShieldFlag` when `Shield=false`), joins the remainder with `Separator`, and returns `Some (LabelText.Plain joined)` — or `None` when the result is empty/all-whitespace.

**Rationale.** Every selector reads a channel the `Token` already encodes (`Faction`/`Klass`/`State`/`Health`/`Threat`/`Speed`/`Shield` — confirmed present on the `Token` record) — **never** a game's raw stats, so the per-game `'stats -> Token` mapping stays the caller's (FR-002, FR-021). Codes are fixed, game-agnostic, compact (3-ish chars) so the readout stays legible in the tight per-grammar regions (Ring is only `1.05R` wide). The output is the existing `LabelText`, so it rides the proven 196–199 fit/wrap/cap/decoration machinery unchanged (FR-011). Emitting `Plain` (not `Rich`) keeps the projection's default styling identical to a hand-authored plain label; a designer who wants per-field styling can still hand-author (explicit wins) — keeping the projection simple (Principle III).

**Determinism.** The projection is a pure fold over the selected channels — no wall-clock, randomness, IO, or global state — so identical channels ⇒ identical text ⇒ identical bytes (FR-004/FR-015). Two `Token`s differing in one projected channel yield observably different labels; identical channels yield byte-identical labels (US1 Independent Test §3/§4).

**Degenerate projection.** `Fields = []`, or a projection whose joined text is empty/all-whitespace, returns `None` ⇒ treated as **no label** (no text node, no exception), exactly like an empty hand-authored label (FR-004/FR-012).

**Alternatives considered.**
- *Project from `Cx/Cy/R/Heading`.* Rejected: those are geometry/placement, not identity channels — a coordinate readout is not a useful label and risks impersonating nothing meaningful.
- *Free-form format string.* Rejected as over-scoped: a mini templating language is a new subsystem (against "bounded completion") and invites per-game semantics creeping into the library. A fixed selector list keeps the projection game-agnostic and the per-game meaning the caller's.
- *Emit `Rich` with a styled code per field.* Deferred to the design loop: the contract is the channel-only projection; per-field styling is an enhancement a designer gets by hand-authoring (explicit override) and is not needed to satisfy any FR.

---

## D3 — How the label binds to motion, and the motion vocabulary

**Decision.** Add `LabelMotion: LabelMotion option` to `Token` with a restrained DU:

```fsharp
type LabelMotion =
    | TypeOn   // progressive whole-glyph prefix reveal driven by phase
    | Fade     // run paint alpha ramps with phase
    | Pulse    // size/alpha oscillation (sin) about the rest value
    | Scroll   // overflow ticker: an overlong line offsets horizontally within the region
```

The binding sits on the `Token` (not on `LabelText`/`LabelRun`) and animates the **resolved** label as a whole.

**Rationale.** Motion applies to the resolved label (whichever of explicit/projected won), so a single `Token`-level field is the natural home and avoids touching `LabelText`/`LabelRun` (which would risk per-run golden drift and conflict with the 199 run attributes). The four kinds are the spec's named, restrained set (type-on / fade / pulse / overflow-scroll — FR-005). Each is realisable from primitives the renderer **already** supports (FR-019): prefix-of-`glyphRunProof` for type-on, `Paint` alpha for fade, `withPerspective`/size scale for pulse (reusing 199's slant primitive), an X-offset within the fitted region for scroll — **no new scene primitive, no new font, no GPU path**.

**Alternatives considered.**
- *A `Motion`-typed binding reusing the existing symbol `Motion` DU (`Pulse|Spin|Blink|Damage|Moving`).* Rejected: those are **whole-symbol** rhythms with established geometry; the label needs **text-specific** kinds (type-on, scroll) the symbol DU lacks, and overloading it would couple label motion to symbol motion. A separate, small DU keeps the two timelines' *kinds* independent while sharing the *phase clock*.
- *A field on `LabelText`/each `LabelParagraph`.* Rejected: motion is a property of the whole resolved label, not a paragraph; a `Token`-level field is simpler and keeps `LabelText` byte-stable.

---

## D4 — Threading the motion phase without a signature change (the crux)

**Decision.** The label-bearing internal drawers gain a `labelPhase: float` parameter **defaulting to `restPhase = 0.0`**:
`tokenLabelNodes`/`badgeLabelNodes`/`ringLabelNodes` (and their `drawSymbol`/`drawBadge`/`drawRing` callers) take `labelPhase` and pass it into a new `motionLabelNodes`. The **static** entry points (`token`/`badge`/`ring`/`render`/`gallery`/`galleryIn`) call the drawers with `restPhase`; the **motion** entry points (`animate`/`animateIn`/`filmstrip`/`filmstripIn`) pass the **same normalised `ph = phase - floor phase`** they already compute for the overlay into the base-symbol label draw. No public signature changes.

**Rationale.** The label is drawn inside the *static* base symbol (`token t`), while the phase only exists inside `animate`/`filmstrip` (which today overlay motion *on top of* the static base symbol). To animate the label, the phase must reach the label draw. An **internal** default parameter does this with zero public surface change: the static call sites pass rest, the motion call sites pass the live phase. This honours FR-005/FR-017 (no entry-point signature change) and reuses the existing phase normalisation convention (`animate` already does `ph = phase - floor phase`) so label motion and symbol overlay sample the **same** clock (FR-006). `restPhase = 0.0` is the existing identity frame (`animate _ _ 0.0` / filmstrip's first sample).

**Implementation note.** `animate` currently computes `baseSymbol = token t` then appends the overlay. The edit recomputes the base symbol's *label* at `ph` (the rest of the base symbol is phase-independent), so the overlay continues to apply unchanged to the symbol while the label animates. `filmstrip`/`filmstripIn` already iterate samples `s` → `phase`; they pass that `phase` into the same path.

**Alternatives considered.**
- *Add a `phase` parameter to the public board/motion entry points.* Rejected outright by FR-005 ("no signature change to the existing board/motion entry points").
- *A separate public `animateLabel` entry point.* Rejected: a new entry point is exactly what FR-005/FR-017 forbid and what "no new subsystem" rules out.
- *Carry the phase on the `Token`.* Rejected: the phase is a board-owned sample, not unit state; putting it on `Token` would make the same unit render differently per phase via a *data* field, breaking the "equal Token ⇒ equal Scene" purity contract (it would have to change per frame).

---

## D5 — Rest phase is the identity transform (rest-is-full reveal)

**Decision.** Every `LabelMotion` kind's value at `restPhase = 0.0` is the **identity** — the transform that reproduces the static `labelDispatch` output exactly:
- `TypeOn` at rest reveals the **full** label (rest = fully typed-on); non-rest phases reveal a growing prefix that reaches full at the cycle end.
- `Fade` at rest = full alpha (the static paint).
- `Pulse` at rest = scale `1.0`, full alpha (`sin(0) = 0` ⇒ no deviation).
- `Scroll` at rest = offset `0.0` (line at its fitted start).

`motionLabelNodes` therefore emits the **exact** `labelDispatch` node list at `restPhase`, giving byte-identity with the static spec-199 label (FR-007).

**Rationale.** FR-007 mandates rest = static byte-for-byte. The cleanest way to guarantee it structurally is to define each kind so its `ph = 0.0` value is the no-op transform, and to route `LabelMotion = None` *and* rest-phase both to the plain `labelDispatch`. Choosing **rest = fully revealed** for `TypeOn` (rather than rest = empty) is what makes the rest frame equal the static label; the animation then plays as a *prefix grows from a non-rest phase*. (If a designer wants a literal "type from empty" they sample the phase forward; the rest frame remains the complete, legible label — which is also the desirable default for a still board.)

**Alternatives considered.**
- *Rest = empty for `TypeOn`.* Rejected: it would make the rest frame an empty/partial label, violating FR-007 (rest must equal the full static label) and making a still gallery show truncated labels.
- *A separate "identity phase" constant ≠ 0.0.* Rejected: `0.0` is already the existing rest frame for `animate`/`filmstrip`; introducing a different identity phase would desync label motion from the symbol overlay.

---

## D6 — Fit at every phase

**Decision.** The animated label is fitted by the **same** real per-run measurement / wrap / cap / truncate / placeholder rules as a static 199 label, evaluated so that **no phase** clips mid-glyph or overflows into adjacent channels:
- **TypeOn** reveals **whole glyphs** (prefix boundaries on glyph edges via measured advances), never a partial glyph; a prefix of a fitted line is always ≤ the fitted line, so it always fits.
- **Pulse** scales by a factor **capped** so the scaled label still fits the region (the cap is derived from the fitted extent vs region width); the pulse never grows the label past the region.
- **Scroll** offsets an **overlong** line within the region and is **clipped to the region extent** (the same fitted-extent bound the static path uses), so the ticker scrolls *within* the region, never beyond it; the drawn line count stays capped to the per-grammar budget.
- **Fade** changes only alpha — geometry (hence fit) is unchanged.

**Rationale.** FR-011/SC-005 require the label to stay fitted at **every** sampled phase — a typed-on/scrolling/pulsing label that overflows mid-animation is worse than a static one (US2 Why-this-priority). Because fit is computed on the resolved label first and motion is a *bounded* transform of the already-fitted geometry (prefix ≤ whole; scale ≤ cap; offset within extent), the static fit guarantees carry to every phase.

**Alternatives considered.**
- *Re-fit per phase from scratch.* Unnecessary: type-on/fade/scroll do not change the resolved text's measured widths; re-using the static fit is deterministic and cheaper. Pulse is the only kind that scales geometry, handled by the fit-preserving cap.

---

## D7 — Zero-drift dispatch arrangement

**Decision.** Keep `labelDispatch` and the per-grammar budgets (Token ≤ 3, Badge ≤ 2, Ring ≤ 2) **unchanged**. Route as:
- `LabelMotion = None` **or** `labelPhase = restPhase` → call `labelDispatch (resolveLabel t)` directly (no transform).
- `LabelMotion = Some kind` **and** non-rest phase → `motionLabelNodes kind ph (labelDispatch (resolveLabel t)) …`.
- `resolveLabel t` = `t.Label` when explicit present; else the projected `AutoLabel`; else `None`.

A `Token` opting into neither capability has `resolveLabel t = t.Label`, `LabelMotion = None`, `labelPhase = restPhase`, hitting the **exact** spec-199 dispatch ⇒ byte-identical (FR-008).

**Rationale.** Mirrors the 198/199 pattern of making unused paths *structurally* the prior path. The new behaviour only diverges when a new field is set **and** (for motion) a non-rest phase is sampled, so the layered goldens (no-label / Plain / Rich / Laid) stay green by construction.

---

## D8 — Render-edge tofu path (unchanged seam, new coverage)

**Decision.** Reuse the `Symbology.Render` bridge and the real measurer it installs **as-is**. Add a render-bridge test that rasterises an **auto-derived + motion-bound** labelled token through `Render.toPng` at sampled phases and asserts every resolved run is non-tofu (`Missing = false` / `TofuCount = 0`) and the board is non-blank.

**Rationale.** Tofu-free *rendering* and *measured fit* are render-edge properties (FR-010/FR-016); the pure library never installs/requires a measurer and never throws without one (it still emits the resolved label's styled nodes + recorded phase on the pure-fallback path). Motion is a transform of `glyphRunProof` nodes that **preserves** the per-glyph `Missing` flags (prefix/alpha/scale/offset don't change glyphs), so tofu-freeness holds across phases — verified by a **real** raster, not assumed.

---

## D9 — Test-battery shape

**Decision.** Additive batteries (no existing assertion weakened):
- **AutoLabelTests.fs (new)** — projection reads channels; differing projected channel ⇒ differing label; identical channels ⇒ byte-identical; explicit overrides auto (exactly one resolved label); empty/whitespace/degenerate projection ⇒ no label + no throw; opt-out ≡ 199 byte-identity.
- **LabelMotionTests.fs (new)** — each kind advances with phase; rest phase ≡ static 199 byte-identity; scroll over overlong content stays within region + capped lines; no-motion ≡ 199 across the timeline; deterministic per-phase (same phase ⇒ same bytes, in-/cross-process); auto + motion composition deterministic/fitted.
- **ChannelPresence/Determinism/Placeholder/Gallery/Legibility/RichLabel** — extended with the auto/motion deltas (projected-channel observability, a pinned auto/motion cross-process golden, degenerate auto/motion → placeholder, roster reproducible per grammar + filmstrip frames, linter `Report` invariance, opt-out byte-identity with the two new fields present-as-None).
- **Symbology.Render.Tests/RenderLabelTests.fs** — the D8 tofu raster at sampled phases.

**Rationale.** Matches the 198/199 battery layout, satisfies every SC, and keeps the existing 196–199 goldens authoritative for zero-drift.

---

### Summary of resolved unknowns

| # | Unknown (from spec Assumptions) | Resolution |
|---|---|---|
| D1 | How a `Token` opts into auto + how explicit overrides | Two `Token` fields (`AutoLabel`, existing `Label`); `resolveLabel` = explicit orElse projected |
| D2 | What channels the projection reads / compact form | `AutoLabelSpec { Fields: AutoField list; Separator }`; fixed game-agnostic codes from `Faction`/`Klass`/`State`/`Health`/`Threat`/`Speed`/`Shield`; emit `Plain` |
| D3 | How motion binds + the vocabulary | `Token.LabelMotion: LabelMotion option`; DU `TypeOn|Fade|Pulse|Scroll` |
| D4 | Threading phase without signature change | internal `labelPhase` (default `restPhase=0.0`); static entry points pass rest, motion entry points pass the existing `ph` |
| D5 | Rest = static | every kind's `ph=0.0` value is the identity (type-on = fully revealed) |
| D6 | Fit at every phase | bounded transforms of the static fit (prefix ≤ whole, scale ≤ cap, offset within extent) |
| D7 | Zero-drift dispatch | `labelDispatch`/budgets unchanged; unused/rest paths route structurally to 199 |
| D8 | Render-edge tofu | reuse `Symbology.Render`; raster auto+motion at phases; assert non-tofu |
| D9 | Test shape | additive AutoLabel/LabelMotion batteries + extended existing + render tofu + new golden |
