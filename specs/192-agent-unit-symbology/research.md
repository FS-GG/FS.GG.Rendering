# Phase 0 Research — Agent-Driven Unit-Symbology Design System

**Date**: 2026-06-25 | **Feature**: `192-agent-unit-symbology`

This phase resolves the four M0 decision gates (G1–G4 from the source report §10.4), re-verifies the
design anchors against the **current** tree (the source report's `file:line` anchors were dated
2026-06-25 and re-checked here), and records the one place where the source report's sketch must bend
to the real public surface. The spec carries **no `NEEDS CLARIFICATION` markers** — the source design
report is detailed and the assumptions section is explicit — so the research here is gate resolution
and anchor verification, not open-question discovery.

---

## Anchor re-verification (current tree)

| Claim (source report) | Verified against | Result |
|---|---|---|
| Public headless render path is `ReferenceRendering.run` | `src/SkiaViewer/ReferenceRendering.fsi:74` | ✅ `run: ReferenceRenderingRequest -> ReferenceRenderingEvidence` |
| Verdict signals pass/fail | `ReferenceRendering.fsi:6-9` | ⚠️ **Three** cases: `ReferencePassed \| ReferenceFailed \| ReferenceEnvironmentLimited` — see Decision R2 |
| Evidence carries image + diagnostics | `ReferenceRendering.fsi:26-37` | ✅ `ImagePath: string option`, `Diagnostics: string list`, `Verdict`, `ImageIdentity: string option` |
| Codec gives canonical bytes / identity | `src/Scene/SceneCodec.fsi:114-120,156-162` | ✅ `export: Scene -> PortableScenePackage`; `.CanonicalBytes: byte[]` |
| Scene primitive set for `token` | `src/Scene/Scene.fsi:71-98` | ✅ `Scene.group/circle/line/path/arc`; `Path.create`; `Paint.fill/stroke/with*` (incl. `withShader`, `withPathEffect`, `withStrokeCap`) |
| Vector channels available | `src/Scene/Types.fsi:60-84` | ✅ `Shader.RadialGradient/LinearGradient/SweepGradient`, `PathEffect.Dash`, `StrokeCap`, `Color/Point/Rect/Size` |
| New-library precedent (Scene-only, packable) | `src/Canvas/Canvas.Lib.fsproj` | ✅ `IsPackable`, `PackageId FS.GG.UI.Canvas`, `ProjectReference ..\Scene`, `InternalsVisibleTo Canvas.Tests` |
| Surface baselines per package | `readiness/surface-baselines/*.txt` (13 files) | ✅ one `FS.GG.UI.<Pkg>.txt` per package — add two |
| Skill trees for library-authoring skills | `.claude/skills/`, `.agents/skills/`, `template/product-skills/` | ✅ `fs-gg-scene`/`-ui-widgets`/`-skiaviewer`/`-testing` present in **all three** |
| Skill-parity gate exists | `scripts/check-agent-skill-parity.fsx`; `src/Diagnostics/skill/SKILL.md` | ✅ `dotnet fsi scripts/check-agent-skill-parity.fsx … --fail-on high` |

**Net:** every anchor holds except the verdict shape, which is richer than the report's PoC snippet
assumed. That single delta is folded into Decision R2 (fail-loud must treat *anything that is not
`ReferencePassed` with a real `ImagePath`* as failure).

---

## Gate resolutions (M0)

### R1 — Library home  *(gate G1)*

- **Decision**: A dedicated **`FS.GG.UI.Symbology`** library under `src/Symbology/`, Scene-only — **not**
  folded into `FS.GG.UI.Canvas` or `Controls`.
- **Rationale**: Mirrors the accepted spec-191 `FS.GG.UI.Canvas` reasoning (`src/Canvas/Canvas.Lib.fsproj`):
  keep game-symbol vocabulary off the core control surface, keep it independently testable and packable,
  and keep the pure library's dependency set to `FS.GG.UI.Scene` alone. Satisfies FR-001/FR-021 and the
  constitution's "distinct layers" constraint (a symbol vocabulary is its own layer, not a control fork).
- **Alternatives considered**: Fold into `Canvas` — rejected: bloats the canvas combinator surface with
  game-domain types unrelated to drawing, and couples the symbol vocabulary's release cadence to Canvas.

### R2 — Render bridge  *(gate G2)*

- **Decision**: A separate thin helper **`FS.GG.UI.Symbology.Render`** (`src/Symbology.Render/`,
  references `Symbology` + `SkiaViewer`) wrapping the public `ReferenceRendering.run` via a `SceneCodec`
  round-trip. **Fail-loud rule (grounded in the real 3-case verdict):** the helper returns the image path
  **only** when `Verdict = ReferencePassed` **and** `ImagePath = Some p`; for `ReferenceFailed`,
  `ReferenceEnvironmentLimited`, or `ImagePath = None`, it raises an error carrying `Diagnostics`
  (joined) — never a blank image as success.
- **Rationale**: `SceneRenderer` is `internal` (source report §2), so scripts cannot call `paintNode`;
  the codec round-trip is the public path and was PoC-verified to preserve every paint channel
  (Path/gradients/Dash/Arc/stroke). It also yields a free content-addressable PNG + `reference-evidence.md`
  per call — a regression identity per iteration (FR-013). Keeping the helper in its **own** project keeps
  the pure library IO/raster-free (FR-011). Satisfies FR-010/FR-012.
- **CI note**: `ReferenceRendering` uses a CPU `SKSurface` (no GL), so headless CI should yield
  `ReferencePassed`; `ReferenceEnvironmentLimited` is nonetheless handled as a loud failure rather than a
  silent blank, per Principle VI.
- **Alternatives considered**: (a) make `SceneRenderer` public — rejected: widens `SkiaViewer`'s surface
  for a niche need and risks core-surface drift (SC-004). (b) Re-implement `paintNode` in scripts —
  rejected: duplication and drift. (c) A new direct-raster public entry in `SkiaViewer` — **deferred** to a
  v2 promotion, gated on a *measured* loop-latency problem (the codec cost is acceptable for design-time
  galleries).

### R3 — v1 grammar scope  *(gate G3)*

- **Decision**: Ship the **Directional-Token** grammar only.
- **Rationale**: It is the PoC-proven grammar; one grammar keeps the channel vocabulary fixed and the
  legibility rules uniform while the agent loop is validated. The library's channel enums are designed so
  Badge/Ring can later land as sibling elements behind the *same* vocabulary.
- **Alternatives considered**: Build Badge/Ring now — rejected: triples the goldens and legibility surface
  before the loop itself is proven; deferred to M7 (out of scope here).

### R4 — Skill mirroring  *(gate G4)*

- **Decision**: Author `fs-gg-symbology` once and mirror to **three** trees: `.claude/skills/`,
  `.agents/skills/`, and `template/product-skills/`.
- **Rationale**: Verified that the existing library-authoring `fs-gg-*` skills (`fs-gg-scene`,
  `fs-gg-ui-widgets`, `fs-gg-skiaviewer`, `fs-gg-testing`) live in exactly those three trees; matching that
  placement is what the skill-parity check enforces (SC-005). `fs-gg-symbology` is a library-authoring skill
  of the same family, so it follows the same three-tree placement (not the smaller product-only subset).
- **Alternatives considered**: `.claude` + `.agents` only — rejected: would fail parity against the
  established three-tree placement for this skill family and starve product templates of the recipe.

---

## Determinism mechanism (decision, not a gate)

- **Decision**: Golden/identity assertions key on `SceneCodec.export token |> _.CanonicalBytes` (and the
  `PackageInspectionReport`/`ImageIdentity` for the rendered board), **not** on the Canvas `Elements`
  content fingerprint.
- **Rationale**: The pure library depends only on `FS.GG.UI.Scene` (R1); the FNV-1a fingerprint and
  `cached` combinator the source report alludes to live in `FS.GG.UI.Canvas` (`src/Canvas/Elements.fsi`),
  which Symbology must **not** depend on. Identical `Token` ⇒ identical `Scene` value (pure function) ⇒
  identical canonical bytes is the honest, dependency-correct identity. This satisfies SC-001 directly and
  feeds SC-003 (the export→import→raster fidelity check) and SC-006 (filmstrip frame reproducibility).

## Legibility & colour discipline (constraints carried into design)

- **Assign-by-urgency + redundancy**: urgent state is encoded across multiple pre-attentive channels;
  detail lives on inspection-only channels (dash, corner mounts). One *active* motion at a time; never put
  critical state on dash alone (FR-016, spec edge "Channel overload").
- **Hue separation (FR-019)**: state semantics reuse the repo's Ant status tokens
  (processing/warning/error/success/default via `fs-gg-ant-design`); faction uses a *separate* saturated
  palette — *state* and *team* never share the hue channel (spec edge "Hue collision").
- **Size-dependent legibility (SC-007)**: channel-presence and faction/class separability are asserted by
  `fs-gg-diagnostics` readback at the **target on-board size**, not at an arbitrary large size.
- **Screen-aligned gauges**: the health arc stays screen-aligned while the body rotates by heading (spec
  edge "Rotating vs screen-aligned gauges"); a per-theme toggle is an open follow-up, not in scope.

## Out of scope (confirmed deferred)

M6 live `volatile'`/`Loop` board sample; M7 legibility-scoring linter, Badge/Ring grammars, label text via
`setRealTextMeasurer`; a direct-raster public `SkiaViewer` entry (R2 v2); auto-generating the `ChannelMap`
without human review (the loop is deliberately human-approved).

## Gate decision record (to copy into the source report §11 at implementation)

- **G1 = dedicated `FS.GG.UI.Symbology`** (R1). **G2 = `ReferenceRendering.run` round-trip, fail-loud on
  the 3-case verdict** (R2). **G3 = Token grammar only** (R3). **G4 = mirror to `.claude` + `.agents` +
  `template/product-skills`** (R4).
