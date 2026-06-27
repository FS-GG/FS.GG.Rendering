# Feature 205 — fs-gg / Spec Kit feedback (T026)

Captured during implementation of side-effect-free template generation. Severity: **info** unless noted.

## Process friction

- **The real hang root cause was the `--allow-scripts` prompt, not "a spinning post-action" (medium).**
  Plan/spec/research framed the CI-hang as the auto-init post-action "spinning forever". The live smoke
  showed the actual mechanism: `dotnet new` guards every Run post-action with an interactive
  allow-scripts confirmation (`Invalid input "". Please enter one of [Y(yes)|N(no)].`) that loops on
  empty stdin in a headless context. This sharpens the fix — removing the auto-run default removes the
  *prompt* from the default path — and surfaces a durable consumer constraint: any automated
  `--initGit true` invocation must also pass `--allow-scripts yes`. Worth stating in the root-cause
  framing up front rather than discovering it at the host.

- **The "always-on instructions-only post-action" (R3) is not expressible on the engine (medium).**
  T005's unknown resolved hard-negative: a post-action with no `actionId` is rejected at load time
  (`CONFIG0202`), and a side-effect-free default also forbids an `echo` process — so there is no clean
  "print a line on every generation" mechanism. The research had already scoped the fallback
  (README/docs surface), so the plan absorbed it without rework — a good example of pre-registering the
  fallback for an unverified engine assumption.

- **`tests/Package.Tests` is omitted from the solution (low, recurring).** This feature's gate lives in
  the release-only `Package.Tests`, which `dotnet test <slnx>` skips. The standing
  `scripts/baseline-tests.fsx` discovery runner is the only thing that catches it; a contributor running
  the solution alone would see green while the feature's own test never compiled. The standing baseline
  warning in tasks.md is doing real work here.

## Generalizable-code candidates

- **`treeFingerprint` is duplicated per validation script (low).** Both `validate-lifecycle-template.fsx`
  and `validate-design-system-template.fsx` carry their own tree-fingerprint/file-count helpers; the
  Feature-205 test wanted the same notion. A shared `TestSupport` fingerprint helper (relativePath →
  sha256, build-output-excluded) would let tests and scripts assert emitted-set invariance from one
  source instead of re-deriving it.

- **The env-free "verdict-core" template-gate pattern generalizes cleanly (info).** Re-deriving
  structural guarantees directly from `template.json` (symbol shape, post-action gating) — as
  Feature204/128 do and Feature205 now does — gives a deterministic, GL-free, CI-safe gate that is
  genuinely failing-first against the pre-feature manifest, while the heavy live `dotnet new` evidence
  lives under gitignored `readiness/`. This is a good default shape for any template-metadata change.

## What worked well

- Post-actions and symbols are generation *behavior*, not `sources`, so the "no emitted-file change"
  guarantee (SC-005/G1) held by construction — the live lifecycle validator independently reported
  `diff-vs-today=none` for every profile.
- Reusing today's hardened guard strings verbatim (existing-repo detection, git-presence check,
  `--allow-empty`, `continueOnError`) under the new `initGit` gate meant the edge-case behavior (C1–C4)
  was correct on the first live run — only *when* the action fires changed, not *what* it does.
