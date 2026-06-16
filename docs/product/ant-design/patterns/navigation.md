---
family: navigation
---

# Ant navigation patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The navigation family covers tabs, menus, context menus, and toolbars. Ant's `Tabs` carries a
`nav` strip of `tab` items with a moving `indicator` (ink bar) marking the active tab. FS.GG maps
the active/hover state through the resolver and the accent through `Seed.colorPrimary`.

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Tabs/nav` | `tabs` header strip | `Space.md` | `resolveDefault` |
| `Tabs/tab` | an individual tab item | `Seed.fontSize` | `resolve` (hover/selected) |
| `Tabs/indicator` | the active-tab ink bar | `Seed.colorPrimary` | `resolve` (selected) |

## Machine-checked references

```refs
control:tabs
control:menu
token:Seed.colorPrimary
token:Space.md
resolver:resolve
resolver:resolveDefault
policy:wcag
part:Tabs/nav
part:Tabs/tab
part:Tabs/indicator
doc:../reference/ant-llms-sources.md
```

Active/selected affordances keep legible contrast under the `wcag` policy.

## Not adopted

Ant's `Tabs` parts are React components styled via `classNames`/`styles` over an HTML/CSS DOM. FS.GG
adopts only the **region concept** (nav/tab/indicator); the React props and DOM are not adopted.
