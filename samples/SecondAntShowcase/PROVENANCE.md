# Provenance - Second Ant Showcase

This sample is derived from the existing package-consuming Ant showcase shape, but it is a
new independent sample under `samples/SecondAntShowcase`. It does not rename, replace, or
weaken `samples/AntShowcase`.

## Sources

| Item | Source | Adaptation |
|---|---|---|
| Three-project package-consuming sample shape | `samples/AntShowcase` and `samples/ControlsGallery` | Copied into `SecondAntShowcase.Core`, `SecondAntShowcase.App`, and `SecondAntShowcase.Tests`, then renamed and extended with the feature 171 surface/evidence contracts. |
| Catalog page grouping and coverage bijection | `FS.GG.UI.Controls.Catalog.supportedControls` and `samples/AntShowcase/coverage-report.md` | Kept as 13 catalog pages with every live catalog id assigned exactly once. |
| Enterprise page templates | `docs/product/ant-design/README.md` and `docs/product/ant-design/patterns/` | Implemented as compositions of known catalog controls only. |
| Ant design-language guidance | `docs/product/ant-design/reference/ant-llms-sources.md` | Cited through the local hub; raw upstream Ant URLs are intentionally not copied into sample docs. |
| Shipped Ant themes | `FS.GG.UI.Themes.AntDesign` | Resolved verbatim through `AntTheme.antLight` and `AntTheme.antDark`. |

## Layering

The sample uses one semantic control set styled by themes. It does not introduce any
Ant-specific behavior fork, product control, product theme, React layer, DOM, HTML, or CSS
dependency.

## Package-Consumer Proof

The framework is consumed exclusively as packed `FS.GG.UI.*` packages from
`~/.local/share/nuget-local`. Building this sample against the local feed is the consumer
proof; project files must not add `ProjectReference` entries into `src/`.
