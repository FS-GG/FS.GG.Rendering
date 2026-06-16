# Ant Design adoption (Workstream F) — pattern docs index

FS.GG adopts Ant Design **as a design language**, realized entirely with this repo's own
primitives. There is **no React/DOM/HTML/CSS dependency**. These docs map Ant's stable interaction
patterns onto the shipped F1–F5 machinery.

## The three pillars

1. **Source of truth** — the three Ant LLM files (`llms.txt` index, `llms-full.txt` full
   API/usage + component tokens, `llms-semantic.md` semantic parts) are catalogued centrally in
   the [Ant source-of-truth hub](reference/ant-llms-sources.md). Cite the hub, not raw URLs.
2. **Per-family pattern docs** — one page per `Catalog.categories` family under
   [`patterns/`](patterns/), each mapping the Ant pattern + the component's named **semantic
   parts** onto repo controls, the token taxonomy, the central `StyleResolver`, and the
   `wcag`/`ant` `ColorPolicy`.
3. **Advisory skill** — `fs-gg-ant-design` steers contributors to the right local seams.

## Layering rule

**One semantic control set styled by themes — no per-theme control forks.** There is no
`AntButton` behavior copy; Ant styling is a theme over the single `Catalog` control set. Ant is a
design language only; the React `classNames`/DOM mechanism is **not adopted** (see each page's
"Not adopted" section and the [hub](reference/ant-llms-sources.md)).

## Pattern pages

- [display](patterns/display.md) · [input](patterns/input.md) ·
  [selection](patterns/selection.md) · [layout](patterns/layout.md) ·
  [navigation](patterns/navigation.md) · [overlay](patterns/overlay.md) ·
  [feedback](patterns/feedback.md) · [data](patterns/data.md) · [chart](patterns/chart.md) ·
  [graph](patterns/graph.md) · [custom](patterns/custom.md)

## Gallery family → catalog category cross-map (research R1)

The Controls Gallery groups controls into 10 presentation families; coverage here is anchored to
the 11 code-derived `Catalog.categories` (a strict superset — `chart` and `graph` get separate
pages).

| Gallery family | Catalog category (this doc set) |
|---|---|
| display / typography | `display` |
| buttons | `input` |
| text / numeric input | `input` |
| selection / toggles | `selection` |
| data / collections | `data` |
| layout / containers | `layout` |
| navigation / menus | `navigation` |
| overlays / feedback | `overlay` + `feedback` |
| charts | `chart` + `graph` |
| pointer-playground / custom | `custom` |

## Machine-checked references

```refs
doc:reference/ant-llms-sources.md
doc:patterns/display.md
doc:patterns/input.md
doc:patterns/selection.md
doc:patterns/layout.md
doc:patterns/navigation.md
doc:patterns/overlay.md
doc:patterns/feedback.md
doc:patterns/data.md
doc:patterns/chart.md
doc:patterns/graph.md
doc:patterns/custom.md
```
