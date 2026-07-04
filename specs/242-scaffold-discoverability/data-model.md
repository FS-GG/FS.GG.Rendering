# Data model: Scaffold discoverability sharpening (spec 242)

The two artifacts are content, not runtime types. This file pins their exact shape and the per-profile symbol inventory the `SWAP-CHECKLIST.md` variants must carry. Symbol lists are derived from `template/base/src/Product/{Model,View,LayoutEvidence,EvidenceCommands}.fs` and are the ground truth the template-authoring test (Decision 4/5) checks against.

## Entity: SWAP-CHECKLIST.md (per model family)

Root-level generated doc. Shared skeleton across families; the **re-point symbol tables** differ. Skeleton:

1. **Purpose + pointer** — one paragraph: "this is your model-swap to-do list"; link to `docs/scaffold-map.md` for the durable-vs-replaceable rationale; restate the rule that must-survive evidence tokens stay present.
2. **Rewrite wholesale (replaceable files)** — checklist of files you replace entirely.
3. **Keep + re-point (durable must-re-point files)** — per file, the specific symbols that read a `Model` field, as checkbox items.
4. **Leave untouched (durable model-agnostic spine)** — named so the reader knows the boundary (no action).
5. **Additive-swap note** — if you only *add* a model field, the re-point files can stay untouched (matches scaffold-map).

### Family: game

**Rewrite wholesale**: `src/<ProductDir>/Model.fs` · `src/<ProductDir>/View.fs` · `tests/Product.Tests/BehaviorTests.fs`

**Keep + re-point — `LayoutEvidence.fs`** (reads game model fields):
- `activeGameplayBoundsForSize` — reads `model.Ball.CenterX`, `model.Ball.CenterY`, `model.PlayfieldWidth`, `model.PlayfieldHeight`
- `spawnUsesGameplayRegion` — reads `initialModel.Ball`
- `scoreTextBounds` — reads `model.TickCount`, `model.LeftScore`, `model.RightScore`
- `layoutEvidenceForSize` — assembles the report from the above
- `movementUsesGameplayRegion` / `collisionUsesGameplayRegion` — read the active-item bounds
- `validateGeneratedLayout` — validates the assembled report

**Keep + re-point — `EvidenceCommands.fs`**:
- `mapKey` — wraps input as `ViewerInput(key, isDown)` (the keyboard→Msg seam the consumer found via errors)
- `layoutEvidenceCommand` / `sceneEvidence` — render from `initialModel` + `view`

### Family: app (profiles `app`, `sample-pack`)

**Rewrite wholesale**: `src/<ProductDir>/Model.fs` · `src/<ProductDir>/View.fs` · `tests/Product.Tests/BehaviorTests.fs`

**Keep + re-point — `LayoutEvidence.fs`**:
- `contentLayout` — derives the content grid geometry
- `activeGameplayBoundsForSize` — reads `model.ContentColumn`, `model.ContentRow`
- `spawnUsesGameplayRegion` — reads the reset cursor (`ContentColumn = 0; ContentRow = 0`)
- `hudTextBounds` — reads `model.ItemCount`, `model.Step`, `model.NextLabel`, `model.Page` (via `pageName`)
- `layoutEvidenceForSize`, `movementUsesGameplayRegion` / `collisionUsesGameplayRegion`, `validateGeneratedLayout`

**Keep + re-point — `EvidenceCommands.fs`**:
- `mapKey` — wraps `ViewerInput`
- `layoutEvidenceCommand` / `sceneEvidence` — render from `initialModel` + `view`

### Family: governed (profiles `governed`, `headless-scene`)

**Rewrite wholesale**: `src/<ProductDir>/Model.fs` · `src/<ProductDir>/View.fs` · `tests/Product.Tests/BehaviorTests.fs`

**Keep + re-point — `LayoutEvidence.fs`**:
- `layoutEvidenceForSize` — reads `model.Name` (title text) and renders `view model`

**Keep + re-point — `EvidenceCommands.fs`**:
- `layoutEvidenceCommand` / `sceneEvidence` — render from `initialModel` + `view`

### Leave untouched (all families — the durable model-agnostic spine)

`Program.fs` · `WindowOptions.fs` · `tests/Product.Tests/GovernanceTests.fs` — read no model field; keep compiling across a swap.

## Entity: Build-target help banner (single, profile-agnostic)

Emitted by `build.fsx` on bare `help` (fsi reserves `--help`/`-h` on the script path) and by `build.sh` on `--help`/`-h`/`help` at the shell level. Content (must agree with `docs/product.md:181-217`):

| Target | Semantics stated in the banner |
|---|---|
| `Dev` | Completion-marker / log-writer only — writes `readiness/logs/Dev.txt`; **does not compile**. A green `Dev` is not proof the product builds. |
| `Test` | First real compile + `dotnet test` (audit-free); use mid-implementation. |
| `Verify` | Runs the merge-gate audit (`EvidenceGraph` → `EvidenceAudit`) **first**, which **hard-blocks until every task is `[X]`**, then runs the tests. |
| `Restore`/`Build`/`Run`/`Pack` | Pass-through to stock `dotnet` over the single root `.slnx`. |

Behavioral invariants: printing the banner runs no target, writes no `readiness/logs/*.txt`, and exits `0`.

## Validation rules (checked by tests)

- **Presence (generated product, `GovernanceTests.fs`)**: `SWAP-CHECKLIST.md` exists at product root; contains the strings `LayoutEvidence.fs`, `EvidenceCommands.fs`, `Model.fs`, `View.fs`, and a `scaffold-map.md` reference. Structural only.
- **No-phantom + coverage (template gate, `SwapChecklistTemplateTests`)**: every symbol named in each family's checklist exists in the corresponding `template/base/src/Product/*.fs` profile branch; every durable re-point *function* enumerated in this data-model appears in that family's checklist.
- **Banner semantics (template gate + `GovernanceTests.fs`)**: banner string contains the `Dev`/`Test`/`Verify` load-bearing phrases; the same phrases appear in `docs/product.md`; the help path writes no log and exits 0.
