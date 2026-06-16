---
name: "fs-gg-ant-design"
description: "Advisory guide for building Ant-styled UI in FS.GG: map Ant Design's stable ideas onto the repo's tokens, controls, resolver, and color policy — no React/DOM."
metadata:
  author: "FS.GG"
  source: "specs/131-ant-pattern-docs-skill"
user-invocable: true
disable-model-invocation: false
---

# fs-gg-ant-design

Advisory aid for translating Ant Design into FS.GG's own machinery. **This skill is advisory: it
suggests where to make changes; it never requires, blocks, or gates any work.**

> Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.

## Source of truth

Draw Ant facts from the central hub — the three Ant LLM files catalogued in one place:
[`ant-llms-sources.md`](../../../docs/product/ant-design/reference/ant-llms-sources.md)
(`llms.txt` = index, `llms-full.txt` = full API/usage + component tokens, `llms-semantic.md` =
semantic parts). Don't cite raw `ant.design` URLs ad hoc — link the hub.

## How to apply an Ant pattern

1. Find the family page under
   [`docs/product/ant-design/patterns/`](../../../docs/product/ant-design/patterns/) (e.g.
   [input](../../../docs/product/ant-design/patterns/input.md)) and the
   [index](../../../docs/product/ant-design/README.md).
2. Use the **semantic parts → repo regions** table: each Ant named region maps to a repo control
   region, a token-taxonomy entry (the "material"), and a `StyleResolver` visual-state.
3. Compose existing `Catalog` controls; pull materials from `DesignTokensExt`/`DesignTokens`; drive
   stateful styling through `StyleResolver`; validate color pairings with `ColorPolicy`
   (`wcag`/`ant`).

## Layering rule

Style with **one semantic control set styled by themes — no per-theme control forks**. Do not
create an `AntButton` behavior copy; Ant styling is a theme over the single control set. Adopt the
semantic-parts **concept** (named regions; tokens-as-materials / semantic-styles-as-application),
not Ant's React `classNames`/DOM **mechanism**.

## Machine-checked references

```refs
doc:../../../docs/product/ant-design/reference/ant-llms-sources.md
doc:../../../docs/product/ant-design/patterns/input.md
doc:../../../docs/product/ant-design/README.md
token:Seed.colorPrimary
resolver:resolveDefault
policy:ant
```
