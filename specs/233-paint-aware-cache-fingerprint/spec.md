# Feature Specification: paint-aware cache-boundary fingerprint

**Feature Branch**: `233-paint-aware-cache-fingerprint`

**Created**: 2026-07-02

**Status**: Draft

**Input**: Finding P2 / R1 of the [2026-07-02 repo review](../../docs/reports/2026-07-02-14-07-repo-code-quality-and-architecture-review.md). Resolves **FS-GG/FS.GG.Rendering#45**.

## Context (non-normative)

`Elements.cached key scene` (`src/Canvas/Elements.fs`) wraps an expensive fragment as a backend
replay-cache boundary — a `CachedSubtree` node carrying a `CacheId` (from the key) and a `Fingerprint`
(a deterministic FNV-1a fold over the fragment's render-affecting content). At paint time,
`PictureReplayCache.paintBoundary` (`src/SkiaViewer/PictureReplayCache.fs:152-158`) replays the
recorded `SKPicture` whenever the incoming `boundary.Fingerprint` **equals** the resident entry's
fingerprint under the same `CacheId`. Fingerprint equality is therefore the *sole* correctness gate
between "reuse last frame's pixels" and "re-record".

**The gap — the fingerprint was paint-blind.** The FNV fold behind the fingerprint
(`Elements.mixNode`) did not cover all render-affecting fields:

1. **`Paint` was ignored** on `PaintedRectangle`, `Points`, `Line`, `Path`, and (via wrapper recursion)
   `ClipNode` / `PerspectiveNode` / `ColorSpaceNode` mixed only their child scene, never the transform
   or clip that reshapes it.
2. **Whole node types collapsed to one constant** (`| _ -> step h 18UL`): `FilledEllipse`, `Ellipse`,
   `Arc`, `Vertices`, `TextRun`, `GlyphRun`, `RegionNode`, `PictureNode`. Any two nodes of these kinds
   hashed identically regardless of their fields.
3. **Latent field drops** even in handled cases: `Path` never hashed its `FillType`, and `ArcTo` path
   commands dropped `bounds.Height`.

**Why it breaks (stale frames):** change a cached subtree's stroke colour, dash pattern, arc sweep,
ellipse bounds, or glyph text under the *same* `cached` key and the fingerprint does not move — so
`paintBoundary` takes the HIT branch and replays the previously recorded picture, painting stale pixels.
A disabled cache (the parity oracle) renders correctly, so the bug only manifests with the replay cache
enabled, exactly on its hot path.

**The fix:** make the fingerprint fold cover **every** render-affecting field of every `SceneNode`
(paint, shaders, filters, path effects, stroke, clips, transforms, colour space, fonts, glyph/text runs,
regions, pictures, vertices), and make the node `match` **exhaustive** (drop the wildcard) so any future
`SceneNode` case is a compile error until it is explicitly hashed. `SceneHash.hashScene` in the Controls
layer already mixes every field; Canvas cannot reference Controls (no project edge), so the coverage is
**mirrored** in `Elements.fs` using the same FNV-1a primitive and Canvas's own byte convention.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A paint-only change to a cached subtree repaints (Priority: P1)

A product author wraps an expensive fragment with `Elements.cached "hud" frag` and, on a later frame,
changes only a *paint* property inside that fragment (e.g. a stroke colour, a dash pattern, an arc
sweep, or glyph text) while keeping the same cache key.

**Why this priority**: This is the reported defect. Serving a stale frame is a silent visual-correctness
failure on the load-bearing replay path — the whole point of the cache is to be indistinguishable from a
full redraw.

**Independent Test**: Fully testable at the Canvas layer: build two single-node scenes that differ only
in a previously-ignored field, wrap each with the same `cached` key, and assert their `Fingerprint`s
differ. Fully verifiable end-to-end at the SkiaViewer layer via the existing replay-cache parity oracle
(enabled cache == disabled cache).

**Acceptance Scenarios**:

1. **Given** a cached subtree containing a stroked shape, **When** only its stroke colour changes under
   the same key, **Then** the boundary fingerprint changes (forcing a re-record / cache miss).
2. **Given** a cached subtree containing an arc, **When** only its sweep angle changes, **Then** the
   fingerprint changes.
3. **Given** a cached subtree containing a glyph run, **When** only its text changes, **Then** the
   fingerprint changes.
4. **Given** a cached subtree containing a dashed path, **When** only its dash pattern or fill-type
   changes, **Then** the fingerprint changes.

### Edge Cases

- **Option/list presence** — a field gaining/losing a value (`Fill: None → Some`) or a list changing
  length (empty gradient stops, empty vertex list) MUST move the fingerprint; the fold tags `None`/`[]`
  and mixes list length.
- **Nested cache boundaries** — a `CachedSubtree` nested inside a cached fragment continues to mix its
  own `CacheId` plus its content, so a nested key change is reflected in the outer fingerprint.
- **Determinism** — the fold remains pure and process-stable (FNV-1a constants + explicit byte/char
  folds, no `String.GetHashCode`), so an unchanged fragment hashes identically across runs and hits.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The cache-boundary fingerprint produced by `Elements.cached` MUST fold over every
  render-affecting field of every `SceneNode` case, including `Paint` and all paint sub-structures
  (fill, stroke, opacity, antialias, blend mode, shader, colour filter, mask filter, image filter, path
  effect).
- **FR-002**: The previously constant-collapsed node types — `FilledEllipse`, `Ellipse`, `Arc`,
  `Vertices`, `TextRun`, `GlyphRun`, `RegionNode`, `PictureNode` — MUST each hash their full field set.
- **FR-003**: Wrapper nodes MUST hash their transform/clip data, not only their child scene:
  `ClipNode` mixes its `Clip`, `PerspectiveNode` its `PerspectiveTransform`, `ColorSpaceNode` its
  `ColorSpace`.
- **FR-004**: `Path` MUST hash its `FillType`, and `ArcTo` commands MUST hash the full `bounds` rect
  (including `Height`).
- **FR-005**: The `SceneNode` `match` in the fold MUST be exhaustive (no wildcard `_` arm), so adding a
  new `SceneNode` case fails compilation until it is hashed.
- **FR-006**: The fold MUST remain pure and deterministic across processes/runs (no randomised hashing);
  an unchanged fragment MUST yield an identical fingerprint (a cache hit).
- **FR-007**: The change MUST NOT alter any public API surface. `Elements.cached`'s signature and the
  `Canvas` `.fsi` are unchanged; all new mixers are `private`.

### Key Entities

- **CacheBoundary** (`src/Scene/Types.fs`): `{ CacheId; Fingerprint; Scene }` — the replay-cache
  boundary. Only `Fingerprint` changes semantics here (it becomes comprehensive); the shape is untouched.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For each render-affecting field previously ignored or collapsed (paint colour, stroke,
  dash, arc sweep, path fill-type, ellipse bounds, glyph/text run text), a scene differing only in that
  field yields a different `Elements.cached` fingerprint. Proven by fail-before/pass-after tests.
- **SC-002**: The replay-cache parity oracle (enabled cache renders identically to disabled cache)
  holds across the existing SkiaViewer replay-cache suite — 0 regressions.
- **SC-003**: The full existing test suite passes with 0 new failures.
- **SC-004**: No public-surface drift (`Canvas` `.fsi` unchanged).

## Assumptions

- The boundary `Fingerprint` is an ephemeral, in-process cache key — no golden, snapshot, or persisted
  artifact asserts a specific numeric fingerprint value, so making the fold comprehensive (which changes
  the numeric values) is safe. Verified: the only fingerprint assertions in the suite test
  *inequality/determinism*, not literal values.
- Canvas keeps its own copy of the fold (mirroring `SceneHash`'s coverage) because there is no
  `Canvas → Controls` project reference and promoting a shared hasher into the `Scene` layer would widen
  public surface and touch Controls' byte-identical guarantee — out of scope for this fix. A single
  shared fold is recorded as a follow-up candidate (see `research.md`).
