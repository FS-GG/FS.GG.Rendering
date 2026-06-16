---
family: data
---

# Ant data patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The data family covers collections: list views, tree views, and data grids. Ant's `Table` is the
canonical collection, with a `header`, `row`s, and `cell`s, plus hover/selected row affordances.
FS.GG composes the grid from the data controls, with density/spacing from `Space` and row states
through the resolver.

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Table/header` | `data-grid` header band | `Space.sm` | `baseStyleFor` |
| `Table/row` | a `list-view`/`data-grid` row | `Space.sm` | `resolve` (hover/selected) |
| `Table/cell` | a single cell | `Seed.fontSize` | `resolveDefault` |

## Machine-checked references

```refs
control:data-grid
control:list-view
control:tree-view
token:Space.sm
token:Seed.fontSize
resolver:resolve
resolver:baseStyleFor
policy:wcag
part:Table/header
part:Table/row
part:Table/cell
doc:../reference/ant-llms-sources.md
```

Row text/background pairings (including hover/selected) are validated with the `wcag` policy.

## Not adopted

Ant's `Table` parts are React components styled via `classNames`/`styles` over an HTML/CSS DOM
(`<table>`/`<tr>`/`<td>`). FS.GG adopts only the **region concept** (header/row/cell); the React
props and DOM are not adopted.
