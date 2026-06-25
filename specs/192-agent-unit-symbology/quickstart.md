# Quickstart — Agent-Driven Unit-Symbology Design System

**Feature**: `192-agent-unit-symbology` | Scope M1–M5. This is a **validation / run guide** — it proves the
feature works end-to-end. Implementation bodies live in `tasks.md` + the source files; full surfaces are in
[contracts/](./contracts/) and shapes in [data-model.md](./data-model.md).

Prereqs: .NET `net10.0` SDK; repo builds (`dotnet build FS.GG.Rendering.slnx`); local pack feed at
`~/.local/share/nuget-local/`. Render validation is CPU-raster (no GL required).

---

## M0 — render-bridge spike (do this FIRST, before any production code)

The early *real-run* check that replaces the "unverified root-cause" standing assumption. Confirms the public
render path works in *this* checkout, independent of the source-report PoC narrative.

- Throwaway ~20-line FSI script from a scratch dir: build a one-token gallery `Scene`, call the §5.3 wrapper
  shape (`SceneCodec.export` → `ReferenceRendering.run`), assert `Verdict = ReferencePassed` and a non-blank
  PNG at `ImagePath`.
- **Expected**: `ReferencePassed`, a content-hash-named PNG + `reference-evidence.md` written. If instead you
  see `ReferenceFailed`/`ReferenceEnvironmentLimited` or `ImagePath = None`, STOP — the bridge assumption is
  wrong for this environment; record it before building M1.

## M1 — pure library `FS.GG.UI.Symbology` (→ O1)

Build & test:

```bash
dotnet build src/Symbology/Symbology.fsproj
dotnet test  tests/Symbology.Tests/Symbology.Tests.fsproj
```

Validate (semantic, through the public surface):

- **Determinism (SC-001)**: render the same `Token` twice ⇒ equal `Scene`; `SceneCodec.export` canonical
  bytes equal across runs.
- **Channel presence (SC-002)**: for each channel in the data-model table, two `Token`s differing in *only*
  that field produce a *visibly different* board (readback at the target size, via `fs-gg-diagnostics`); and
  differ in *only* that channel.
- **Codec fidelity (SC-003)**: export→import→raster of a `token` scene preserves Path / gradients / `Dash` /
  `Arc` / stroke width/cap/join.
- **Placeholder (FR-020)**: a zero/empty-area `Token` renders a visible placeholder, not a blank/crash.
- **Surface gate (SC-004)**: `readiness/surface-baselines/FS.GG.UI.Symbology.txt` exists and matches; existing
  baselines unchanged.

**Done when**: every channel observably alters output; golden + determinism green; baseline pinned.

## M2 — motion + boards (→ O2; overlaps M3/M4)

```bash
dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj
```

- **Per-motion golden**: `animate m token phase` overlays the rhythm on the base symbol; pure in `(m,token,phase)`.
- **Filmstrip reproducibility (SC-006)**: `filmstrip samples entries` rendered twice ⇒ byte-identical frames
  (phase comes from the schedule; no wall-clock read).
- **Gallery reproducibility**: `gallery cols spacing tokens` lays out a stable grid; stable package identity.

**Done when**: deterministic motion overlays + review boards; baseline updated.

## M3 — render bridge `FS.GG.UI.Symbology.Render` (→ O3; starts after M0)

```bash
dotnet build src/Symbology.Render/Symbology.Render.fsproj
dotnet test  tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj
```

- **Pass path (SC-008)**: `Render.toPng size (Symbology.gallery …) dir` returns a path to a non-blank PNG with
  `ReferencePassed`, reaching no internal entry.
- **Fail-loud (FR-012)**: a scene/verdict that does not pass (or `ImagePath = None`) ⇒ `Render.toPng` raises
  carrying the `Diagnostics`, never a blank image.
- **Surface gate (SC-004)**: `FS.GG.UI.Symbology.Render.txt` added; `Controls`/`Canvas`/`Scene`/`SkiaViewer`
  baselines show **zero** drift.

**Done when**: public scriptable Scene→PNG; no core-surface change.

## M4 — `fs-gg-symbology` skill + FSI recipe (→ O4; after M3)

- Author `SKILL.md` once; mirror to `.claude/skills/fs-gg-symbology/`, `.agents/skills/fs-gg-symbology/`,
  `template/product-skills/fs-gg-symbology/`; check in a reference `.fsx` (roster → `ChannelMap` → gallery render).
- Content = the grammar + legibility rules + library/render API + grammar-vs-mapping pattern + the FSI recipe
  + the feedback protocol (see [contracts/agent-loop-protocol.md](./contracts/agent-loop-protocol.md)).

```bash
dotnet fsi scripts/check-agent-skill-parity.fsx \
  --out /tmp/fs-gg-skill-parity --report /tmp/fs-gg-skill-parity/report.md \
  --summary-json /tmp/fs-gg-skill-parity/summary.json --fail-on high
```

**Done when (SC-005)**: skill present + consistent across all three trees; parity check green.

### FSI recipe (the loop's core move)

```fsharp
#r "nuget: FS.GG.UI.Symbology"          // or #r the built DLLs in-tree
#r "nuget: FS.GG.UI.Symbology.Render"
open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

type UnitStats = { Side: string; Role: string; Dps: float; Hp: float; HpMax: float; Speed: float; Armor: float; Facing: float }

// the editable per-game ChannelMap (data — NOT library internals):
let mapUnit (u: UnitStats) : Token =
    { Symbology.defaultToken with
        Faction = (match u.Side with "blue" -> Ally | "red" -> Enemy | _ -> Neutral)
        Klass   = (match u.Role with "tank" -> Heavy | "scout" -> Scout | _ -> Mobile)
        Threat  = min 1.0 (u.Dps / 120.0)
        Health  = u.Hp / u.HpMax
        Speed   = int (min 4.0 (u.Speed / 4.0))
        Shield  = u.Armor > 30.0
        Heading = u.Facing }

let board = Symbology.gallery 4 24.0 (roster |> List.map mapUnit)
let png   = Render.toPng { Width = 920; Height = 660 } board "./work/iter-001"
// → read `png` back, CRITIQUE at target size, capture feedback, TWEAK mapUnit only, repeat.
```

## M5 — end-to-end dry-run with provenance (→ O5)

- Drive the full loop on a real roster (6–10 units) across ≥2 feedback rounds where **only** `mapUnit` changes
  between rounds.
- **Provenance (SC-009 / FR-017)**: every iteration writes a *timestamped board PNG* + a *mapping snapshot* under
  the working dir; on approval emit the final symbol-set module + rationale and pin a golden board.

**Done when**: a complete render→tweak→approve audit trail exists; final set pinned (golden, byte-identity).

---

## Coverage map (success criteria → where validated)

| SC | Where |
|---|---|
| SC-001 determinism / stable identity | M1 |
| SC-002 channel presence | M1 |
| SC-003 codec fidelity | M1 |
| SC-004 no core-surface drift | M1 / M2 / M3 surface gate |
| SC-005 skill parity | M4 |
| SC-006 filmstrip reproducibility | M2 |
| SC-007 legibility at target size | M1 (readback) |
| SC-008 public render pass + fail-loud | M3 |
| SC-009 dry-run audit trail | M5 |
