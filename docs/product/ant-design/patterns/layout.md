---
family: layout
---

# Ant layout patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The layout family covers containers and structure: stacks, grids, docks, borders, panels, and
scroll/split views. Ant's `Card` is the canonical container, with `header`/`body`/`extra`/`title`
regions and an elevation. FS.GG composes these from the layout controls, with spacing from `Space`
and elevation from `Elevation`; containers use the neutral resolver path.

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Card/root` | `panel`/`border` container box | `Elevation.low` | `baseStyleFor` |
| `Card/header` | a `stack` header row | `Space.lg` | `baseStyleFor` |
| `Card/body` | the content region | `Space.lg` | `baseStyleFor` |

## Machine-checked references

```refs
control:panel
control:stack
control:border
token:Space.lg
token:Elevation.low
resolver:baseStyleFor
policy:wcag
part:Card/root
part:Card/header
part:Card/body
doc:../reference/ant-llms-sources.md
```

Container surface/text pairings are validated with the `wcag` policy.

## Not adopted

Ant's `Card` parts are React components styled via `classNames`/`styles` over an HTML/CSS DOM
(`<div>` regions). FS.GG adopts only the **container-region concept** (root/header/body); the
React props and DOM are not adopted.
