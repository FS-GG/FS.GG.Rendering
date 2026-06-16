---
family: custom
---

# Ant custom / pointer-playground patterns → FS.GG

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

The custom family covers bespoke, app-specific surfaces (the pointer playground and any
`custom-control`). Ant has no semantic-parts vocabulary for arbitrary custom content — by
definition the interior is application-defined. The one portable Ant idea is the **container**: a
`Card` `root`/`body` that hosts the custom surface so it inherits the surrounding spacing and
surface materials. FS.GG sizes the container on the base unit (`Seed.sizeUnit`).

## Ant semantic parts → repo regions

Source: the central hub ([`../reference/ant-llms-sources.md`](../reference/ant-llms-sources.md)).

| Ant part | Repo region | Material (token) | Resolver state |
|---|---|---|---|
| `Card/root` | the `custom-control` host container | `Seed.sizeUnit` | `baseStyleFor` |
| `Card/body` | the custom drawing region | `Space.md` | `resolveDefault` |

## Machine-checked references

```refs
control:custom-control
token:Seed.sizeUnit
token:Space.md
resolver:resolveDefault
resolver:baseStyleFor
policy:wcag
part:Card/root
part:Card/body
doc:../reference/ant-llms-sources.md
```

Any text the custom surface draws on the container is validated with the `wcag` policy.

## Not adopted

A custom surface's **interior** has no Ant semantic-part vocabulary and no Ant DOM — it is fully
application-defined and drawn directly in Skia. FS.GG adopts only the **container concept**
(root/body); the React `classNames`/DOM realization is not adopted.
