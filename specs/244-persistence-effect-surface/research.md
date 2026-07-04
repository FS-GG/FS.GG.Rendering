# Phase 0 Research: Persistence (save/load) effect surface + fs-gg-persistence product skill

Feature `244-persistence-effect-surface`. All Technical-Context unknowns resolved below; each item
is Decision / Rationale / Alternatives. This mirrors the just-shipped Feature 243 (audio) research,
which established the placement/precedents this feature follows.

## R1 — Home for the pure `PersistenceEffect` surface

**Decision**: Add a new `Persistence` module (`src/Canvas/Persistence.fsi` + `Persistence.fs`) to
the existing `FS.GG.UI.Canvas` package. No new package, no `template/capabilities.yml` catalog row
(**skill-only treatment** — this is the FR-013 decision).

**Rationale**:
- `FS.GG.UI.Canvas` is the game-primitives package (Rng/FixedStep/Loop, and now `Audio`), is
  **dependency-light** (single ProjectReference to Scene, zero PackageReferences), and is already
  gated to `profile in [game, sample-pack]` in the generated product. Persistence is a game-facing
  pure request vocabulary with no rendering dependency — the exact same shape as the sim primitives
  and the audio surface already there.
- `fs-gg-game-core` and `fs-gg-audio` are deliberately **skill-only** (no capability catalog row;
  Canvas carries the primitives). Mirroring that keeps the 16-package set, the BOM, the release
  matrix, and 38 lockfiles unchanged. A new `FS.GG.UI.Persistence` package would touch all of those
  for a minimal surface — disproportionate.
- Consequence: FR-010 (persistence available only for game/sample-pack) is satisfied by the
  **existing** Canvas package gate — the module ships automatically with Canvas. Only the gate
  comment needs to mention persistence; no new `Product.fsproj` reference is added.

**Alternatives considered**:
- *New `FS.GG.UI.Persistence` package* — cleaner isolation, but adds a package to the 16-package
  set, BOM, release tag matrix, lockfiles, and surface baselines. Rejected as disproportionate for
  a minimal capability; can be extracted later if persistence grows.
- *Put `PersistenceEffect` in `Scene`* — Scene is referenced by both Canvas and SkiaViewer, which
  would let a SkiaViewer arm see the type. Rejected: persistence is not a scene primitive; it would
  pollute the dependency-light scene surface with a semantically unrelated type.

## R2 — Where the interpreter lives, and the SkiaViewer seam

**Decision**: The **record-only (headless) interpreter is pure and lives in Canvas** beside the DU
(`Persistence.record`/evidence over a `PersistenceEffect list`). The **real file-backed backend in
`src/SkiaViewer/Host` is an explicit deferral**, documented as the extension point — it is *not*
stubbed into the shipped surface in this feature.

**Rationale**:
- `SkiaViewer` does **not** reference Canvas today (its edges are Scene, KeyboardInput,
  Diagnostics). Adding a `SkiaViewer→Canvas` edge now, solely to host a no-op persistence arm, is
  speculative coupling. The constitution minimises dependencies, and Review P6 (#49) specifically
  removed "success-shaped stubs on public API." A hollow SkiaViewer persistence arm would
  reintroduce exactly that.
- `fs-gg-game-core` and `fs-gg-audio` set the precedent: their surfaces live in Canvas and are
  consumed by **pure product code**; there is **no SkiaViewer arm** for either. Persistence mirrors
  this precisely — the product's own MVU `update` emits `PersistenceEffect` values and the pure
  record-only interpreter turns them into evidence. That fully delivers US1 + US2 without touching
  the viewer.
- The **load *result*** (a real backend reads a file and dispatches the loaded payload back to the
  model as a `Msg`) is the one place persistence differs from audio's fire-and-forget effects. This
  is deferred with the backend: the pure surface only *requests* a load; the result-dispatch is a
  host concern the seam admits additively without changing `Persistence.fsi` (FR-007). When the
  real backend is built it brings its own `SkiaViewer→Canvas` (or dedicated host) edge with it.

**Interpreter shape** mirrors the established `interpret*` pattern and the audio record-only fold:
the interpreter is the fold that turns a batch of requested effects into an ordered evidence value.
An unknown-slot `Load`/`DeleteSlot` is recorded faithfully (no error) — safe failure lives at the
deferred backend, not in the pure request/record path.

**Alternatives considered**:
- *Add a no-op persistence arm to `ViewerEffect<'msg>` + `interpretEffect` now* — forces the
  SkiaViewer→Canvas edge and ships a hollow stub. Rejected per the honest-API principle; recorded
  as the deferred real-backend seam instead (same call audio made).
- *Model `Load` as returning the payload synchronously from the pure surface* — would require the
  pure surface to perform or fake I/O. Rejected: the load *result* is a host-dispatched `Msg`,
  deferred with the backend; the pure surface only requests.

## R3 — Headless evidence model

**Decision**: The record-only interpreter produces an ordered **`PersistenceEvidence`** value — the
list of requested `PersistenceEffect`s in dispatch order (slot, version, and opaque payload
preserved) — that tests assert on directly. Persistence to a `readiness/*.json` artifact reuses the
existing readiness/run-evidence convention but is optional for the minimal slice (the pure value is
the primary evidence).

**Rationale**: This mirrors the canonical "evidence comes from requested values, not real output"
mechanism the viewer and the audio surface already use. Keeping the primary evidence a pure value
keeps US2's tests filesystem-free and deterministic (Constitution Principle V — this is **real**
evidence of what was requested, not synthetic; nothing is faked, so no `Synthetic` disclosure is
required). The recorded payload is carried verbatim — the interpreter never parses it.

**Alternatives considered**: writing a `readiness/persistence-evidence.json` on every run — deferred
as optional; adds I/O to the minimal slice with no test benefit over asserting the pure value.

## R4 — Skill wiring formats (exact, to replicate game-core / audio)

**Decision**: Author `template/product-skills/fs-gg-persistence/SKILL.md` (canonical body), add the
`.agents/skills/fs-gg-product-persistence/SKILL.md` + `.claude/skills/fs-gg-product-persistence/SKILL.md`
wrapper pair, add a `template.json` copy block, and **regenerate** the manifest digest.

**Rationale / mechanics** (confirmed against the shipped `fs-gg-audio` wiring):
- Manifest entry (`template/skill-manifest/skill-manifest.json`) has `id`, `scope: product`,
  `sha256`, `resolvablePath`, `materializes-when: "profile in [game, sample-pack]"`, `supplied-by`.
  The **sha256 is generated**, not hand-written, by `scripts/generate-skill-manifest.fsx` (SHA256
  over the canonical SKILL.md body). → Task: edit SKILL.md, run the generator, commit the digest.
- Template copy block (`.template.config/template.json`) mirrors the `fs-gg-audio` block:
  `{ condition: "(profile == \"game\" || profile == \"sample-pack\")", source:
  "template/product-skills/fs-gg-persistence/", target: ".agents/skills/fs-gg-persistence/",
  copyOnly: ["**/*"] }`. Product skills emit to `.agents/skills/` only (Feature 231/ADR-0014); the
  `copyOnly` verbatim ship is what makes the delivered body byte-match the manifest sha256.
- The `fs-gg-product-*` wrappers are single-file `SKILL.md` thin pointers to the canonical body; the
  `.agents` and `.claude` copies differ only in the "Codex-active"/"Claude-active" token. There is
  **no wrapper generator** — wrappers are authored per-skill and validated for **parity** by
  `tools/Rendering.Harness/SkillParity.fs`. The predicate must be identical in the manifest and the
  template condition.

**Alternatives considered**: none — these are fixed repo mechanisms.

## R5 — API surface baseline

**Decision**: Regenerate `readiness/surface-baselines/FS.GG.UI.Canvas.txt` after adding the module
(expect new rows `FS.GG.UI.Canvas.Persistence` and the public types e.g.
`FS.GG.UI.Canvas.PersistenceEffect`, `SaveEnvelope`, `SaveSlot`, `SavePayload`,
`PersistenceEvidence`), via `scripts/refresh-surface-baselines.fsx`; optionally ship a doc copy
`template/base/docs/api-surface/Canvas/Persistence.fsi` alongside the existing Loop/Rng/Audio docs.

**Rationale**: The baseline is the authoritative input to the API surface-drift gate
(`tests/Package.Tests/SurfaceAreaTests.fs`, cross-checked by `build/Governance/PackageSurface.fs`) —
the Principle II "visibility lives in .fsi" guard. Any Tier-1 surface add must update it or the gate
fails.

**Alternatives considered**: hand-editing the baseline — rejected; the generator reflects over the
built assembly and is the intended path (hand-edits drift from reality).

## R6 — Change tier & constitution posture

**Decision**: **Tier 1 (contracted change)** — adds public API to `FS.GG.UI.Canvas`. Requires `.fsi`
first (Principle I), surface-baseline update (Principle II), semantic tests (Principle V), and doc
updates.

**Rationale**: A new public module + DU is by definition public-surface. The Elmish-boundary
obligation (Principle IV) is met because `PersistenceEffect` is precisely the "Effect / Cmd<Msg>"
data the product's own `update` emits and an edge interpreter consumes; persistence itself is a
stateless request vocabulary (no Model/Msg of its own), so it does not need its own MVU triad — it
plugs into the consuming product's MVU. The one asymmetry vs. audio (a load *result* returns to the
model) is a **deferred host concern**, not part of this pure surface.
