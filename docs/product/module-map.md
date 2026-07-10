# Product / module map

> The authoritative answer to **"what does the rendering product own, and which shipped
> package owns it?"** The source has been imported (the archived `EHotwagner/FS-Skia-UI`
> migration completed); this map now records the **shipped** F# assemblies and the
> `FS.GG.UI.*` packages that carry them. The `Package` column is verified against the
> packable projects in `FS.GG.Rendering.slnx` by `Feature242DocsCurrencyTests` — a new
> packable product cannot land without a row here, and a retired one cannot linger.

## Ownership boundary

FS.GG.Rendering owns the F# UI framework as a product: the retained **scene** and drawing
primitives (colour lives in the scene vocabulary and the design-system colour roles),
**layout**, **pointer and keyboard input**, the **SkiaSharp-over-OpenGL viewer/host**,
**Elmish/MVU integration**, the **semantic control set**, the **design-system / theme / kit**
layers that style and compose those controls, **diagnostics**, **canvas** primitives,
**unit symbology**, **governance evidence gates**, **testing helpers**, and the **`dotnet new`
template** that generates consumer apps. It does **not** own a Vulkan backend or any
skill machinery (see [Exclusions](#exclusions)).

The four UI layers (controls, design-system primitives, themes, design-specific kits) are
defined in [`layering.md`](./layering.md). They ship as distinct assemblies
(`Controls` / `DesignSystem` / `Themes.*`); design-specific kits remain embedded in `Controls`
until a design language adds behaviour beyond styling. The layer *dependency* claims below
verify against the projects' `.fsproj` references.

## Modules

Every packable product (17 libraries + the BOM metapackage) has a row. The scene, layout,
input, viewer, and Elmish assemblies form **Rendering.Core**; the rest layer on top.

| Area | Source module | Package | Structural area | Responsibility |
|---|---|---|---|---|
| Scene | `Scene` | `FS.GG.UI.Scene` | Rendering.Core | Retained scene graph, drawing primitives (incl. the `Colors` vocabulary), and animation. |
| Layout | `Layout` | `FS.GG.UI.Layout` | Rendering.Core | Layout engine and layout graph with validation. |
| Keyboard input | `KeyboardInput` | `FS.GG.UI.KeyboardInput` | Rendering.Core | Pointer + keyboard input model and dispatch — the live input path wired into viewer/controls. |
| Viewer | `SkiaViewer` | `FS.GG.UI.SkiaViewer` | Rendering.Core | SkiaSharp-over-GL viewer/host: window, frame loop, present mode, screenshot/replay seams. |
| Elmish integration | `Elmish` | `FS.GG.UI.Elmish` | Rendering.Core | The pure scene Elmish adapter (`ElmishAdapter`): wraps a `render : model → SceneNode` product and threads viewer messages/effects as pure values, plus the animation tick. For control-set products use `Controls.Elmish`. |
| Diagnostics | `Diagnostics` | `FS.GG.UI.Diagnostics` | Rendering.Core | Shared runtime diagnostic taxonomy, aggregation, readiness, and artifact contracts. |
| Controls | `Controls` | `FS.GG.UI.Controls` | Controls | Semantic control set (Button, TextBox, ComboBox, DataGrid, Dialog), accessibility, catalog, charts. |
| Controls Elmish integration | `Controls.Elmish` | `FS.GG.UI.Controls.Elmish` | Controls | The control-set Elmish adapter: a full interactive host over the semantic control set (`runInteractiveApp`/`program`, Cmd/subscriptions) plus responds/perf proof seams. Used by every in-repo sample. |
| Canvas | `Canvas` | `FS.GG.UI.Canvas` | Controls | Dependency-light pure element library and deterministic fixed-timestep game loop for canvas controls. |
| Design-system primitives | `DesignSystem` | `FS.GG.UI.DesignSystem` | DesignSystem | Token model, `Theme` record, `ResolvedStyle`, density/typography/radii/colour roles, visual-state rules, the pure `Style.resolve` resolver, the public `DesignTokensExt` Ant-derived taxonomy, and the `StyleResolver`/`IntentPolicy` seam. Depends only on `Scene`. Decisions [0003](./decisions/0003-designsystem-namespace-relocation.md), [0004](./decisions/0004-public-token-resolver-surface.md). |
| Default theme | `Themes.Default` | `FS.GG.UI.Themes.Default` | Themes | The default Light/Dark `Theme` value module, `Theming` mode/accent derivation, and the DTCG token source. Depends only on `DesignSystem`. |
| Ant Design theme | `Themes.AntDesign` | `FS.GG.UI.Themes.AntDesign` | Themes | The Ant Design `Theme` values (`AntTheme.antLight`/`antDark`) and the `AntIntentPolicy` (`StyleResolver.IntentPolicy`) driving Ant's visual language over the existing controls — no control fork. Depends only on `DesignSystem`. Decision [0006](./decisions/0006-antdesign-theme-and-new-controls.md). Fluent/Material remain future work. |
| Symbology | `Symbology` | `FS.GG.UI.Symbology` | Symbology | Pure, deterministic unit-symbology vocabulary: a fixed channel grammar turning a per-game stat→channel Token mapping into legible abstract vector symbols (Scene-only). |
| Symbology render | `Symbology.Render` | `FS.GG.UI.Symbology.Render` | Symbology | Thin headless Scene→PNG bridge for the symbology design loop over the public SkiaViewer reference-rendering path; fails loud on any non-passing verdict. |
| Governance build engine | `Build` | `FS.GG.UI.Build` | Governance | In-process governance engine (EvidenceGraph / EvidenceAudit gates) for generated FS.GG.UI products. |
| Testing helpers | `Testing` | `FS.GG.UI.Testing` | Testing | Test helpers: capture, screenshot, and responds/perf proof seams. |
| BOM metapackage | `Meta` | `FS.GG.UI` | Meta | The version-coherent BOM / umbrella metapackage referencing the whole set at one version (feature 207). |
| Design-specific kits | (in `Controls`) | — | Kits | Optional design-specific compositions (e.g. `AntDesign.Form`, `AntDesign.Table`). Embedded in `Controls`; split out only when a design language adds behaviour beyond styling. |
| Template support | `.template.config` + `.template.package` | `FS.GG.UI.Template` | Tooling/Template | `dotnet new` template and template package for generated consumers. Released on its own tag axis (decoupled from the framework pin). See [decision 0002](./decisions/0002-template-ownership.md). |

### Retired modules

| Source module | Package | Disposition | Reason |
|---|---|---|---|
| Color | `FS.GG.UI.Color` | retired (feature 179) | Orphaned, unshipped colour library that no production code referenced. Colour now lives in `Scene` (`Colors`) and the `DesignSystem` colour roles; the internal `ColorPolicy` was preserved for `Controls.Tests`. |
| Input | `FS.GG.UI.Input` | retired (feature 179) | Orphaned keyboard-input implementation superseded by `KeyboardInput`, the live path wired into SkiaViewer, Controls, and Controls.Elmish. |

## Exclusions

Explicitly **not** owned by this repository:

| Area | Source module | Disposition | Reason |
|---|---|---|---|
| Skill support | `SkillSupport` | excluded | Governance-flavored (CodeGen, EvidenceTour, Graph, Globbing). The constitution removed mandatory skill gates and treats skills as advisory; do not auto-import. Re-evaluate only if a concrete product need appears. |
| Vulkan backend | — | excluded | The constitution scopes this repository to SkiaSharp over **OpenGL (GL)**. Vulkan is out of scope here. |

## Notes

- **Sample galleries** (`BasicViewer`, `ControlsGallery`, `DataGridGallery`, …) are
  *validation surface*, not product modules; they are not packable and carry no row.
- An area that spans layers (e.g. a control bundling its own theming) keeps a **primary**
  Structural area in this map, with the split flagged for resolution — it is never left
  unclassified.
- This map is kept honest by a gate, not by hand: `Feature242DocsCurrencyTests` asserts the
  `Package` column lists exactly the packable projects in the solution.
