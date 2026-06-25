---
phase: implement
date: 2026-06-25
severity: minor
---

## Process friction
The implement phase went smoothly — the `samples/CanvasDemo` precedent is close enough to the M6 sample
that `Game.fs`/`Program.fs` mapped almost line-for-line onto `Board.fs`/`Program.fs`, and the
`board-core.md`/`cli-contract.md` contracts pinned the exact module shapes and canonical output strings up
front so there was no guesswork. The only real friction: the docs describe the test project as "xUnit-style"
but the actual repo convention (and the `tests/Canvas.Tests` precedent) is **Expecto + FsCheck** — the
`plan.md`/`tasks.md` wording could mislead. The `quickstart.md` also refers to a "documented different-seed
invocation (see cli-contract.md)" but the CLI exposes no `--seed` flag; seed-divergence is actually shown by
`Board.evidence 2` (the T015 test / a script over the built assemblies), not a subcommand — the cross-doc
pointer is slightly circular. What would have helped: the test-framework name stated once in plan.md, and the
different-seed pointer naming `Board.evidence`/the divergence test rather than a non-existent CLI invocation.

## Generalizable code
none. The sample is a pure consumer of existing public surfaces (`Symbology`/`Canvas`/`Scene`/`Controls`/
`SkiaViewer`). The one piece of reusable shape — a deterministic seeded fixed-timestep board sim with
Previous→Current interpolation — is deliberately duplicated from `CanvasDemo` per the Tier-2 sample
convention (samples are illustrative, not a shared library); promoting it would be a separate decision. The
`jitter` int-hash helper for seeded positions is small and sample-local; no library candidate.

## Skill gaps
none. `fs-gg-symbology` (channel grammar → `animate`), `fs-gg-scene` (`SceneCodec` canonical bytes),
`fs-gg-product-skiaviewer` (`runtimeCapability` headless gate), and `fs-gg-diagnostics` (readiness evidence
rules) covered every surface this sample touches.

## Research links
research blocked — none needed; all surfaces were in-tree and FSI-validated by specs 191/192.
