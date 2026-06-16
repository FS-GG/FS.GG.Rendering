---
family: feedback
---

# Ant feedback patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The feedback family covers toasts, progress, spinners, and validation messages. Ant's `Alert`
carries an `icon`, `description`, and optional `close`, and uses the functional color families
(success/warning/error/info). FS.GG drives the status color through the resolver and the functional
`Seed` colors.

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Alert/root` | `toast`/`validation-message` container | `Seed.colorError` | `resolve` (status) |
| `Alert/icon` | the status glyph | `Seed.colorSuccess` | `resolve` (status) |
| `Alert/description` | the message body | `Seed.fontSize` | `resolveDefault` |
| `Alert/close` | the dismiss affordance | `Space.sm` | `resolve` (hover) |

## Machine-checked references

```refs
control:toast
control:validation-message
control:progress-bar
token:Seed.colorError
token:Seed.colorSuccess
token:Space.sm
resolver:resolve
resolver:resolveDefault
policy:ant
part:Alert/root
part:Alert/icon
part:Alert/description
part:Alert/close
doc:../reference/ant-llms-sources.md
```

Functional status colors are validated with the `ant` policy.

## Not adopted

Ant's `Alert` parts are React components styled via `classNames`/`styles` over an HTML/CSS DOM.
FS.GG adopts only the **region + functional-color concept** (root/icon/description/close); the
React props and DOM are not adopted.
