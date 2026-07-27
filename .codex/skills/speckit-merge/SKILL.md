---
name: speckit-merge
description: Squash-merge feature branches, bump local packages, pack the local feed, push, and delete merged branches.
---

# speckit-merge

Use this repo-local command guidance when consolidating feature work onto the
trunk.

## Merge Rules

- Work from a clean tree, detect `main` or `master`, pull with `--ff-only`, and
  squash-merge feature branches before deleting them.
- Never force-push. Never delete a branch that has not been successfully merged
  or explicitly classified as stale by the operator.

## Post-Merge Package Evidence

- Every merge that touches packable `FS.GG.UI.` projects must record package
  bump evidence, pack to the local feed at `~/.local/share/nuget-local/`, align
  sample package pins, and run restore or validation for package-consuming
  samples.
- Sample work must check stale package pins with
  `scripts/refresh-local-feed-and-samples.fsx` or the `package-feed` proof
  workflow before claiming local feed readiness.
- Update the readiness ledger with the package bump, local feed pack result,
  sample package pins, restore or validation result, and any caveat.
- Committed merge evidence under `specs/*/readiness/` is ignored by default;
  add or verify the `.gitignore` allowlist and record `git check-ignore` proof
  before treating readiness artifacts as committed.

## Evidence Honesty

Screenshot evidence remains separate from live responsiveness. Prefer real
screenshot captures, disclose degraded capture, require reviewer accepted
readiness, and preserve manual caveats outside generated summary or managed
section rewrites. Keep accepted readiness explicit in the merge evidence even
when generated summary or managed section output is refreshed after the merge.

Canceled, timed-out, skipped, synthetic, substitute, degraded, pending-review,
or environment-limited checks must remain visibly caveated and must not be
reported as green.
