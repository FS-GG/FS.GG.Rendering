# Contract: Evidence output (`evidence-graph.md` / `evidence-audit.md`)

The engine's two targets each produce one readiness artifact. The authoritative shape is
`template/base/docs/evidence-formats.md` — itself generated from the original engine's
`EvidenceFormatSchema`, so honoring it keeps the re-authored engine drift-compatible with the
documented contract that ships in every generated product.

## `EvidenceGraph` target → `readiness/evidence-graph.md`

- **Purpose**: synthesize the generated product's sensed `readiness/**` evidence artifacts into a
  validation graph (which evidence exists, its kind, and whether its token contract is satisfied).
- **Input surface sensed** (presence is profile-dependent — graph what exists):
  - headless profiles: `readiness/layout-evidence.txt`, `readiness/headless-scene-evidence.txt`
  - interactive profiles additionally: `readiness/evidence-launch-mode.txt`,
    `readiness/game-image-evidence.png(+.metadata.txt)`, `readiness/game-screenshot-evidence.txt`,
    `readiness/game-pixel-readback-evidence.txt`, `readiness/window-diagnostics.txt`,
    `readiness/window-options.txt`, `readiness/bounded-viewer-smoke.txt`,
    `readiness/bounded-viewer-frame-diagnostics.txt`
- **Output**: `readiness/evidence-graph.md`, a real synthesized report (NOT a completion-only stub —
  SC-001). Must be written under `readiness/` with parent-dir creation.
- **Exit**: 0 when the graph is built over the available, well-formed surface; non-0 (with a named
  reason) when a required-for-profile artifact is missing/malformed.

## `EvidenceAudit` target → `readiness/evidence-audit.md`

- **Purpose**: audit the graph against the readiness contract and emit a single governance verdict.
- **Required token** (`evidence-formats.md` §`evidence-audit.md`): **`verdict`** — e.g.
  `verdict=PASS` or `verdict=FAIL`. The file's presence is itself required; the audit is a
  feature-local merge-gate record.
- **Output**: `readiness/evidence-audit.md` containing the `verdict` token (and, on fail, the failing
  class/reason).
- **Exit**: `verdict=PASS` ⇒ 0; otherwise non-0 with a diagnostic.

## Failure / honesty contract (FR-005, Principle VI)

- Engine-unresolvable (restore/load) is surfaced by `build.fsx` before `run` is ever called — message
  names `FS.GG.UI.Build <version>` and the cache/feed searched.
- Engine-internal failure (missing/invalid required artifact) returns non-0 and writes a verdict/reason
  that makes clear whether the cause is a **framework/feed condition** or a **defect in the generated
  product** (acceptance US3 #2). Never emit `verdict=PASS` / exit 0 on an unresolved or invalid run.
- The two reports are real output every passing run, not skipped or log-only (SC-001, FR-001).

## Token shapes referenced from `evidence-formats.md` (for the audit's checks)

The engine's audit MAY validate the per-file token contracts the schema enumerates (key/value
`token=value` form for the parsed files), e.g. `interactive-visible-window.md`
(status/mode/window-visible/…), `window-state-diagnostics.md` (diagnostic-class=…),
`real-image-evidence.md` (evidence-kind/artifact-decodable/proves-scene-rendering/…),
`generated-validation.md` (exact-package-match/generated-tests-ran/authoritative/failure-class). Depth
of enforcement is an implementation choice bounded by "must actually execute, not a stub."
