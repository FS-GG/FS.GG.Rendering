---
name: fs-gg-ant-design
description: "Advisory guide for building Ant-styled UI in FS.GG: map Ant Design's stable ideas onto the repo's tokens, controls, resolver, and color policy — no React/DOM."
# Carried over from the body that used to sit AT this path (#1080, #1082). These two are ACTIVATION
# flags: the runtime reads them from the skill root, not from the canonical body the wrapper routes to,
# so leaving them behind with the body would have silently changed how this skill activates. The
# `metadata:` provenance block moved WITH the body, where provenance belongs.
user-invocable: true
disable-model-invocation: false
---
<!-- skill-refs: closed-ok FS.GG.Rendering#1080 — cited as history: the three-root union item whose work moved the canonical Ant body out of this path to docs/product/ant-design/skill/SKILL.md, leaving these two activation flags behind. Not somewhere to go; closed is correct and it stays closed. The ref it excuses sits in the YAML frontmatter above, which cannot host an HTML comment without breaking the parse; closed-ok is file-scoped, so it is honoured from here. -->
<!-- skill-refs: closed-ok FS.GG.Rendering#1082 — cited as history: the decision that ruled Rendering's three skill roots BYTE-IDENTICAL and the old per-surface Claude-active/Codex-active wrappers legacy drift. Read the decision comment, not the title: the title poses "divergent BY DESIGN" as the question and the answer was the opposite. That answer is why this file is an ordinary wrapper, identical in all three roots — and so why the flags had to be carried HERE rather than travel with the canonical. Not somewhere to go; closed is correct and it stays closed. Same frontmatter-scoping note as above. -->

# FS.GG Ant Design

This is a wrapper for the canonical local skill.

Before acting, read the canonical instructions in:

`../../../docs/product/ant-design/skill/SKILL.md`
