# Feature 187 — implementation-phase feedback (2026-06-22)

## Process / planning friction (high-value, generalizable)

1. **The plan's "bodies out, contracts stay" premise only holds for *self-contained* bodies.**
   It worked perfectly for US3's pure wire codec and US1's pure input-queue/responsiveness encoders
   (verbatim move to a pre-compiled `module internal` + delegators). It does **not** hold for the
   bulk of `SkiaViewer.fs`/`OpenGl.fs.run`, which is a closure over many live-path mutables: those
   bodies can't move *before* the public `.fs` (back-edge) and can't hoist to module helpers without a
   state-record rewrite — which collides head-on with the spec's own "do not reorder float
   accumulation / present sequencing" invariant. **Generalizable rule for future decomposition specs:
   classify each target body as "self-contained" vs "live-path-stateful" *before* committing a file-
   split task; only the former is behavior-preservingly relocatable.** A 5-minute dependency probe
   (`grep` each candidate for forward refs + module mutables) would have flagged this at plan time.

2. **data-model.md asserted `Tag: byte` for the node codec; the actual wire writes `Int32` tags**
   (`writer.Write(0)` + `reader.ReadInt32()`). Following the doc literally would have broken SC-004
   byte-identity. Design docs that name concrete encodings must be checked against the code, not memory.

3. **The read-side codec table already existed** (`sceneNodeCodec`/`readerByTag`) from prior work — the
   tasks assumed a hand-aligned write/read pair to convert. US3's real remaining deliverable was the
   *file split*, not the Pattern-A conversion. Re-confirming "current state" before authoring tasks
   (the standing note) would have right-sized US3.

4. **Module-name vs type-name collisions.** `module internal ViewerInputQueue` collided with the
   `ViewerInputQueue` *type* in `Viewer.Types`. Internal helper modules carved from a type-heavy module
   need a disambiguating name (used `ViewerInputQueueOps`). Worth a one-line note in the split recipe.

## Generalizable-code candidates

- The `codec-corpus.fsx` hash harness (export N representative scenes → sha256) is a reusable
  byte-identity oracle for any future Scene/codec refactor; consider promoting to `scripts/`.

## What went well

- The surface-baseline gate (`refresh-surface-baselines.fsx` + `git diff --exit-code`) is an excellent,
  fast FR-007 oracle — caught the `module internal` vs public-leak question immediately and confirmed
  byte-identical surface in seconds.
- `GetExportedTypes()`-based baseline means `module internal` helpers are invisible by construction —
  the SceneRenderer/Numeric precedent generalized cleanly.

**Severity:** medium (planning-accuracy, not correctness — the oracles caught every deviation).
