# Phase 1 Data Model — Agent-Driven Unit-Symbology Design System

**Feature**: `192-agent-unit-symbology` | Derived from spec.md "Key Entities" + source report §4–§5.

These are *value* shapes — pure data in the `FS.GG.UI.Symbology` library, plus the agent-loop artifacts
written by the render bridge and the loop protocol. No mutable runtime state lives in the library; the
only stateful artifacts are filesystem provenance (boards, snapshots) produced at the edge.

---

## 1. `Token` — symbol description (the props record)

The unit over which `token` is pure and over which determinism/fingerprint are asserted (FR-002, spec
"Symbol description"). Every field is a typed channel input.

| Field | Type | Channel (FR-004) | Notes / validation |
|---|---|---|---|
| `Cx`, `Cy` | `float` | centre placement | board coordinates |
| `R` | `float` | size / scale | `R <= 0` (or no drawable area) ⇒ visible **placeholder**, not a crash (FR-020) |
| `Heading` | `float` | whole-body rotation (facing) | radians; rotates body+sigil+tail rigidly; gauges stay screen-aligned |
| `Faction` | `Faction` | stroke **hue** | from the saturated faction palette — never the state palette (FR-019) |
| `Klass` | `Klass` | body silhouette | picks a fixed silhouette from the table (FR-005) |
| `Sigil` | `Sigil` | centre identity mark | vector sigil only — no label text (FR-022) |
| `State` | `TokenState` | stroke **dash** | inspection state: `Confirmed` = solid, `Suspected` = dashed (never sole carrier of critical state) |
| `Threat` | `float` | stroke **width** | normalised `0..1`; ~4 reliable ordered levels |
| `Charge` | `float` | interior **gradient** (charge/energy) | normalised `0..1`; ~4 reliable ordered levels; drives `Shader.RadialGradient` intensity |
| `Speed` | `int` | tail **bead count** | `0..4`; clamp out-of-range to the reliable band |
| `Health` | `float` | belly **arc** length + hue | `0..1`; green→red lerp; arc stays screen-aligned under rotation |
| `Shield` | `bool` | corner **mount** | boolean mount flag (one corner slot in v1; inspection channel) |

**Invariant (FR-003)**: `token` is a total, pure function `Token -> Scene`; equal `Token` values produce
equal `Scene` values and therefore equal `SceneCodec.export … |> _.CanonicalBytes`. No wall-clock, no IO.

**`defaultToken`**: a fully-populated baseline `Token` (centre, unit radius, neutral faction, mobile class,
a default sigil, `Confirmed`, mid threat/charge/health, zero speed, no shield) so a `ChannelMap` overrides only
the fields a given game encodes (`{ defaultToken with Faction = …; Threat = … }`).

## 2. Channel enums — the fixed vocabulary (FR-005)

A game theme *picks* from these; it does not invent geometry.

- **`Faction`** — categorical affiliation → stroke hue (saturated palette). `Ally | Enemy | Neutral | Custom of Color` (≈7 reliable categories).
- **`Klass`** — class → silhouette. `Mobile | Heavy | Scout` for v1 (≈6 reliable silhouettes max; Token grammar uses these three).
- **`Sigil`** — identity mark → centre vector glyph. Fixed marks plus an escape hatch: `Bolt | Ring | Fang | Mark of PathSpec`.
- **`TokenState`** — `Confirmed | Suspected` → solid vs dashed stroke.
- **`Motion`** — activity/alert rhythm (see §4): `Idle | Pulse | Spin | Blink | Damage | Moving`.

## 3. Encoding-channel grammar — the fixed system (entity: "Encoding channel grammar")

The stable mapping of *visual channel → primitive → reliable level count → salience* (source report §4
table). It is **not** a runtime value; it is the contract the `token` body implements and the skill
teaches. Captured here so tasks and goldens can assert "each channel observably alters output" (SC-002):

| Channel | Primitive | Reliable levels | Salience |
|---|---|---|---|
| Stroke hue → faction | `Paint.stroke` colour | ~7 categorical | ★★★ |
| Motion rhythm → activity | overlay over phase | ~4 rhythms | ★★★ |
| Size → magnitude | symbol radius `R` | ~4 ordered | ★★★ |
| Silhouette + sigil → class + identity | `Path.create` + centre mark | ~6 + many | ★★ |
| Rotation → heading | point transform | continuous | ★★ |
| Stroke width → threat | `Paint.stroke` width | ~4 ordered | ★★ |
| Interior gradient → charge/energy | `Shader.RadialGradient` | ~4 ordered | ★★ |
| Belly arc → health | `Scene.arc` + colour lerp | continuous | ★ |
| Tail beads → speed | `Scene.circle` run | ~4 | ★ |
| Stroke dash → confirmed/suspected | `PathEffect.Dash` | ~3 | ☆ inspection |
| Corner mount → boolean mount flag | small `Path`/`Circle` | ~3 per slot | ☆ inspection |

## 4. `Motion` rhythm — pure animation overlay

A named overlay `animate : Motion -> Token -> phase:float -> Scene` (FR-007). **Phase is caller-owned**
(`0..1`), no wall-clock read. Identical `(motion, token, phase)` ⇒ identical `Scene` (FR-009). Rhythms
(minimum set): `Idle` (base), `Pulse` (fired ring), `Spin` (channeling), `Blink` (alert),
`Damage` (scale+hue throb), `Moving` (translate + echo trail). Guardrail: one *active* motion per symbol at
a time (legibility budget).

## 5. Review boards — composed drawings (entity: "Review board")

- **`gallery : cols:int -> spacing:float -> Token list -> Scene`** — a reproducible grid of symbols for
  at-a-glance eyeballing; layout is a pure function of inputs (FR-008).
- **`filmstrip : samples:int -> (Motion * Token) list -> Scene`** — a motion sampled across `samples` phase
  steps from a deterministic schedule; **byte-reproducible** from the schedule alone (FR-009, SC-006).

Both are pure `… -> Scene` and carry a stable `SceneCodec` package identity ⇒ goldenable (SC-001).

## 6. Render evidence — produced by the render bridge (entity: "Render evidence")

Returned/derived from `ReferenceRendering.run` per `Render.toPng` call:

| Field | Source | Use |
|---|---|---|
| image path | `ReferenceRenderingEvidence.ImagePath` (`Some` required) | the PNG the agent reads back; `None` ⇒ fail-loud |
| verdict | `.Verdict` (`ReferencePassed` required) | any other value ⇒ raise with diagnostics (FR-012) |
| image identity | `.ImageIdentity` | content-addressable regression identity (FR-013) |
| diagnostics | `.Diagnostics` | the message body of the fail-loud error |

`Render.toPng` collapses this to **`string` (the image path) on success** and **raises** otherwise — the
public helper hides the evidence record but never hides a failure.

## 7. Channel map — the per-game mapping (entity: "Channel map", the editable artifact)

A *data* function `'stats -> Token` authored **per game**, living in the loop's working files / final
emitted module — **not** in the library (FR-006, D3). It is the artifact the agent tweaks each iteration
(grammar stays fixed). Legibility rules constrain it (FR-016): assign-by-urgency, redundancy on critical
state, no faction/state hue collision (FR-019). Not a library type — it is product/loop code shaped by the
skill's recipe.

## 8. Design provenance — the audit trail (entity: "Design provenance")

Filesystem artifacts written under a working directory each loop iteration and on approval (FR-017,
FR-018, D7):

- **per iteration**: a *timestamped board image* (the rendered gallery PNG) + a *mapping snapshot* (the
  `ChannelMap`/`Token` set that produced it) — forming an auditable history.
- **on approval**: a *final symbol-set module* (pure drawing-producing functions), a *design rationale*
  (channel assignments + rejected alternatives + legibility notes), and a *pinned golden board* with a
  stable `SceneCodec` identity.

Timestamps are supplied to the loop as inputs (the library/render code reads no clock); the *workflow*
stamps provenance filenames, preserving in-library determinism.

---

## Relationships

```
ChannelMap('stats -> Token)        Motion ─┐
        │ produces                          │ animate(motion,token,phase)
        ▼                                    ▼
      Token ──token──► Scene ◄── gallery/filmstrip compose ── [Token]/[Motion*Token]
                         │
              SceneCodec.export (CanonicalBytes = determinism identity)
                         │
                Render.toPng ──► ReferenceRendering.run ──► Render evidence (path|raise)
                         │
                 Design provenance (timestamped board + mapping snapshot ; final module + golden)
```
