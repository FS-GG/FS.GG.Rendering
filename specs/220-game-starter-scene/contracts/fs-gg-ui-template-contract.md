# Contract — `fs-gg-ui-template` (template scaffold surface)

This is the versioned cross-repo template contract consumed by **SDD** (scaffold-provider) and
**Templates** (governance expectations). It defines the profiles, the per-profile generated
output guarantees, and the durable governance invariants. The change introduced by feature 220 is
**additive + a relaxation**, not a removal.

## C1. Profiles

| Profile | Contract |
|---|---|
| `game` (**NEW**) | Game/rendering default starter. Generated default launch (`dotnet run`, no flags) renders a **minimal interactive game-style scene** (Pong skeleton) explicitly designated replaceable. Selects the game `//#else` branches throughout `template/base`. Capabilities = `scene, skiaviewer, elmish, keyboard-input, layout, controls, full-governance`. |
| `app` | Explicit **controls showcase** option (unchanged behavior; re-described as opt-in, no longer "the" default). |
| `headless-scene`, `governed`, `sample-pack` | **Unchanged.** Generated output + tests byte-identical to pre-220 (FR-007). |

**Default-starter selection** (which profile a game/rendering scaffold gets by default) is owned
by the **SDD scaffold-provider** and changes from `app` → `game`. That flip is the coordinated
contract change (C5).

## C2. Default-entrypoint launch guarantee (family-agnostic)

At the normal entrypoint (`| None ->` branch of `Program.main`), the generated product launches
the **family-appropriate persistent interactive host**:

- controls family (`app`) → `ControlsElmish.runInteractiveApp viewerOptions interactiveHost`
- game family (`game`) → `Viewer.runApp viewerOptions generatedHost`
- `sample-pack` → `Viewer.runApp viewerOptions generatedHost` (**unchanged**)

The contract **no longer requires the product to retain a specific UI family's launch call**
(FR-003). A developer may replace the starter `Model`/`View` at this entrypoint and the launch
assertion remains satisfied by the family's persistent host — **no alternate launch flag**
(e.g. `-- pong`) is permitted as a precondition for green governance (FR-002 / FR-008).

## C3. Durable governance spine (must survive a starter swap)

`GovernanceTests.fs` asserts, model-agnostically, that the generated product source carries:
- the six scanned files in compile order
  (`Model.fs → View.fs → LayoutEvidence.fs → WindowOptions.fs → EvidenceCommands.fs → Program.fs`);
- `--scene-evidence` + `SceneEvidence.render` with `RendererMode = "deterministic-scene"`;
- the bounded smoke / launch / image / screenshot / pixel-readback evidence command surface and
  its honesty vocabulary (`visualEvidenceGuidance`);
- the family-appropriate persistent host in the default branch (C2);
- desktop/session diagnostics in the default branch without silent evidence fallback.

**Invariant (SC-004):** every assertion in the game `//#else` branch MUST be satisfiable by both
(a) the unmodified minimal skeleton and (b) a representative replaced game (Pong). No assertion
may pin a controls-only token in the game branch.

## C4. Replaceable vs durable classification (scaffold-map authority)

| Class | Files (game family) |
|---|---|
| Replaceable (rewrite on swap) | `<ProductDir>/Model.fs`, `<ProductDir>/View.fs`, `tests/Product.Tests/BehaviorTests.fs` |
| Durable — re-point model fields | `<ProductDir>/LayoutEvidence.fs`, `<ProductDir>/EvidenceCommands.fs` |
| Durable — untouched | `<ProductDir>/WindowOptions.fs`, `<ProductDir>/Product.fsproj`, `<ProductDir>/Program.fs`, `tests/Product.Tests/GovernanceTests.fs` |

`scaffold-map.md` MUST match this set exactly; no undocumented coupling may force edits beyond it
(FR-005 / SC-003 / SC-005).

> `WindowOptions.fs` and `Product.fsproj` are **conditional-only authoring** changes (the `game`
> profile is threaded into their existing package/compile gates once, in T014/T015). After
> instantiation the conditionals are resolved, so a developer swapping the starter **does not edit
> them** — they are durable-untouched at swap time, not "re-point model fields" (there are no model
> fields to re-point in window options / project metadata).

## C5. Cross-repo coordination obligations (FR-009)

1. **SDD (scaffold-provider):** enumerate the new `game` profile; switch the game/rendering
   default selection `app → game`. Must not break composition.
2. **Templates (governance):** expectations updated so the new default starter + relaxed
   entrypoint assertion are the accepted output.
3. **This repo:** ADR in `docs/product/decisions/`; contract/compatibility registry entry for
   `fs-gg-ui-template` updated; template republished at a coherent (preview) version; Coordination
   board issue filed and sequenced alongside sibling item **#32**.

## C6. Verification hooks (map to Success Criteria)

| Check | SC |
|---|---|
| Scaffold `game`, swap starter → Pong, build+test green, **0** governance-test edits | SC-001, SC-004 |
| `game` default launches the dev's scene at normal entrypoint, **0** hidden flags | SC-002 |
| Swap edit set ⊆ scaffold-map replaceable/re-point classification, **0** undocumented files | SC-003, SC-005 |
| `headless-scene`/`governed`/`sample-pack` generated output + tests diff = empty | SC-006 |
| `app` controls showcase still generates + passes governance | SC-006 (controls half) |
