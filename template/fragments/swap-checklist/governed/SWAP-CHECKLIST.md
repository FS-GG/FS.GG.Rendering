# SWAP-CHECKLIST — replace the starter model

This is your **model-swap to-do list**. The generated product ships a minimal headless-scene
starter plus a durable governance spine. When you swap in your own scene/model you rewrite
only the replaceable parts; the durable parts keep compiling and keep their source/evidence
scans green across the swap. Read [`docs/scaffold-map.md`](docs/scaffold-map.md) for the
durable-vs-replaceable rationale — this file is the precise symbol-level checklist that
projects it, so you don't have to rediscover the re-points from compiler errors.

> Paths use `<ProductDir>` = `src/<ProjectName>` (your generated tree names it after the
> project). The module name stays `Product.*`.

## 1. Rewrite wholesale (replaceable — these define/call the starter model directly)

- [ ] `<ProductDir>/Model.fs` — the starter `Model`/`Msg`/`update` (`Name`, `RenderCount`).
      Replace with your own model.
- [ ] `<ProductDir>/View.fs` — the starter `view` (`Model -> SceneNode`) reading `model.Name`
      and `model.RenderCount`. Replace with your own view.
- [ ] `tests/Product.Tests/BehaviorTests.fs` — the replaceable scaffold-behavior tests that drive
      the starter's `view`/`update` directly. Rewrite for your model.

## 2. Keep the file + re-point the model-field reads (durable must-re-point)

Keep these files and every must-survive evidence token they carry, but re-point the
model-field references at your own model. A purely additive swap (you only *add* a field)
leaves them untouched — see §4.

### `<ProductDir>/LayoutEvidence.fs`

- [ ] `layoutEvidenceForSize` — reads `model.Name` for the title text and renders `view model`

### `<ProductDir>/EvidenceCommands.fs`

- [ ] `layoutEvidenceCommand` / `sceneEvidence` — render from `initialModel` + `view`

## 3. Leave untouched (durable model-agnostic spine — reads no model field)

- `<ProductDir>/Program.fs` · `<ProductDir>/WindowOptions.fs` ·
  `tests/Product.Tests/GovernanceTests.fs` — pure plumbing; keep compiling across the swap.

## 4. Additive-swap note

If your swap only **adds** a model field (rather than changing the fields the §2 files read),
the durable re-point files need no edit — a purely additive change leaves
`LayoutEvidence.fs` / `EvidenceCommands.fs` untouched.
