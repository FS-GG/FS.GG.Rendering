# Contract: guidance surfacing (§3.4 collision + §3.6 measureText)

No F# surface. These are content contracts for `docs/product.md` and product skills — what MUST be
present for the discoverability requirements (FR-001, FR-004) to hold.

## G1 — §3.4 collision line (`template/base/docs/product.md`)

MUST add, in the existing collision guidance paragraph (beside `Text` / `CloseRequested` / `Rect` /
`ControlEventOrigin.Text`):

- A statement that `FS.GG.UI.KeyboardInput.KeyboardMsg` exports `KeyDown of KeyId` and `KeyUp of KeyId`,
  which collide with a product's own `Msg.KeyDown` / `Msg.KeyUp`.
- The remedy: qualify the framework cases (`KeyboardMsg.KeyDown`) or avoid an unqualified
  `open FS.GG.UI.KeyboardInput` where the product defines its own input messages — **independent of
  `open` order**.

**Check**: a scaffolded `game`/`sample-pack` product defining `Msg.KeyDown of KeyId` + a `mapKey`
returning it compiles when the author follows this line; `docs/product.md` names `KeyDown`/`KeyUp`
(SC-001, acceptance US1-1/US1-2).

## G2 — §3.6 measureText HUD idiom (`template/base/docs/product.md` + a product skill)

MUST add:

- A named pointer to `FS.GG.UI.Scene.measureText : string -> FontSpec -> TextMetrics` as the **pure,
  authoring-time** text metric (contrast with the render-edge shaping), noting `TextMetrics` carries
  `Width` / `Height` / `Baseline` and that the heuristic is conservative (box never narrower than
  drawn).
- A worked self-positioning snippet: place a HUD label (e.g. right-align a score within the reserved
  HUD band) computing its origin/box from `(measureText text font).Width`/`.Height` and the HUD
  region — **no literal coordinate**.
- The pointer named in at least one product skill (`fs-gg-scene`, cross-linked from `fs-gg-layout`
  and/or `fs-gg-game-core`).

**Check**: the snippet compiles in a scaffolded product and positions text with 0 magic numbers
(SC-002, SC-003, acceptance US2-1/US2-2). Any product `SKILL.md` touched keeps the skill-manifest /
currency gates green (FR-008).

## G3 — §3.5 alias note (`template/base/docs/product.md` + `fs-gg-elmish` skill)

MUST show a product `update` returning `model, Cmd.none` and `subscriptions` returning `Sub.none`,
with the one-line note that both equal `[]` and the qualified fallback if Fable.Elmish is also opened
(research D2). This is guidance for the E1 surface, not a separate contract.
