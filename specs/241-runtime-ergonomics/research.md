# Phase 0 Research: FS.GG.UI runtime ergonomics polish

All three items were reproduced against the current tree; the "unknowns" were remedy choices, not
unknown facts. Each decision below is grounded in a concrete surface.

## D1 — §3.4: how to remove the `KeyDown`/`KeyUp` collision

**Decision**: **Doc-only.** Add `KeyboardMsg.KeyDown` / `KeyboardMsg.KeyUp` to the existing
collision guidance in `template/base/docs/product.md`, beside the current `Text` / `CloseRequested`
/ `Rect` / `ControlEventOrigin.Text` entries. State that a product modelling its own
`Msg.KeyDown`/`KeyUp` must qualify the framework cases (`KeyboardMsg.KeyDown`) or not `open`
`FS.GG.UI.KeyboardInput` unqualified, independent of `open` order.

**Rationale**: The colliding constructor is `FS.GG.UI.KeyboardInput.KeyboardMsg`
(`KeyDown of KeyId` / `KeyUp of KeyId`, `KeyboardInput.fsi:78–80`) — a **shipped public contract
type**, not a viewer-internal one (the feedback's "ViewerKey" phrasing was imprecise; the observed
error `type 'KeyId' does not match 'ViewerKey'` is the framework `KeyboardMsg.KeyDown of KeyId`
being selected and handed a `ViewerKey` from the consumer's `mapKey`). Putting
`[<RequireQualifiedAccess>]` on `KeyboardMsg` would force **every** existing unqualified
`KeyDown`/`KeyUp`/`FocusLost`/… use across the KeyboardInput package, samples, and any live consumer
to qualify — a breaking public-surface change that violates FR-002's regression constraint for a
`Polish` item.

**Alternatives considered**:
- `[<RequireQualifiedAccess>]` on `KeyboardMsg` — rejected (breaking; blast radius across package +
  consumers; disproportionate to a Polish fix).
- Rename the consumer's message — not ours to mandate; the fix must live in the framework's guidance.
- A new non-colliding alias type — adds surface for no benefit; the doc line is sufficient and
  matches how the repo already handles `Text`/`CloseRequested`/`Rect`.

## D2 — §3.5: where the `Cmd.none` / `Sub.none` no-ops live

**Decision (refined during implementation — approved 2026-07-04)**: Add two public no-op values in a
dedicated sub-namespace **`FS.GG.UI.Controls.Elmish.Authoring`** (new files `Authoring.fsi`/`.fs`) —
`module Cmd { val none : AdapterCommand<'msg> }` and `module Sub { val none : AdapterSubscription<'msg> list }`,
each `[]`. Consume them in the product template's `update`/`subscriptions`, and surface in
`docs/product.md` + the `fs-gg-elmish` product skill.

> **Why the sub-namespace, not the root `FS.GG.UI.Controls.Elmish`.** Implementation revealed the
> package's `ControlsElmish.fs` does `open Elmish` and depends on Fable's `Cmd` (e.g.
> `let none: Cmd<'msg> = Cmd.none`, `toCmd ... : Cmd<'msg>`). A `module Cmd` declared in the ROOT
> `FS.GG.UI.Controls.Elmish` namespace is visible to every same-namespace file and would shadow
> Fable `Elmish.Cmd`, breaking the package build. Isolating the aliases in
> `FS.GG.UI.Controls.Elmish.Authoring` (which does not `open Elmish`) leaves the package untouched;
> a generated product opts in with `open FS.GG.UI.Controls.Elmish.Authoring` and — since it does not
> `open Elmish` — gets `Cmd.none`/`Sub.none` unambiguously. Verified by build + FSI + Expecto.

**Rationale**: `AdapterCommand<'msg> = AdapterEffect<'msg> list` and `AdapterSubscription<'msg>` are
defined in `FS.GG.UI.Controls.Elmish` (`ControlsElmish.fsi:26`; baseline
`FS.GG.UI.Controls.Elmish.txt`), and every generated product already references that package
(`Product.fsproj`). Hosting the no-ops there makes them a real, baseline-tracked capability that all
consumers get, versus template-only boilerplate that a consumer editing `Model.fs` would have to
carry by hand. The existing `AdapterCmd.none : Cmd<'msg>` is the **Elmish `Cmd`** no-op (a different
type used by `toCmd`), so it cannot double as the effect-list no-op — a distinct value is genuinely
needed (FR-003 "verify-before-adding" satisfied: checked, not present).

**Name-collision guard (self-consistency with §3.4)**: naming the modules `Cmd`/`Sub` could shadow
Fable.Elmish's `Cmd`/`Sub`. Generated FS.GG.UI products use the `AdapterCommand` model and do **not**
`open Elmish` in `Model.fs`, so `Cmd.none` resolves to ours unambiguously; the template proves it.
`docs/product.md` records the qualified fallback (`FS.GG.UI.Controls.Elmish` `open` gives `Cmd.none`;
a product that also opens Fable.Elmish qualifies) — consistent with the collision guidance this
feature is already extending.

**Alternatives considered**:
- Template-only local `module Cmd`/`Sub` in `template/base/src/Product/` — simpler (no library surface,
  no baseline churn) but not a durable capability; every hand-edited product re-owns it. Kept as the
  documented fallback if implementation surfaces a real Elmish-`Cmd` ambiguity in the package.
- Reuse `AdapterCmd` with a second `none` — impossible (F# forbids two `val none` of different types
  in one module).
- Non-Elmish names (`AdapterSub.none`) — loses the requested Elmish-convention readability that is
  the entire point of §3.5.

## D3 — §3.6: build a `measureText` or surface the existing one

**Decision**: **Surface the existing helper**;
`FS.GG.UI.Scene.measureText : string -> FontSpec -> TextMetrics` already ships as the pure,
host-independent heuristic (`Scene.fsi:135`; `TextMetrics = { Width; Height; Baseline }`,
`Types.fsi:187`) **and is already in the packed api-surface** (`api-surface/Scene/Scene.fsi:489`),
so a generated product can call it today (Scene is referenced on every profile). Add a worked HUD
self-positioning idiom to `docs/product.md` and name the helper in a product skill
(`fs-gg-scene`, cross-linked from `fs-gg-layout`/`fs-gg-game-core`). Do **not** author a new measurer.

**Rationale**: The friction was pure discoverability — the consumer used magic-number coordinates
because it believed only render-edge shaping existed. The capability is present and packed; the fix
is guidance (FR-004) with no duplication (FR-005). The consumer asked for `-> Size`, but `TextMetrics`
is strictly richer (adds `Baseline`); the idiom uses `.Width`/`.Height` directly, so a separate
`Size` projection is **not** added (avoids a second, thinner surface). The helper's documented
conservative calibration (never narrower than drawn) is what makes it safe for HUD box sizing (SC-003).

**Alternatives considered**:
- New `measureText : string -> FontSpec -> Size` — rejected (duplicates the existing measurer;
  FR-005). 
- A `Size`-shaped projection over `TextMetrics` — deferred/omitted; `.Width`/`.Height` suffice and
  the spec's FR-005 makes any projection a thin optional derivation only if a real need appears.

## D4 — surface & gate mechanics (applies to D2)

**Decision**: After adding the `.fsi` no-ops, run `scripts/refresh-surface-baselines.fsx` and commit
the two new lines (`FS.GG.UI.Controls.Elmish.Cmd`, `FS.GG.UI.Controls.Elmish.Sub`) to
`readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt`. `SurfaceAreaTests` reads that file and is
the Principle II drift gate. No packed `api-surface/` edit is needed: the Controls.Elmish surface is
not part of the packed tree, and `Scene.measureText` (D3) is already packed.

**Rationale**: The baseline file is the single authoritative surface location (writer =
`refresh-surface-baselines.fsx`, readers = `SurfaceAreaTests` + `PackageSurface.fs`), so an
undeclared export fails CI loudly — exactly the safe-failure the feature relies on for §3.5.
