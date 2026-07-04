# Phase 0 Research: Audio effect surface + fs-gg-audio product skill

Feature `243-audio-effect-surface`. All Technical-Context unknowns resolved below; each item is
Decision / Rationale / Alternatives.

## R1 — Home for the pure `AudioEffect` surface

**Decision**: Add a new `Audio` module (`src/Canvas/Audio.fsi` + `Audio.fs`) to the existing
`FS.GG.UI.Canvas` package. No new package, no `template/capabilities.yml` catalog row
(**skill-only treatment** — this is the FR-012 decision).

**Rationale**:
- `FS.GG.UI.Canvas` is the game-primitives package (Rng/FixedStep/Loop), is **dependency-light**
  (single ProjectReference to Scene, zero PackageReferences — `Canvas.Lib.fsproj:32`), and is
  already gated to `profile in [game, sample-pack]` in the generated product
  (`template/base/src/Product/Product.fsproj:21-24`). Audio is a game-facing pure request
  vocabulary with no rendering dependency — the exact same shape as the sim primitives already
  there.
- `fs-gg-game-core` is deliberately **skill-only** (no capability catalog row; Canvas carries the
  primitives). Mirroring that keeps the 16-package set, the BOM, the release matrix, and 38
  lockfiles unchanged. A new `FS.GG.UI.Audio` package would touch all of those for a minimal
  surface — disproportionate.
- Consequence: FR-009 (audio available only for game/sample-pack) is satisfied by the **existing**
  Canvas package gate — the audio module ships automatically with Canvas. Only the gate comment
  needs to mention audio; no new `Product.fsproj` reference is added.

**Alternatives considered**:
- *New `FS.GG.UI.Audio` package* — cleaner isolation, but adds a package to the 16-package set,
  BOM, release tag matrix, lockfiles, and surface baselines. Rejected as disproportionate for a
  minimal capability; can be extracted later if audio grows.
- *Put `AudioEffect` in `Scene`* — Scene is referenced by both Canvas and SkiaViewer, which would
  let a SkiaViewer arm see the type. Rejected: audio is not a scene primitive; it would pollute
  the dependency-light scene surface with a semantically unrelated type.

## R2 — Where the interpreter lives, and the SkiaViewer seam

**Decision**: The **record-only (headless) interpreter is pure and lives in Canvas** beside the
DU (`Audio.record`/evidence over an `AudioEffect list`). The **real audio-output backend in
`src/SkiaViewer/Host` is an explicit deferral**, documented as the extension point — it is *not*
stubbed into the shipped surface in this feature.

**Rationale**:
- `SkiaViewer` does **not** reference Canvas today (its edges are Scene, KeyboardInput,
  Diagnostics — `SkiaViewer.fsproj:86-88`). Adding a `SkiaViewer→Canvas` edge now, solely to host
  a no-op audio arm, is speculative coupling. The constitution minimises dependencies, and Review
  P6 (#49, closed) specifically removed "success-shaped stubs on public API." A hollow SkiaViewer
  audio arm would reintroduce exactly that.
- `fs-gg-game-core` sets the precedent: its primitives live in Canvas and are consumed by **pure
  product code**; there is **no SkiaViewer arm** for game-core. Audio mirrors this precisely — the
  product's own MVU `update` emits `AudioEffect` values and the pure record-only interpreter turns
  them into evidence. That fully delivers US1 + US2 without touching the viewer.
- When the real backend is built later, it legitimately depends on the audio types, so it brings
  the `SkiaViewer→Canvas` (or a dedicated host) edge **with** it — an honest additive edge at the
  point it's actually needed, satisfying FR-006 (surface unchanged).

**Interpreter shape** mirrors the established `interpret*` pattern: the record-only interpreter is
the fold that turns a batch of requested effects into an ordered evidence value (the same idea as
the SkiaViewer headless fold at `SkiaViewer.fs:2209`, where in the no-GL path the recorded effect
list *is* the evidence — `CaptureImageEvidence`/`WriteRunEvidence` are no-ops that just accumulate).

**Alternatives considered**:
- *Add a no-op `PlayAudio` arm to `ViewerEffect<'msg>` (Diagnostics.fsi:92) + `interpretEffect`
  (OpenGl.fs:1227) now* — matches the original issue's literal wording, but forces the
  SkiaViewer→Canvas edge and ships a hollow stub. Rejected per the honest-API principle; recorded
  as the deferred real-backend seam instead.

## R3 — Headless evidence model

**Decision**: The record-only interpreter produces an ordered **`AudioEvidence`** value — the list
of requested `AudioEffect`s in dispatch order — that tests assert on directly. Persistence to a
`readiness/*.json` artifact reuses the existing readiness/run-evidence convention but is optional
for the minimal slice (the pure value is the primary evidence).

**Rationale**: This mirrors the canonical "evidence comes from requested values, not real output"
mechanism the viewer already uses (`ViewerRunEvidence` at `Viewer.Types.fsi:426`; the no-GL fold
treats the recorded effect list as evidence). Keeping the primary evidence a pure value keeps
US2's tests hardware-free and deterministic (Constitution Principle V — this is **real** evidence
of what was requested, not synthetic; nothing is faked, so no `Synthetic` disclosure is required).

**Alternatives considered**: writing a `readiness/audio-evidence.json` on every run — deferred as
optional; adds I/O to the minimal slice with no test benefit over asserting the pure value.

## R4 — Skill wiring formats (exact, to replicate game-core)

**Decision**: Author `template/product-skills/fs-gg-audio/SKILL.md` (canonical body), add the
`.agents/skills/fs-gg-product-audio/SKILL.md` + `.claude/skills/fs-gg-product-audio/SKILL.md`
wrapper pair, add a `template.json` copy block, and **regenerate** the manifest digest.

**Rationale / mechanics** (all confirmed):
- Manifest entry (`template/skill-manifest/skill-manifest.json`) has `id`, `scope: product`,
  `sha256`, `resolvablePath`, `materializes-when: "profile in [game, sample-pack]"`,
  `supplied-by`. The **sha256 is generated**, not hand-written, by
  `scripts/generate-skill-manifest.fsx` (SHA256 over the canonical SKILL.md body, lowercase hex).
  → Task: edit SKILL.md, run the generator, commit the digest.
- Template copy block (`.template.config/template.json`) is
  `{ condition: "(profile == \"game\" || profile == \"sample-pack\")", source:
  "template/product-skills/fs-gg-audio/", target: ".agents/skills/fs-gg-audio/", copyOnly:
  ["**/*"] }`. Product skills emit to `.agents/skills/` **only** (Feature 231/ADR-0014); the
  `copyOnly` verbatim ship is what makes the delivered body byte-match the manifest sha256.
- The `fs-gg-product-*` wrappers are single-file `SKILL.md` thin pointers to the canonical body;
  the `.agents` and `.claude` copies differ only in the "Codex-active"/"Claude-active" token.
  There is **no wrapper generator** — wrappers are authored per-skill and validated for **parity**
  by `tools/Rendering.Harness/SkillParity.fs`. The predicate must be identical in the manifest and
  the template condition.

**Alternatives considered**: none — these are fixed repo mechanisms.

## R5 — API surface baseline

**Decision**: Regenerate `readiness/surface-baselines/FS.GG.UI.Canvas.txt` after adding the module
(expect new rows `FS.GG.UI.Canvas.Audio` and any public DU type e.g.
`FS.GG.UI.Canvas.AudioEffect`), via `scripts/refresh-surface-baselines.fsx`; optionally ship a doc
copy `template/base/docs/api-surface/Canvas/Audio.fsi` alongside the existing Loop/Rng docs.

**Rationale**: The baseline is the authoritative input to the API surface-drift gate
(`tests/Package.Tests/SurfaceAreaTests.fs`, cross-checked by `build/Governance/PackageSurface.fs`)
— the Principle II "visibility lives in .fsi" guard. Any Tier-1 surface add must update it or the
gate fails.

**Alternatives considered**: hand-editing the baseline — rejected; the generator reflects over the
built assembly and is the intended path (hand-edits drift from reality).

## R6 — Change tier & constitution posture

**Decision**: **Tier 1 (contracted change)** — adds public API to `FS.GG.UI.Canvas`. Requires
`.fsi` first (Principle I), surface-baseline update (Principle II), semantic tests (Principle V),
and doc updates.

**Rationale**: A new public module + DU is by definition public-surface. The Elmish-boundary
obligation (Principle IV) is met because `AudioEffect` is precisely the "Effect / Cmd<Msg>" data
the product's own `update` emits and an edge interpreter consumes; audio itself is a stateless
request vocabulary (no Model/Msg of its own), so it does not need its own MVU triad — it plugs
into the consuming product's MVU.
