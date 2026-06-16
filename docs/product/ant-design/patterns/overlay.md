---
family: overlay
---

# Ant overlay patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The overlay family covers tooltips, dialogs, and generic overlays. Ant's `Modal` is the canonical
overlay, with a `mask` scrim, a `header`/`body`/`footer` content stack, and high elevation. FS.GG
composes the dialog from layout controls over an `overlay`, taking elevation from `Elevation.high`.

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Modal/mask` | the `overlay` scrim | `Elevation.high` | `baseStyleFor` |
| `Modal/header` | dialog title row | `Space.md` | `baseStyleFor` |
| `Modal/body` | dialog content region | `Space.md` | `baseStyleFor` |
| `Modal/footer` | dialog action row | `Space.md` | `baseStyleFor` |

## Machine-checked references

```refs
control:dialog
control:tooltip
control:overlay
token:Elevation.high
token:Space.md
resolver:baseStyleFor
policy:wcag
part:Modal/mask
part:Modal/header
part:Modal/body
part:Modal/footer
doc:../reference/ant-llms-sources.md
```

Overlay surface/text pairings are validated with the `wcag` policy.

## Not adopted

Ant's `Modal` parts are React components styled via `classNames`/`styles` over an HTML/CSS DOM
(portal + `<div>` regions). FS.GG adopts only the **region concept** (mask/header/body/footer);
the React props, portal, and DOM are not adopted.
