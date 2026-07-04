# Phase 1 Data Model: FS.GG.UI runtime ergonomics polish

This feature adds almost no data — two no-op values and three guidance entries over existing types.
"Entities" here are the surfaces touched.

## E1 — `Cmd.none` / `Sub.none` (NEW public values, §3.5)

| Symbol | Type | Definition | Law |
|---|---|---|---|
| `FS.GG.UI.Controls.Elmish.Cmd.none` | `AdapterCommand<'msg>` | `[]` | `Cmd.none = ([] : AdapterCommand<'msg>)`; `AdapterCmd.productMessages Cmd.none = []` |
| `FS.GG.UI.Controls.Elmish.Sub.none` | `AdapterSubscription<'msg> list` | `[]` | `Sub.none = ([] : AdapterSubscription<'msg> list)` |

- `AdapterCommand<'msg> = AdapterEffect<'msg> list` (existing, `ControlsElmish.fsi:26`).
- `AdapterSubscription<'msg>` (existing; product `subscriptions` returns `AdapterSubscription<'msg> list`).
- Purely additive; behavior-identical to the current `[]` returns (FR-006). No generic constraints,
  no runtime cost.

## E2 — `KeyboardMsg` collision entry (guidance over EXISTING type, §3.4)

- Subject: `FS.GG.UI.KeyboardInput.KeyboardMsg` — `| KeyDown of KeyId | KeyUp of KeyId | …`
  (`KeyboardInput.fsi:78–87`). **Unchanged.**
- New: a `docs/product.md` collision-guidance line naming `KeyboardMsg.KeyDown` / `KeyboardMsg.KeyUp`
  as collision-prone with a product's own `Msg.KeyDown`/`KeyUp`, with the qualify-or-don't-open
  remedy, order-independent.

## E3 — `measureText` / `TextMetrics` HUD idiom (guidance over EXISTING helper, §3.6)

- `FS.GG.UI.Scene.measureText : string -> FontSpec -> TextMetrics` (existing, packed). **Unchanged.**
- `TextMetrics = { Width: float; Height: float; Baseline: float }` (`Types.fsi:187`). **Unchanged.**
- New: a documented self-positioning idiom — compute a HUD label's box/origin from
  `(measureText text font).Width`/`.Height` and the reserved HUD region, with **no** literal
  coordinate. Conservative calibration guarantees the box ≥ drawn glyph advance (SC-003).

## Surface / gate deltas

| Artifact | Delta |
|---|---|
| `src/Controls.Elmish/ControlsElmish.fsi` | +`module Cmd` (`val none`), +`module Sub` (`val none`) |
| `src/Controls.Elmish/ControlsElmish.fs` | +implementations `none = []` (paired with `.fsi`) |
| `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` | +`FS.GG.UI.Controls.Elmish.Cmd`, +`…Sub` |
| `template/base/src/Product/Model.fs` | `model, []` → `model, Cmd.none`; `[]` → `Sub.none` |
| `template/base/src/Product/EvidenceCommands.fs` | command no-op `[]` → `Cmd.none` |
| `template/base/docs/product.md` | +§3.4 collision line, +§3.6 measureText idiom, +§3.5 alias note |
| `template/product-skills/fs-gg-elmish/SKILL.md` | show `Cmd.none`/`Sub.none` in update/subscribe |
| `template/product-skills/fs-gg-scene/SKILL.md` (± `fs-gg-game-core`) | +HUD-measure pointer to `Scene.measureText` |

No packed `api-surface/` change (Scene already carries `measureText`; Controls.Elmish is
baseline-tracked, not packed). No version bump, no `Directory.Packages.props` change, no new package
reference.
