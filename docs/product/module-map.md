# Product / module map

> Migration Stage R2 deliverable. The authoritative answer to **"what does the rendering
> product own?"** It is a *map and disposition* record, not imported code — source import
> happens at Stage R4. Source of record: archived `EHotwagner/FS-Skia-UI` (`src/**`,
> `.template.config/`).

## Ownership boundary

FS.GG.Rendering owns the F# UI framework as a product: the retained **scene** and drawing
primitives, **color** primitives, **layout**, **pointer and keyboard input**, the
**SkiaSharp-over-OpenGL viewer/host**, **Elmish/MVU integration**, the **semantic control
set**, the **design-system / theme / kit** layers that style and compose those controls,
**testing helpers**, and the **`dotnet new` template** that generates consumer apps. It does
**not** own a Vulkan backend or any governance/skill machinery (see [Exclusions](#exclusions)).
Everything inside the boundary is imported from the source repository at Stage R4; this map is
that import checklist.

The four UI layers (controls, design-system primitives, themes, design-specific kits) are
defined in [`layering.md`](./layering.md). In the source they are currently bundled inside the
`Controls` module; the rows below record them as **distinct target layers** so the split is
resolved deliberately at import rather than carried over implicitly.

## Modules

| Area | Source module | Structural area | Responsibility | Disposition | Reason |
|---|---|---|---|---|---|
| Scene | `Scene` | Rendering.Core | Retained scene graph, drawing primitives, and animation. | import-from-source | — |
| Color | `Color` | Rendering.Core | Color primitives — contrast, palettes, color roles. | import-from-source | — |
| Layout | `Layout` | Rendering.Core | Layout engine and layout graph with validation. | import-from-source | — |
| Input | `Input` | Rendering.Core | Pointer/input event model and dispatch. | import-from-source | — |
| Keyboard input | `KeyboardInput` | Rendering.Core | Keyboard input model and key handling. | import-from-source | — |
| Viewer | `SkiaViewer` | Rendering.Core | SkiaSharp-over-GL viewer/host: window, frame loop, present mode, screenshot/replay seams. | import-from-source | — |
| Elmish integration | `Elmish` | Rendering.Core | Elmish/MVU runtime integration and animation tick. | import-from-source | — |
| Controls | `Controls` | Controls | Semantic control set (Button, TextBox, ComboBox, DataGrid, Dialog), accessibility, catalog, charts. | import-from-source | — |
| Controls Elmish integration | `Controls.Elmish` | Controls | Elmish/MVU bindings and responds/perf proof seams for controls. | import-from-source | — |
| Design-system primitives | `DesignSystem` | DesignSystem | Token model, theme records, density, typography, radii, color roles, visual-state rules, the pure `Style.resolve` resolver. Now also the **public** `DesignTokensExt` Ant-derived token taxonomy (seed/map/alias/component + space/density/type/elevation) and the **public** `StyleResolver` front-half resolver + `IntentPolicy` seam (promoted in feature 130/F5). | import-from-source | Owned assembly `FS.GG.UI.DesignSystem` (feature 125, decision [0003](./decisions/0003-designsystem-namespace-relocation.md); public token/resolver surface, decision [0004](./decisions/0004-public-token-resolver-surface.md)); depends only on `Scene`. |
| Default theme | `Themes.Default` | Themes | The default Light/Dark `Theme` value module, `Theming` mode/accent derivation, and the DTCG token source. | import-from-source | Owned assembly `FS.GG.UI.Themes.Default` (feature 125); depends only on `DesignSystem`. Additional concrete themes (Ant, Fluent, Material-inspired) remain future work. |
| Design-specific kits | (in `Controls`) | Kits | Optional design-specific compositions (e.g. `AntDesign.Form`, `AntDesign.Table`). | import-from-source | Currently embedded in `Controls`; split at import; justified only when a design language adds behavior beyond styling. |
| Testing helpers | `Testing` | Testing | Test helpers: capture, screenshot, and responds/perf proof seams. | import-from-source | — |
| Template support | `.template.config` + `.template.package` | Tooling/Template | `dotnet new` template and template package for generated consumers. | import-from-source | Rendering owns templates for now — see [decision 0002](./decisions/0002-template-ownership.md). |

All dispositions above are `import-from-source`: the rendering repo is the product owner, and
the code physically arrives from FS-Skia-UI at Stage R4. Nothing in scope is left without a
disposition.

## Exclusions

Explicitly **not** owned by this repository:

| Area | Source module | Disposition | Reason |
|---|---|---|---|
| Skill support | `SkillSupport` | excluded | Governance-flavored (CodeGen, EvidenceTour, Graph, Globbing). The constitution removed mandatory skill gates and treats skills as advisory; do not auto-import. Re-evaluate only if a concrete product need appears. |
| Vulkan backend | — | excluded | The constitution scopes this repository to SkiaSharp over **OpenGL (GL)**. Vulkan is out of scope here. |

## Notes

- **Sample galleries** (13: `BasicViewer`, `ControlsGallery`, `DataGridGallery`, … ) are
  *validation surface*, not product modules; their import is decided with the validation set
  at Stage R3, not here.
- An area that spans layers (e.g. a control bundling its own theming) keeps a **primary**
  Structural area in this map, with the split flagged for resolution at import — it is never
  left unclassified.
