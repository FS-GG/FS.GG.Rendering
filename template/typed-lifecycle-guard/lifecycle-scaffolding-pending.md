# Typed SDD lifecycle scaffolding pending

This product was scaffolded with `--lifecycle typed-sdd`. The raw template preserved that explicit choice; it has not aliased or downgraded it to the untyped `sdd` lane.

FS.GG.SDD must supply the compiler/package identity, canonical agent-authored F#, normalized AST, receipt, Markdown projection, and readiness evidence. Until that succeeds, this sentinel intentionally keeps readiness fail-closed and the build warning visible.

Run the published `fsgg-sdd` typed lifecycle flow. It removes this file only after the lifecycle artifacts have materialized successfully.
