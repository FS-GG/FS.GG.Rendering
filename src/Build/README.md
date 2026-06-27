# FS.GG.UI.Build

The in-process governance engine for generated FS.GG.UI products. It provides the
**EvidenceGraph** and **EvidenceAudit** gates that a scaffolded product's `build.fsx` `Verify`
target binds by reflection (`FS.GG.UI.Build.Evidence.GeneratedRunner.run target dir`).

- **EvidenceGraph** senses the product's `readiness/**` surface and writes a synthesized
  `readiness/evidence-graph.md`.
- **EvidenceAudit** judges the sensed graph against the readiness token contract
  (`docs/evidence-formats.md`) and writes `readiness/evidence-audit.md` carrying a `verdict` token.

The engine is dependency-minimal (FSharp.Core only), runs entirely in-process (no external
process), and is packed in lock-step with every other `FS.GG.UI.*` package from the single
`$(FsSkiaUiVersion)` version pin. See `specs/202-fix-build-fsx-engine/` for the contract and
design.
