---
name: fs-gg-ant-design
description: "Advisory guide for building Ant-styled UI in FS.GG: map Ant Design's stable ideas onto the repo's tokens, controls, resolver, and color policy — no React/DOM."
# Carried over from the body that used to sit AT this path (#1080/#1082). These two are ACTIVATION
# flags: the runtime reads them from the skill root, not from the canonical body the wrapper routes to,
# so leaving them behind with the body would have silently changed how this skill activates. The
# `metadata:` provenance block moved WITH the body, where provenance belongs.
user-invocable: true
disable-model-invocation: false
---

# FS.GG Ant Design

This is a wrapper for the canonical local skill.

Before acting, read the canonical instructions in:

`../../../docs/product/ant-design/skill/SKILL.md`
