---
family: selection
---

# Ant selection patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The selection family covers toggles and pickers: checkboxes, radios, switches, list/combo
selection, and the color picker. Ant expresses selection through a control indicator (`icon`) and
a `label`, with the selected state carrying the primary color. FS.GG drives the selected/checked
state through the resolver and pulls the accent from `Seed.colorPrimary`.

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Checkbox/root` | `check-box` hit/box region | `Seed.controlHeight` | `resolveDefault` |
| `Checkbox/icon` | the check/indicator glyph | `Seed.colorPrimary` | `resolve` (selected) |
| `Checkbox/label` | the adjacent text | `Seed.fontSize` | `resolveDefault` |

## Machine-checked references

```refs
control:check-box
control:switch
control:color-picker
token:Seed.colorPrimary
token:Seed.controlHeight
resolver:resolve
resolver:resolveDefault
policy:ant
part:Checkbox/root
part:Checkbox/icon
part:Checkbox/label
doc:../reference/ant-llms-sources.md
```

The selected accent is validated with the `ant` policy.

## Not adopted

Ant's checkbox/switch parts are React components styled via `classNames`/`styles` over an HTML/CSS
DOM. FS.GG adopts only the **named-region concept** (box/indicator/label); the React props and DOM
are not adopted.
