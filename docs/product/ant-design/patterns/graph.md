---
family: graph
---

# Ant graph patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The graph family covers node/edge graph views. As with charts, Ant has no core graph component or
semantic-parts vocabulary for the graph canvas itself (graph visualization lives in
`@ant-design/graphs`/G6). The portable Ant idea is again the **panel chrome**: a `Card` with a
`header` (toolbar/title) wrapping a `body` (the graph canvas). FS.GG renders the graph inside that
panel and takes the node accent from `Seed.colorPrimary`.

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Card/root` | the graph panel container | `Space.md` | `baseStyleFor` |
| `Card/body` | the graph drawing canvas | `Seed.colorPrimary` | `resolveDefault` |

## Machine-checked references

```refs
control:graph-view
token:Seed.colorPrimary
token:Space.md
resolver:resolveDefault
resolver:baseStyleFor
policy:ant
part:Card/root
part:Card/body
doc:../reference/ant-llms-sources.md
```

Node/edge accent colors are validated with the `ant` policy.

## Not adopted

The **graph canvas internals** (nodes, edges, layout, hit-testing) have no Ant semantic-part
vocabulary — Ant delegates them to a separate graph library with its own DOM/canvas. FS.GG draws
them directly in Skia and does **not** adopt any Ant graph DOM. Only the surrounding `Card` panel
chrome is mapped; its React `classNames`/DOM realization is not adopted.
