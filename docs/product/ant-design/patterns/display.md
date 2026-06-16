---
family: display
---

# Ant display patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The display family covers non-interactive presentation: text, labels, icons, separators, and
status badges. Ant's display components express hierarchy through type scale and through small
status affordances (the `Badge` dot/count). FS.GG realizes these with the catalog display controls,
the type/`Seed` tokens for size and color, and the neutral resolver path (display controls have no
intent state of their own — they inherit surface and text materials).

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Badge/root` | the host control's content box (`badge`/`label`) | `Seed.fontSize`, `Seed.colorTextBase` | `resolveDefault` (neutral) |
| `Badge/indicator` | the status dot/count drawn over the host | `Seed.colorError` / `Seed.colorPrimary` | `resolveDefault` |

## Machine-checked references

```refs
control:badge
control:label
token:Seed.fontSize
token:Seed.colorTextBase
resolver:resolveDefault
policy:wcag
part:Badge/root
part:Badge/indicator
doc:../reference/ant-llms-sources.md
```

Text/foreground pairings are validated with the `wcag` policy so display content stays legible.

## Not adopted

Ant's `Badge` semantic parts live in a React component styled via the `classNames`/`styles` props
over an HTML/CSS DOM. FS.GG adopts only the **named-region concept** (a `root` content box with an
`indicator` overlay); the `classNames`/DOM realization is not adopted.
