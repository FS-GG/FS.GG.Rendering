---
family: chart
---

# Ant chart patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The chart family covers line/bar/pie charts and scatter plots. Ant's core component set (and the
semantic-parts documentation) does **not** include charts — charting lives in the separate
`@ant-design/charts`/`@ant-design/plots` libraries. What Ant *does* standardize is the **panel
chrome** a chart sits in: a `Card` with a `header` (title/legend area) and a `body` (the plot
region). FS.GG adopts that container model — the chart controls render the plot inside a panel —
and takes the series accent from `Seed.colorPrimary`.

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Card/header` | the chart panel's title/legend row | `Space.md` | `baseStyleFor` |
| `Card/body` | the plot drawing region | `Seed.colorPrimary` | `resolveDefault` |

## Machine-checked references

```refs
control:line-chart
control:bar-chart
control:pie-chart
token:Seed.colorPrimary
token:Space.md
resolver:resolveDefault
resolver:baseStyleFor
policy:ant
part:Card/header
part:Card/body
doc:../reference/ant-llms-sources.md
```

Series colors are validated with the `ant` policy.

## Not adopted

The **plot internals** (axes, series, ticks, gridlines) have no Ant semantic-part vocabulary —
Ant delegates them to a separate charting library with its own canvas/SVG DOM. FS.GG draws them
directly in Skia and does **not** adopt any Ant chart DOM. Only the surrounding `Card` panel
chrome is mapped; the React `classNames`/DOM realization of even that chrome is not adopted.
