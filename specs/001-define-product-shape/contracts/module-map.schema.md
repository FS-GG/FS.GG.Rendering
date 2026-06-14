# Contract: Module Map (`docs/product/module-map.md`)

The module map is the product's external "what do we own?" contract. Format:

## Required structure

1. A one-paragraph **ownership boundary** statement.
2. A **module table** with these columns, in order:

   | Area | Source module | Structural area | Responsibility | Disposition | Reason |
   |---|---|---|---|---|---|

3. An **exclusions** subsection listing what is explicitly NOT owned, each with a reason.

## Field rules

- **Area**: unique; human-readable.
- **Source module**: a `src/**` module name from the source repo, or `—` if new.
- **Structural area**: one of `Rendering.Core`, `Controls`, `DesignSystem`, `Themes`,
  `Kits`, `Tooling/Template`, `Testing`. This is a broader classification than the four UI
  layers in `layering.md` — `Controls`, `DesignSystem`, `Themes`, and `Kits` ARE those UI
  layers; `Rendering.Core`, `Tooling/Template`, and `Testing` are non-UI structural buckets.
  Do not read this column as "seven UI layers."
- **Disposition**: one of `owned-here`, `import-from-source`, `deferred`, `excluded`.
- **Reason**: required iff disposition is `deferred` or `excluded`; otherwise may be `—`.

## Acceptance (maps to spec)

- [ ] Every area named in FR-001 appears (scene, color, layout, input, viewer, Elmish
      integration, controls, controls Elmish integration, testing helpers, template support).
      *(SC-002)*
- [ ] No row has an empty Disposition. *(SC-002)*
- [ ] Vulkan backend and governance/SkillSupport appear in exclusions with reasons. *(FR-003)*
- [ ] A reader can state the ownership boundary from this file alone. *(SC-001)*

## Example row

```text
| Viewer | SkiaViewer | Rendering.Core | SkiaSharp-over-GL window/host + frame loop | import-from-source | — |
| Skill support | SkillSupport | — | governance-flavored skill helpers | excluded | governance machinery removed by constitution; not a product concern |
```
