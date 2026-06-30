# Scaffold map — durable vs replaceable (read before you design)

This generated product ships a **minimal, replaceable Pong-style game starter** plus a **durable
governance spine**. When you replace the starter with your own game/UI, you rewrite only the
replaceable parts; the durable parts keep compiling and keep their source/evidence
scans green across the swap. Read this map **before** you start designing, so you
know what survives and what you own.

> **Project-named paths.** A generated tree puts the product under
> `src/<ProjectName>/**` (e.g. `src/Invoice1/**`, `src/Spread1/**`), not a literal
> `src/Product/**`. Below, `<ProductDir>` means that directory — `src/<ProjectName>`
> — so every cited path matches your generated tree verbatim. The module name stays
> `Product.*` even though the directory is project-named.

## Replaceable `<ProductDir>/**` (rewrite when you swap the scaffold model)

These call/define the starter game model directly — they are yours to replace:

- `<ProductDir>/Model.fs` — the starter `Model`/`Msg`/`update` (the Pong state machine).
- `<ProductDir>/View.fs` — the starter `view` (`Model -> SceneNode`).
- `tests/Product.Tests/BehaviorTests.fs` — the replaceable scaffold-behaviour tests that drive
  the starter's `view`/`update`/`tick`/host directly (the test-split detail is below).

## Durable — model-agnostic (keep, do not touch)

These are pure plumbing with **no scaffold model references**; they keep compiling
unchanged across a model swap — do not rewrite them:

- `<ProductDir>/Program.fs` — host/CLI entry point (`Viewer.runApp`, the
  `--scene-evidence` / evidence command wiring). Host wiring only.
- `<ProductDir>/WindowOptions.fs` — window-options parsing/diagnostics. Model-agnostic; the
  `game` profile is threaded into its package/compile gate once at template-authoring time, so
  after instantiation it carries **no** model-field reads — you do not touch it on a swap.
- `<ProductDir>/Product.fsproj` — compile order + package set. The profile conditionals are
  already resolved in your generated tree, so there is nothing model-specific to re-point.
- `tests/Product.Tests/GovernanceTests.fs` — reads the product **source text** and
  asserts structural / evidence / discoverability invariants; never calls the
  product's `view`/`update`, so it **survives a starter swap** (you never edit it to make a
  replacement pass).

## Adding new source files is fine (keep the six scanned files' relative order)

You may add your own source files (e.g. a pure engine module) without breaking the
governance gate. `GovernanceTests.fs` asserts only the **relative compile order** and
presence of the six scanned scaffold files —
`Model.fs → View.fs → LayoutEvidence.fs → WindowOptions.fs → EvidenceCommands.fs → Program.fs`.
A new file inserted **before, between, or after** them is safe as long as those six keep
that relative order in the `.fsproj`. (You do not need to read the governance test body to
confirm this.)

## Durable — must re-point (keep the file + its scanned tokens, re-point model fields)

These are **durable** — keep the file and every must-survive source-scan token it
carries — **but** they read scaffold *model fields*, so on a model swap you must
**re-point those model-field references** at your own model. "Durable" here means
*keep the file and its scanned evidence tokens while re-pointing the model-field references* —
it does **not** mean "do not touch":

- `<ProductDir>/LayoutEvidence.fs` — layout-region bounds evidence; reads the
  scaffold's HUD/gameplay regions. Re-point the region computations at your own
  layout, keeping the evidence tokens.
- `<ProductDir>/EvidenceCommands.fs` — the deterministic `SceneEvidence.render`
  evidence command (`RendererMode = "deterministic-scene"`); renders the starter
  scene. Re-point it at your own `view`, keeping the command surface and tokens.

> You only touch the re-point files when your swap **changes the specific model fields they read**
> (the starter's HUD/gameplay regions or `view`/`initialModel`). A purely additive swap — e.g.
> adding a new model field — leaves them untouched, as the feature-220 swap evidence shows.

## The test split: `GovernanceTests.fs` durable, `BehaviorTests.fs` replaceable

`tests/Product.Tests/` compiles `GovernanceTests.fs` **first** and
`BehaviorTests.fs` **after** (see `Product.Tests.fsproj`):

- **`GovernanceTests.fs` — durable, model-agnostic.** See above — do not rewrite it.
- **`BehaviorTests.fs` — replaceable scaffold-behavior.** Calls the scaffold
  product's `view`/`update`/host/scene-text directly. When you replace the
  scaffold model with your own, you **rewrite this file**; `GovernanceTests.fs`
  keeps passing.

## Worked example: remap the layout regions onto a non-game UI

`LayoutEvidence.fs` describes the scaffold as a **HUD** region and a **gameplay**
region. For a non-game UI you keep the file and its evidence tokens and re-point the
two regions onto your own surface:

- **HUD region → headers / toolbar.** The scaffold's status HUD becomes your app's
  header bar, toolbar, or command strip — the persistent chrome around the content.
- **Gameplay region → main content grid.** The scaffold's gameplay field becomes
  your main content area — e.g. the invoice line-item table or the spreadsheet
  cell grid — the scrollable region the user actually edits.

So an invoice builder maps HUD→the invoice header/toolbar and gameplay→the
line-item grid; a spreadsheet editor maps HUD→the formula/menu bar and
gameplay→the cell grid. Keep the region names' **evidence tokens** intact; only the
bounds computations re-point.

## Must-survive source-scan strings (keep these tokens present)

`GovernanceTests.fs` (and the framework's generated-guidance scans) assert these
strings remain in the product source across any model swap — keep them present
when you re-point the durable files:

- `--scene-evidence` and `SceneEvidence.render` (the deterministic scene evidence
  command) with `RendererMode = "deterministic-scene"`.
- The visual-evidence honesty vocabulary (decodable image; image dimensions;
  non-trivial content; renderer mode; fallback classification; unsupported reason;
  "metadata-only reports do not satisfy visual proof"; "1x1 fallback images do not
  satisfy visual proof"; benign/blocking/deferred warning; name-collision
  guidance) carried in `GovernanceTests.visualEvidenceGuidance`.

## API surface authority: the shipped `.fsi` / `docs/api-surface/` is ground truth

When you need to know a framework API's real shape, the **authoritative** reference
is the shipped `.fsi` signature files and the generated `docs/api-surface/` tree —
they are the curated public contract the packages actually expose. An
**agent-generated API summary** (e.g. an Explore/grep digest, or a hand-written
"here's what the API looks like" note) is **supporting reference only, never ground
truth**: it can silently mix confirmed signatures with inferred or stale shapes.
Always reconcile any agent-produced API summary against the `.fsi` / `docs/api-surface/`
before you design against it; when they disagree, the `.fsi` wins.

> **Typed front door is absent from `docs/api-surface/` (feature 085, FR-013).** The
> generated `docs/api-surface/` tree exposes only the **legacy builder** surface
> (`TextBlock.create`, `Button.create`, `Stack.children`, …). The **typed** front door
> (`FS.GG.UI.Controls.Typed.*` — immutable `Props` records + `view`) is **not** listed
> there, so "it's not in `docs/api-surface/`" does **not** mean it's unavailable. To
> enumerate the typed surface, read the **package** (`FS.GG.UI.Controls.dll` →
> `FS.GG.UI.Controls.Typed` modules) or the catalog's per-control **`module:`** field in
> `catalog.yml` (e.g. `module: TextBlock`) — that is the authoritative typed-front-door probe,
> not `docs/api-surface/`. See the `fs-gg-ui-widgets` skill's consumer note.

> **Interactive host seam is present in the package, not in `docs/api-surface/` (feature 108,
> FR-019).** The persistent interactive launch seam — `Controls.Elmish.runInteractiveApp`, the
> `InteractiveAppHost<'model,'msg>` record (incl. the feature-108 additive `MapKeyChord` /
> `OnFrameMetrics` fields), `PointerInteraction`, and the pure `Perf.runScript` frame driver — lives
> in the **`FS.GG.UI.Controls.Elmish`** package and its `ControlsElmish.fsi`. It is **not** mirrored
> under `docs/api-surface/` (which tracks the lower SkiaViewer / Controls surfaces), so "it's not in
> `docs/api-surface/`" does **not** mean it's unavailable. The **authority** for this seam is the
> `fs-gg-ui-widgets` skill + `ControlsElmish.fsi`; reconcile any summary against those. Focus
> visibility on this seam is the public `Focus.markFocused model.Focused (view …)` call inside `view`.

## Resolution-independent rendering: windowed-fullscreen blur (feature 085, FR-010)

The default window startup is **windowed fullscreen**, which scales a fixed-resolution scene
up to the monitor work area and **blurs** it. Two fixes: render with a **size-aware view**
(`InteractiveAppHost.View: Size -> 'model -> Control<'msg>`, content laid out to the actual
swapchain extent — the preferred path), **or** launch with exactly one flag —
`--window-startup normal` — for a 1:1 sharp normal window. See the `fs-gg-skiaviewer` skill.

## Pre-design pointer: record-label collision (fs-gg-scene)

**Before** you design your model's records, read the `fs-gg-scene` skill's
**Common pitfalls → record-label collision** note. Scene point/rect literals use
the labels `X`/`Y`/`Width`/`Height`; if your own model declares a record with the
same labels, a bare literal can infer to the wrong record type. Plan your record
label names (or annotate/qualify) up front — it is far cheaper than reworking the
model after the inference errors appear.
