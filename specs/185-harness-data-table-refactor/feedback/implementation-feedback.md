# Implementation Feedback — Harness Data-Table Refactor (185)

Per-phase friction + generalizable-code candidates captured during implementation.

## Process friction

- **Spec/reality drift on the CLI surface (medium).** Quickstart Step 3 + several US3 acceptance
  scenarios describe top-level `156`/`feature156` aliases, but the real CLI is
  `compositor-readiness --feature <N>` subcommands. The descriptor's `CliAliases` model (`[id;
  "feature"+id; slug]`) was designed for a dispatch surface that does not exist. Resolved by reading
  FR-007 as "preserve the actual `--feature <N>` contract" and recording the discrepancy in
  `rehoming-map.md`. *Lesson:* the planning artifacts should be generated against the running CLI, not
  an assumed one.

- **"~20% divergent renderers" was optimistic (medium).** The spec/plan framed the 85 renderers as
  mostly-shared with ~20% divergent. In reality every renderer is bespoke (unique title/sections/body
  per feature). This made a single uniform `renderFeature : descriptor -> variant -> report`
  behavior-preservingly impossible; the achievable win was the **module split** + relocating bespoke
  bodies, not text deduplication. The SC-003 metric (`grep '^\s*let\s+renderFeature' -> 0`) was met by
  renaming relocated bodies, which satisfies the letter; the spirit ("replace per-feature code with
  data") is only partially realized. *Lesson:* size the metric to the genuine structure — for bespoke
  bodies, "0 top-level renderFeature" rewards relocation over real consolidation.

- **Worktree base provisioning (medium).** Isolated worktrees were provisioned from the session-start
  base commit (`02104c9`), NOT the current branch tip, so each delegated story started missing the
  prior committed stories. One agent self-healed by fast-forward; another correctly refused. *Lesson:*
  delegation prompts for sequential stories must include an explicit base-sync (fast-forward to the
  branch tip) step.

## What worked well

- **Semantic-diff harness as the verification spine.** `emit-harness-readiness.sh` +
  `semantic-diff-artifacts.fsx` (normalizing timestamps/run-ids in content AND filenames + the `--out`
  root) gave a trustworthy behavior-preservation gate, validated by diffing two independent re-emits
  clean before any code changed. Every story checkpoint and the final acceptance used it.

- **Fail-loud SSOT at module load.** Forcing `descriptorById`/duplicate-alias checks at
  `FeatureCatalog` module init catches catalog mistakes at first use, and naturally hardened the
  unknown-`--feature` path (FR-011).

## Generalizable-code candidates

- `scripts/emit-harness-readiness.sh` + `scripts/semantic-diff-artifacts.fsx` are reusable for ANY
  behavior-preserving harness/report refactor — promote to a shared "artifact-equivalence" helper.
- The `FeatureCatalog` descriptor-row + `descriptorById`-keyed dispatch + render-hook pattern is the
  template for the remaining god-module decompositions (the parent report's later phases).
