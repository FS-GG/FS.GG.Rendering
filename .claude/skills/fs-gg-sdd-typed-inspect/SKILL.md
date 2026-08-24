---
name: fs-gg-sdd-typed-inspect
description: Inspect Typed SDD compiler identity, authority, and derived projection freshness.
---

# Typed SDD inspect

Run `fsgg-sdd typed-sdd inspect --work <id>`. A clean result compiles the canonical F# authority,
checks its recorded compiler/package/extension identities and authoring receipt, and proves the
normalized JSON and Markdown bytes are derived from it. Resolve reported diagnostic IDs; never
bypass or hand-edit `readiness/<id>/typed-authority.json`.
