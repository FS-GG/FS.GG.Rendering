---
name: fs-gg-sdd-typed-author
description: Author and accept canonical Typed SDD F# specifications through FS.GG.SDD.
---

# Typed SDD author

Start with `fsgg-sdd typed-sdd author --work <id> --title <title> --agent <agent-id> --session <session-id>`.
Treat `work/<id>/specification.fsx` as authority. After editing that F# authority, run the same
command with `--accept` and a fresh agent/session receipt. Acceptance compiles the F# and regenerates
normalized JSON, Markdown, and the authority manifest. Never hand-edit generated projections.
