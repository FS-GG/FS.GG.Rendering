---
family: input
---

# Ant input patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The input family covers buttons and text/numeric entry. Ant's button intents
(primary/default/dashed/text/link, plus danger) and its 8-unit grid with `controlHeight 32` map
directly onto the central resolver's intent vocabulary and the `Seed` sizing tokens. Text inputs
share the same height/spacing rhythm and expose affix regions (`prefix`/`suffix`).

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Button/root` | `button` background + border box | `Seed.controlHeight`, `Seed.colorPrimary` | `resolve` (intent + states) |
| `Button/content` | the button label run | `Seed.fontSize` | `resolve` |
| `Button/icon` | leading/trailing glyph slot | `Space.md` | `resolveDefault` |
| `Input/prefix` | leading affix of `text-box` | `Space.md` | `resolveDefault` |
| `Input/suffix` | trailing affix of `text-box` | `Space.md` | `resolveDefault` |

Intent colors (primary/danger) are validated with the `ant` policy; sizing follows
`Seed.controlHeight` (32) on the 8-unit grid.

## Machine-checked references

```refs
control:button
control:text-box
control:numeric-input
token:Seed.controlHeight
token:Space.md
resolver:resolve
resolver:resolveDefault
policy:ant
part:Button/root
part:Button/content
part:Button/icon
part:Input/prefix
part:Input/suffix
doc:../reference/ant-llms-sources.md
```

## Not adopted

Ant realizes button/input parts via React `classNames`/`styles` over a DOM (`<button>`,
`<input>`, wrapper `<span>`s). FS.GG adopts the **region vocabulary and intent/size model** only;
the React props and DOM are not adopted.
