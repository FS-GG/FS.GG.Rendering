# 0104 — the fixed-step double buffer is a simulation primitive; `FS.GG.UI.Canvas.Loop` is deprecated

**Status**: Accepted · **Date**: 2026-07-10 · **Issue**: [FS.GG.Rendering#269](https://github.com/FS-GG/FS.GG.Rendering/issues/269)

> **Numbering.** Repo-local ADRs resume at `0100` (see [ADR-0100](./0100-gate-is-a-required-check.md)).
> This one follows [ADR-0103](./0103-gate-is-fully-enforced.md).

## Context

FS.GG.Game asked ([#269](https://github.com/FS-GG/FS.GG.Rendering/issues/269), pairing with
FS.GG.Game#44) that `FS.GG.UI.Canvas.Loop` be deprecated in favour of `FS.GG.Game.Core.Loop`, and
explicitly invited the opposite answer: *"If Rendering still believes the classification is right, say
so on this issue and we will close it — that is a legitimate answer, and it is the answer we would like
recorded either way."* This ADR is that record.

The classification under challenge lives in a **`.fsproj` comment**, not in an ADR:

> Canvas now carries only the render-adjacent surfaces: pure Elements, **the render Loop**, and the
> Persistence request vocabulary.

[ADR-0022](https://github.com/FS-GG/.github/blob/main/docs/adr/0022-extract-fs-gg-game-as-an-sdd-driven-component.md)
P5 moved `FixedStep.drain` down to `FS.GG.Game.Core` and **never mentions `Loop`**. So "the render
Loop" is an implementation-time judgement that nobody ratified, and it is the reason the double buffer
stayed upstream when the accumulator it is built on moved down.

The judgement is wrong on the merits. `Loop.advance` contains no rendering — it is `FixedStep.drain`
plus a fold retaining the previous world. `StepState.Previous` is the world one tick ago: simulation
state. Only `alpha` is render-adjacent, and it is `Accumulator / dt`, arithmetic whose *consumer* is
the renderer exactly as the consumer of `FixedStep.drain` is the renderer.

Two costs, both already paid:

1. **`FS.GG.Game.Core` is BCL-only and reaches up to nothing (ADR-0022 §2), so a headless deterministic
   simulation cannot use the double-buffered loop.** It gets `FixedStep.drain` and must hand-roll
   `Previous`/`Current`/`alpha`. The samples in this repo do not have to — they take a
   `ProjectReference` on `Canvas` and call `Loop` directly — but both still hand-write the *world*
   interpolation `alpha` feeds, and `samples/CanvasDemo/Game.fs` says why in a comment: *"no framework
   lerp/interpolation API — `Loop.alpha` supplies only the factor"*. `samples/SymbologyBoard/Board.fs`
   writes its own for the same reason. A product that cannot reference `Canvas` hand-rolls the buffer
   too.
2. **Two implementations of one accumulator, in two repos — and they diverged on the thing that
   matters.** `FixedStep.drain` was hardened against non-finite input and documents it; `Loop.advance`
   propagated `NaN` and froze the simulation permanently ([#266](https://github.com/FS-GG/FS.GG.Rendering/issues/266)).
   **The hardened one was not the one products used.** That is the whole argument, delivered as a bug.

## Decision

1. **Accept the reclassification.** The fixed-step double buffer is a **simulation** primitive, owned by
   `FS.GG.Game.Core.Loop`. This ADR supersedes the `Canvas.Lib.fsproj` classification, which is
   corrected in place to point here.
2. **Deprecate `FS.GG.UI.Canvas.Loop` and `StepState`** in their doc comments now, naming
   `FS.GG.Game.Core.Loop` as the replacement and this ADR as the authority.
3. **No `[<Obsolete>]` attribute yet.** See below — the replacement is not reachable.
4. **No re-export.** `FS.GG.UI.Canvas` does not grow a `ProjectReference`/`PackageReference` to
   `FS.GG.Game.Core`. Requested explicitly by FS.GG.Game, and correct: `Canvas.Lib.fsproj` has exactly
   one `ProjectReference` (to `Scene`), and a re-export would create a package edge for one type and
   three functions.
5. **Retire at the next `FS.GG.UI.Canvas` major**, with a registry flip and a `CHANGELOG` entry. Canvas
   is `0.4.0-preview.1`, so the retirement lands at `0.5.0`.

### Why a doc-comment deprecation and not `[<Obsolete>]`

Because **the migration target ships in no published package.** `FS.GG.Game.Core.Loop` merged as
FS.GG.Game#61 at `2026-07-10T11:28Z`; `FS.GG.Game.Core` `0.2.0` was tagged at `2026-07-09T17:19Z` and
published at `17:20Z`, roughly eighteen hours earlier. `src/Game.Core/` at tag `v0.2.0` contains no
`Loop.fs`. The published feed offers `0.2.0` and `0.1.0-preview.1`; neither has it.

An `[<Obsolete>]` attribute is a compiler-enforced instruction to migrate. Emitting one that points at
a type no consumer can reference is a false instruction: it reddens or noises every downstream build
while offering no fix. The issue anticipated this and sanctioned the alternative — *"or a doc-comment
deprecation if `Obsolete` is too loud for a preview package"*.

The attribute is not abandoned, it is **sequenced**: it lands with the same change that migrates the
samples, once a `FS.GG.Game.Core` release carrying `Loop` is on the feed. Tracked as a follow-up on
this issue's epic. The doc comment says so, so the deferral cannot be mistaken for an oversight — which
is the failure mode this ADR exists to correct.

### The blast radius is narrower than the issue feared

FS.GG.Game asked us to *"confirm which profiles actually materialize `FS.GG.UI.Canvas` for non-game
products — if `app`/`headless-scene`/`governed` consumers use `Loop` today, the retirement in step 5
needs a longer runway."*

**None do.** `template/base/src/Product/Product.fsproj` and `template/base/Directory.Packages.props`
both guard the `FS.GG.UI.Canvas` reference with the same condition:

```xml
<!--#if (profile == "game" || profile == "sample-pack") -->
```

`FS.GG.UI.Canvas` is materialized on **`game` and `sample-pack` only** — the two profiles that already
pin `FS.GG.Game.Core` in the very same guarded block. `app`, `headless-scene` and `governed` never
reference the package at all, so they cannot be using `Loop`. Step 5 needs **no extended runway**: at
the Canvas major, every consumer that can see `Loop` can already see `FS.GG.Game.Core.Loop`.

## Consequences

### The samples cannot migrate in this change

`samples/CanvasDemo/Game.fs`, `samples/SymbologyBoard/Board.fs` and `tests/Canvas.Tests` consume
`Loop.init`/`advance`/`alpha` and keep doing so. Wherever `FS.GG.Game.Core` is consumed in this repo it
is consumed as a **package** — `tests/Canvas.Tests` takes a `PackageReference`, and
`Directory.Packages.local.props` pins `0.1.0-preview.1` — so nothing here can reach a `Loop` that
exists only on FS.GG.Game's `main`. (The two samples do not reference `FS.GG.Game.Core` at all today;
migrating them adds that reference, which is a package bump, not an edit.) Sample migration is step 4
of the issue's sequence and is deliberately out of scope here: it is blocked on the release, not on the
decision.

`samples/SymbologyBoard/` is also inside the live touch-set of #285 (ADR-0021), so it could not be
edited here in any case.

### The public surface does not move

The deprecation is doc comments and a `.fsproj` comment. `readiness/surface-baselines/FS.GG.UI.Canvas.txt`
and its `members/` sibling record type and member signatures, not attributes or documentation — so no
`CP0002` and no `CompatibilitySuppressions.xml`. (When the `[<Obsolete>]` attribute does land it is
still additive metadata, not a signature change.)

Verified rather than assumed: `scripts/refresh-surface-baselines.fsx` was run against a full Debug
build of the solution and rewrote every baseline byte-identically — `git status readiness/` is clean.

### The template's bundled `.fsi` is updated with the source

`template/base/docs/api-surface/Canvas/Loop.fsi` is a copy of `src/Canvas/Loop.fsi` that
`Feature204LifecycleTemplateTests` marks `copyOnly` (`docs/api-surface/**`), i.e. copied **verbatim**,
unprocessed by the template engine, into the scaffolded product root. It is what a consumer actually
reads. `ApiSurfaceMirrorTests` byte-compares only the `Controls.Elmish` copies (`inRepoExactCopies`),
so nothing would have caught this mirror going stale. Both files are updated together, and remain
byte-identical.

Leaving the mirror behind would have reproduced this ADR's own subject exactly: a stale classification,
shipped to consumers, contradicted by nothing and believed by everyone.

### `#266` is unaffected

The NaN-totality fix stays in `Loop.advance` for as long as the module exists. A deprecated primitive
that freezes the simulation is worse than a deprecated primitive.
