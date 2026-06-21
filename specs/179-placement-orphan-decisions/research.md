# Research — Placement & Orphan Decisions (Feature 179, Phase 0)

The three decisions were confirmed with the owner; this phase resolves the **how** (exact homes,
reference inventory, surface-gate consistency) against the working tree. No `NEEDS CLARIFICATION`
remained after the spec; the one open fork (the `ColorPolicy` home) was confirmed by the owner
during planning (R3).

## R1 — Harness relocation: `tests/Rendering.Harness` → `tools/Rendering.Harness`

**Decision**: Move the project verbatim (all 39 `.fs`/`.fsi` files) to a new top-level `tools/`
directory and rewrite every **genuine** reference. The full old→new map lives in
`contracts/harness-path-map.md`. Categories (verified by ripgrep):

| # | Category | Sites |
|---|----------|-------|
| 1 | `.slnx` project entries | 2 (`tests/Rendering.Harness.Tests/…` is a *different* project — only the harness line moves; the Tests line keeps its own path) |
| 2 | Dependent test `ProjectReference` | `tests/Rendering.Harness.Tests` → `..\..\tools\Rendering.Harness\Rendering.Harness.fsproj` (depth changes) |
| 3 | Linked `TestAssertions.fs` includes | 4 projects (Layout/Scene/SkiaViewer/Controls.Tests), each `..\..\tools\Rendering.Harness\TestAssertions.fs` |
| 4 | Helper scripts | `check-agent-skill-parity.fsx`, `run-validation-lanes.fsx`, `refresh-local-feed-and-samples.fsx` — `ArgumentList.Add("tools/Rendering.Harness/Rendering.Harness.fsproj")` |
| 5 | Harness-internal command literals | `Compositor.fs` (2), `ValidationLanes.fs` (3), `Live.fs` (1) — see note below |
| 6 | Feature 170 lane-test assertion | `tests/Rendering.Harness.Tests/Feature170RetainedInspectionLaneTests.fs:27` (asserts the lane command string) |
| 7 | FSX evidence scripts (`specs/**`) | 5 files with `#r`/`open` at varying relative depth |
| 8 | Skill doc | `src/Diagnostics/skill/SKILL.md` (2 mentions) |

**Rationale**: `tests/` should hold test projects only (SC-001). `OutputType=Exe` + ~18.4k lines is
tooling, not a test. Doing the highest-touch move first under a captured baseline lets later stories
build on a known-good tree.

**Important distinction (from Feature 178's learnings)**: `tests/Rendering.Harness.Tests` (the *test*
project) does **not** move — only the *harness* CLI does. Several internal literals (category 5/6)
name `tests/Rendering.Harness.Tests/…Tests.fsproj` (the test project) and must stay pointing at
`tests/`, while literals naming `tests/Rendering.Harness/Rendering.Harness.fsproj` (the CLI) move to
`tools/`. Each category-5/6 literal is classified individually in `contracts/harness-path-map.md` so
the test-project path is **not** accidentally rewritten.

**Relative-path depth**: includes (`..\Rendering.Harness\…`) and FSX `#r`
(`../../../../tests/Rendering.Harness/bin/…`) encode depth from the consuming file. Moving the target
from `tests/` to `tools/` (both top-level) keeps the *number* of `..` segments the same for
same-depth consumers but changes the **leaf** segment (`tests`→`tools`). The dependent test project's
`..\Rendering.Harness\…` (sibling under `tests/`) becomes `..\..\tools\Rendering.Harness\…`
(one level up, then into `tools/`). Each site is recomputed individually, not by blind find/replace.

**Alternatives considered**: (a) leave under `tests/` — rejected, perpetuates the mis-file the spec
exists to fix. (b) `src/` — rejected, it is not a packaged library; `tools/` is the honest home for
an executable harness.

## R2 — Retire & unpublish `FS.GG.UI.Input`

**Decision**: Delete `src/Input/` (`Input.fsproj`, `KeyboardInput.fs` 1,400, `KeyboardInput.fsi` 452,
`README.md`) and `tests/Input.Tests/` (3 files); de-list both from `FS.GG.Rendering.slnx`; remove the
`"FS.GG.UI.Input", "Input"` row from `scripts/refresh-surface-baselines.fsx`; delete
`readiness/surface-baselines/FS.GG.UI.Input.txt`.

**Rationale**: `src/Input/` has **zero** production consumers (verified: no `src/*` project
references it; only `tests/Input.Tests/` does). The live keyboard path is `src/KeyboardInput/`
(`FS.GG.UI.KeyboardInput`), referenced by `SkiaViewer`, `Controls`, `Controls.Elmish`. Removing the
orphan removes a genuinely misleading second keyboard API.

**Surface-gate invariant (Tier 1)**: the drift gate reads `readiness/surface-baselines/` and the
manifest in `refresh-surface-baselines.fsx`; both must drop `FS.GG.UI.Input` in the **same** change so
the gate sees neither an orphaned baseline nor an unbaselined package (FR-006, SC-004). Details in
`contracts/package-surface-changes.md`.

**Migration impact**: this is a breaking change for hypothetical external consumers, accepted by the
owner. The existing `docs/bridge/package-deprecation-notice.md` and `package-identity-migration.md`
already list `FS.GG.UI.Input`; their notices remain valid (the package is now removed, not merely
renamed). `docs/usage.md:65`'s package inventory line is updated to drop it.

**Alternatives considered**: (a) keep but mark deprecated — rejected, the owner chose removal and the
duplicate keyboard API is the exact debt being retired. (b) re-point `Input.Tests` at
`KeyboardInput` — rejected, those tests assert the *old* package's surface; `KeyboardInput.Tests`
already covers the live path.

## R3 — Retire `src/Color/` while preserving `ColorPolicy` (owner-confirmed home)

**Refinement discovered in research**: the spec frames `ColorPolicy.fs` as the *only* live consumer
of `src/Color`, with `Contrast`/`Palettes` as dead public surface (FR-008 says both "MUST be
removed"). Verification showed `Contrast` is **not** dead:

- `ColorPolicy.fs` hard-depends on `Contrast.verdict`, `Contrast.ratio`, `Contrast.compositeOver`
  (lines 98, 151–152, 169) and on the `Role`/`Verdict` types `Contrast.fsi` declares.
- `Controls.Tests/Feature108ThemingTests.fs` uses `FS.GG.UI.Color.Contrast.ratio`/`verdict` and the
  `FS.GG.UI.Color.Text`/`GraphicOrUi`/`Aa`/`Fail` cases (i.e. `Role`/`Verdict`) directly, via the
  `ProjectReference` to `src/Color` (line 180).
- Only `Palettes` is truly dead — referenced solely by `tests/Color.Tests/PaletteTests.fs`.

So `Contrast` (with `Role`/`Verdict`/`ContrastResult`) cannot be deleted; it must **relocate**.

**Decision (owner-selected during planning)**: create a **new non-packed `src/ColorPolicy` project**
(`IsPackable=false`, no surface baseline) holding `Contrast.fsi` + `Contrast.fs` + `ColorPolicy.fs`
moved verbatim, with `<InternalsVisibleTo Include="Controls.Tests" />`. Keep the `FS.GG.UI.Color`
namespace so every consumer is **edit-free**. `Controls.Tests` repoints its `ProjectReference` from
`src/Color` to `src/ColorPolicy`. Delete `Palettes.fsi`/`Palettes.fs`, `tests/Color.Tests/`, and the
rest of `src/Color/`. Update the two policy scripts' `#load` paths
(`src/Color/Contrast.fs`→`src/ColorPolicy/Contrast.fs`, same for `ColorPolicy.fs`). Full contract in
`contracts/colorpolicy-relocation.md`.

**Rationale**: keeps `ColorPolicy` a "production-ish" library (preserving the documented Feature 127
F5 public-promotion path) without shipping it — `IsPackable=false` + no baseline ⇒ zero package
surface (FR-010). `src/ColorPolicy` depends only on `FS.GG.UI.Scene` (matching old `src/Color`), so
no new package-graph edge appears beyond the test-only `Controls.Tests → src/ColorPolicy` replacing
`Controls.Tests → src/Color`. Namespace preservation makes the move byte-identical at every call
site (SC-006).

**Coverage note (disclosed, Principle V)**: deleting `tests/Color.Tests/` drops the granular
`ContrastTests`/`PaletteTests`. `Palettes` is gone, so `PaletteTests` is moot. `Contrast` keeps
indirect coverage: Feature 108 pins `ratio`/`verdict` against WCAG reference pairs, and Feature 127
drives `ratio`/`compositeOver`/`verdict` through `evaluatePairing`. If the owner wants the granular
`Contrast` unit tests retained, re-homing `ContrastTests.fs` into `Controls.Tests` is a trivial
bounded follow-up — **out of scope** here per FR-008 + the chosen option.

**Alternatives considered**:
- **Fold Contrast+ColorPolicy into `Controls.Tests`** — least churn, drops the IVT grant entirely,
  fully deletes `src/Color`. Rejected by the owner: it makes the modules test-only and complicates
  the documented future F5 promotion.
- **Fold into shipped `src/DesignSystem`** — rejected: bloats a shipped assembly with test-support
  logic and contradicts Feature 127's explicit intent to keep this *out* of DesignSystem.
- **Keep a slimmed `src/Color` (Contrast+ColorPolicy only)** — rejected: FR-008 retires the `Color`
  package identity; reusing the name/project would leave a half-retired orphan and a dangling
  `Palettes`-shaped gap.

## Cross-cutting: baseline & evidence

**Decision**: capture `dotnet build` + `dotnet test` over `FS.GG.Rendering.slnx` **before any
change** (`readiness/baseline.md`), recording the green count and the two documented pre-existing
package-feed reds (Package.Tests, ControlsGallery package-feed). Diff after each story; the two reds
must remain the **only** non-green entries (FR-011, SC-005). This is the sole "real evidence" the
feature can produce — there is no runtime behavior to smoke (see plan's standing-assumption note).

**Sequencing**: US1 (harness, highest touch) → US2 (Input, clean delete) → US3 (Color, most
delicate). Each is independently shippable and diffed against the one baseline.
