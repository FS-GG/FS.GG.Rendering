# Implementation Plan: Agent-Driven Unit-Symbology Design System

**Branch**: `192-agent-unit-symbology` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

> ## ✅ Implementation status — COMPLETE (2026-06-25)
>
> All 44 tasks (T001–T044) across Phases 1–6 are done; `tasks.md` is fully checked.
>
> **Shipped**
> - `src/Symbology/` (`FS.GG.UI.Symbology`) — pure, Scene-only: `Token` + `Faction`/`Klass`/`Sigil`/`TokenState`/`Motion`, and `Symbology.defaultToken`/`token`/`animate`/`gallery`/`filmstrip`.
> - `src/Symbology.Render/` (`FS.GG.UI.Symbology.Render`) — `Render.toPng`, fail-loud over the public `ReferenceRendering.run`.
> - `tests/Symbology.Tests` (**34/34 green**) + `tests/Symbology.Render.Tests` (**3/3 green**).
> - Surface baselines `FS.GG.UI.Symbology.txt` + `FS.GG.UI.Symbology.Render.txt`; **zero drift** on existing core baselines (SC-004). Registered in `scripts/refresh-surface-baselines.fsx` + the surface gate.
> - `fs-gg-symbology` skill: canonical `src/Symbology/skill/` + `.claude`/`.agents`/`template/product-skills` wrappers + reference `.fsx`; **parity gate green** (critical=0 high=0 warning=0, SC-005).
> - M5 dry-run audit trail under `readiness/dry-run/` (3 rounds + golden board + final module + rationale); M0 spike evidence under `readiness/m0-spike-evidence.md`.
> - Both packages pack to and restore from the local feed (`~/.local/share/nuget-local/`).
>
> **Evidence**: every SC-001…SC-009 demonstrated — see `readiness/quickstart-validation.md`. No-regression
> baseline: before (`readiness/baseline.md`) = 17 projects, 3 red (Package.Tests 8, ControlsGallery 2,
> SecondAntShowcase 1); after (`readiness/baseline-after.md`) = 19 projects, **2 red** (Package.Tests 8,
> ControlsGallery 2 — both pre-existing and unrelated). The two new test projects are green and introduced
> **zero new reds** (SecondAntShowcase's prior lone transient failure even cleared). Gate decisions G1–G4
> recorded in the source report §11.

**Input**: Feature specification from `/specs/192-agent-unit-symbology/spec.md`

**Source design**: [`docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md`](../../docs/reports/2026-06-25-12-48-agent-symbology-design-system-analysis-and-plan.md) (the §4 grammar, §5 architecture, D1–D8 decisions, O1–O5 objectives, M0–M7 roadmap). This plan scopes **M1–M5** (the minimum viable agent loop with provenance); M6 (live board) and M7 (governance/breadth) are out of scope.

## Summary

Deliver a reusable, deterministic **unit-symbology design system**: a pure first-party library that turns a per-game stat-to-channel mapping into legible abstract vector symbols (a fixed encoding grammar of shape/colour/stroke/fill/motion), a public headless render path so an agent can *see* a board, and an orchestrating skill that drives a render→eyeball→tweak loop with provenance until a design is approved.

Technical approach (grounded against the tree on 2026-06-25):

- A new Scene-only library **`FS.GG.UI.Symbology`** (`src/Symbology/`) mirrors the `FS.GG.UI.Canvas` precedent (`src/Canvas/Canvas.Lib.fsproj`): `IsPackable`, references **only** `FS.GG.UI.Scene`, `InternalsVisibleTo` its test project. It exposes the `Token` props record, channel enums, the pure `token : Token -> Scene`, motion overlays `animate : Motion -> Token -> phase:float -> Scene`, and board layouts `gallery`/`filmstrip`. Determinism identity = identical `Token` ⇒ identical `Scene` value ⇒ identical `SceneCodec.export scene |> .CanonicalBytes`.
- A separate thin helper library **`FS.GG.UI.Symbology.Render`** (`src/Symbology.Render/`) references `Symbology` + `SkiaViewer` and wraps the public `ReferenceRendering.run`. It keeps all IO/raster out of the pure library and **fails loud** on any non-`ReferencePassed` verdict *or* a `None` image path — never returning a blank image as success.
- A single authored **`fs-gg-symbology`** skill, mirrored to all three skill trees (`.claude/skills/`, `.agents/skills/`, `template/product-skills/`) that carry the library-authoring `fs-gg-*` skills, encoding the grammar, the legibility rules, the library/render API, the grammar-vs-mapping pattern, the FSI recipe, and the feedback protocol. Gated by the existing skill-parity check.
- An **end-to-end dry-run** on a real roster (6–10 units) across ≥2 feedback rounds, writing per-iteration provenance (timestamped board + mapping snapshot) and, on approval, a final symbol-set module + rationale + a pinned golden board.

Per the constitution this lands **`.fsi`-first** (Principle I/II) with new surface baselines and zero drift on existing core surfaces (FR-021, SC-004). It is a **Tier 1** (contracted) change: it adds public package surface.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This is a *greenfield additive* feature, not a defect fix, so there is no root-cause map. The
> analogue of the live-smoke mandate here is the **M0 render-bridge spike** (T0.1): before any
> production code, a throwaway script must drive the public `ReferenceRendering.run` from a *new*
> project and confirm a one-token gallery renders `ReferencePassed` with a non-blank PNG. Deterministic
> unit tests can pass while the real render path is broken; `/speckit-tasks` MUST schedule that spike
> as the first Foundational task and treat its evidence — not the source-report PoC narrative — as the
> confirmation that the render bridge works in *this* checkout.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints).

**Primary Dependencies**: `FS.GG.UI.Scene` (immutable scene + `SceneCodec`) for the pure library; `FS.GG.UI.SkiaViewer` (`ReferenceRendering`, CPU `SKSurface`, no GL) for the render bridge only. No new third-party dependency.

**Storage**: Filesystem only, and only in the render bridge / loop provenance — content-addressable PNG + `reference-evidence.md` written by `ReferenceRendering.run`, plus per-iteration timestamped boards and mapping snapshots under a working directory. The pure library performs no IO.

**Testing**: project test pattern — `tests/Symbology.Tests/` (pure: golden scenes, determinism, channel-presence readback, codec-fidelity) and `tests/Symbology.Render.Tests/` (render smoke). xUnit-style projects mirroring `tests/Canvas.Tests/`. Surface gate in the existing package surface tests.

**Target Platform**: Linux/CI headless (CPU raster); same as `ReferenceRendering`. No GL required.

**Project Type**: Multi-project F# library solution (`FS.GG.Rendering.slnx`). Two new `src/` projects + two new `tests/` projects; library-authoring skill mirrored across three skill trees.

**Performance Goals**: Design-time, not runtime — galleries render in a tight human-in-the-loop edit cycle. The codec round-trip (D2) is accepted for design-time boards; a direct-raster public entry is deferred (G2) and promoted only if loop latency demands it. No 60 fps obligation in this scope (live board is M6, out of scope).

**Constraints**: Determinism is the hard constraint — no wall-clock or IO in `token`/`animate`/`gallery`/`filmstrip`; motion phase is caller-owned; filmstrip frames byte-reproducible from a phase schedule (SC-006). Identity is vector sigils only — no label text this iteration (FR-022). State colour reuses Ant status tokens; faction uses a separate saturated palette (FR-019) — *state* and *team* never share hue.

**Scale/Scope**: M1–M5. Library surface ≈ one `Token` record + ~5 enums + ~6 functions (`defaultToken`, `token`, `animate`, `gallery`, `filmstrip`); render helper ≈ one function (`toPng`). One skill (×3 trees) + one `.fsx` template. One dry-run roster (6–10 units). Deferred: M6 live board sample, M7 legibility linter / Badge+Ring grammars / label text.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence in this plan |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | PASS | `.fsi`-first sketch in [contracts/](./contracts/); semantic tests exercise the *packed/public* surface (FSI recipe in quickstart), fail-before/pass-after. |
| **II. Visibility in `.fsi`, not `.fs`** | PASS | Both new projects ship curated `.fsi`; no `private`/`internal`/`public` modifiers on `.fs` top-level bindings; new surface baselines `FS.GG.UI.Symbology.txt` + `FS.GG.UI.Symbology.Render.txt`. |
| **III. Idiomatic Simplicity** | PASS | Pure functions + records + DUs; no SRTP/reflection/type providers/custom CE. Local geometry mutation (e.g. a point-transform accumulator) is allowed and disclosed at the use site if used on a hot inner loop. |
| **IV. Elmish/MVU boundary** | PASS (scoped) | The pure library is stateless data (no boundary needed). The render bridge is a **thin edge wrapper over `ReferenceRendering`**, which *already* exposes the Model/Msg/Effect/`init`/`update`/interpreter boundary; `Render.toPng` is the documented edge helper, not new stateful workflow. The design *loop* is the agent/skill protocol (human-in-the-loop), not an in-process workflow engine — it carries no in-code `Model`/`update`, so it adds no MVU surface. |
| **V. Test Evidence Mandatory** | PASS | M0 spike (real render) + golden/determinism/readback/codec-fidelity tests + render smoke + parity report + dry-run audit trail. Real evidence preferred; any synthetic disclosed per Principle V. |
| **VI. Observability & Safe Failure** | PASS | Render bridge **fails loud** with `ReferenceRendering` diagnostics on any non-`ReferencePassed` verdict or missing image (FR-012); zero-area token degrades to a visible placeholder (FR-020). |
| **Change Classification** | Tier 1 | Adds public package surface; full artifact chain (spec/plan/`.fsi`/baselines/tests/docs) required. |
| **Engineering Constraints** | PASS | `net10.0`; SkiaSharp-over-GL backend untouched (render bridge uses the existing CPU reference path); per-module `.fsi` + baselines; no new dependency; `FS.GG.UI.*` package identity; pack to `~/.local/share/nuget-local/`. New library is a *distinct layer* (symbol vocabulary), not a per-theme control fork — it does not touch the one semantic control set. |

**Gate result: PASS** — no violations; Complexity Tracking left empty. The two-new-projects choice (vs folding into Canvas/Controls) is justified by D1 and the spec-191 precedent (keeps game-symbol vocabulary off the core control surface; independently testable/packable) and is therefore not a complexity violation.

## Project Structure

### Documentation (this feature)

```text
specs/192-agent-unit-symbology/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — gate/decision resolution (G1–G4, D-anchors re-verified)
├── data-model.md        # Phase 1 — Token, channels, Motion, boards, evidence, provenance
├── quickstart.md        # Phase 1 — FSI render recipe + per-milestone validation
├── contracts/           # Phase 1 — .fsi surface sketches + skill/loop protocol contract
│   ├── FS.GG.UI.Symbology.fsi
│   ├── FS.GG.UI.Symbology.Render.fsi
│   └── agent-loop-protocol.md
├── checklists/          # (pre-existing)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Scene/                       # EXISTING — sole dependency of the pure library
│   ├── Scene.fsi / Scene.fs     #   Scene.path/circle/arc/group/line, Path.*, Paint.*
│   ├── Types.fsi                #   Color, Point, Rect, Size, Shader(RadialGradient…), PathEffect(Dash), …
│   └── SceneCodec.fsi / .fs     #   export : Scene -> PortableScenePackage (.CanonicalBytes) — determinism identity
├── SkiaViewer/                  # EXISTING — referenced ONLY by the render bridge
│   └── ReferenceRendering.fsi   #   run : ReferenceRenderingRequest -> ReferenceRenderingEvidence (3-case verdict)
│
├── Symbology/                   # NEW (M1/M2) — pure, Scene-only
│   ├── Symbology.fsproj         #   IsPackable; PackageId FS.GG.UI.Symbology; refs Scene only; InternalsVisibleTo Symbology.Tests
│   ├── Symbology.fsi            #   AUTHORED FIRST — Token, Faction/Klass/Sigil/TokenState/Motion, defaultToken, token, animate, gallery, filmstrip
│   └── Symbology.fs             #   silhouette+sigil tables; token body (Appendix A); non-public place/pathOf/strokePaint/lerpColor
│
└── Symbology.Render/            # NEW (M3) — thin IO helper, NOT in the pure lib
    ├── Symbology.Render.fsproj  #   IsPackable; PackageId FS.GG.UI.Symbology.Render; refs Symbology + SkiaViewer
    ├── Render.fsi               #   AUTHORED FIRST — toPng : Size -> Scene -> dir:string -> string
    └── Render.fs                #   wraps ReferenceRendering.run; fail-loud on non-ReferencePassed / None image

tests/
├── Symbology.Tests/             # NEW (M1/M2) — pure: golden scenes, determinism (SC-001), channel presence (SC-002),
│                                #   codec fidelity (SC-003), filmstrip reproducibility (SC-006), zero-area placeholder (FR-020)
└── Symbology.Render.Tests/      # NEW (M3) — render smoke: ReferencePassed + non-blank PNG (SC-008); fail-loud on bad verdict

readiness/surface-baselines/
├── FS.GG.UI.Symbology.txt        # NEW — pinned by the surface gate
└── FS.GG.UI.Symbology.Render.txt # NEW — pinned by the surface gate

.claude/skills/fs-gg-symbology/           # NEW (M4) — authored skill
.agents/skills/fs-gg-symbology/           # NEW (M4) — mirror
template/product-skills/fs-gg-symbology/  # NEW (M4) — mirror   → all three gated by skill-parity (SC-005)
    └── (SKILL.md + reference .fsx roster→ChannelMap→gallery template)

FS.GG.Rendering.slnx              # register the two src projects + two test projects
```

**Structure Decision**: Two new `src/` libraries (pure `FS.GG.UI.Symbology` + IO `FS.GG.UI.Symbology.Render`) plus two matching `tests/` projects, registered in `FS.GG.Rendering.slnx`. This mirrors the accepted `FS.GG.UI.Canvas` two-layer precedent (`src/Canvas/Canvas.Lib.fsproj` references Scene only; IO/host stays out). The pure library depends on `FS.GG.UI.Scene` alone; the render helper is the only component that may reference `SkiaViewer`. Existing core `Controls`/`Canvas`/`Scene`/`SkiaViewer` public surfaces are not modified (FR-021, SC-004). The orchestrating skill is authored once and mirrored to the three trees that carry the existing library-authoring `fs-gg-*` skills (verified: `fs-gg-scene`, `fs-gg-ui-widgets`, `fs-gg-skiaviewer`, `fs-gg-testing` are present in `.claude/skills/`, `.agents/skills/`, and `template/product-skills/`).

## Complexity Tracking

> No constitution violations. Section intentionally empty.
