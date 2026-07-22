---
name: fs-gg-product-symbol-design
description: Design a game's visual language iteratively, in situ, over the WHOLE renderable-element set — not just the unit roster, but doors/interactables, projectiles, explosions & effects, terrain & obstacles, pickups, hazards, and status/marker overlays. You hand the agent the game structure (world / unit / stats types), the FULL element inventory with stats, and a captured gamestate frame (element positions + terrain); it drafts SEVERAL competing visual directions, renders each as a FAITHFUL frame — real symbols at their real positions over the real board via `Symbology.Render` — screens them with the `Legibility` linter and the eye, presents a contact sheet, and converges with you to one approved symbol language. It PRODUCES AND MAINTAINS the machine-readable element↔visual CATALOG (`FS.GG.UI.Symbology.Catalog`) — the single source of truth #989's `Coverage` check consumes — so every gameplay element resolves to a `Shown` token or an explicit `Hidden`-by-mechanic opt-out, never silent omission. The divergent, whole-frame, whole-inventory exploration loop on top of the single-mapping mechanics of [[fs-gg-symbology]].
---

# FS.GG Product Symbol-Design

<!-- skill-refs: prose-ok #989 — the description echoes the canonical body's provenance citation of the framework's Coverage design issue, not a pointer into the reader's product tracker. -->

This is the Claude-active wrapper for the generated-product skill variant.

Before acting, read the canonical instructions in:

`../../../template/product-skills/fs-gg-symbol-design/SKILL.md`
