# T025 — Downstream unblock (SC-004, INV-16) — 🟢 PROVEN (composition green on 0.1.53)

**Captured**: 2026-06-30. This feature **confirms and links** the downstream; the Templates re-pin is
Templates-owned (spec Assumption, contract §4).

## Dispatch → pin-bump PR (auto)

The `fs-gg-ui-template/v0.1.53-preview.1` dispatch (run `28404668533`, green) notified
`FS-GG/FS.GG.Templates`; its `upstream-bump.yml` auto-opened **FS.GG.Templates#33**
"chore: bump FS.GG.UI.Template to 0.1.53-preview.1" (branch `chore/bump-fs-gg-ui-template`).
Diff: `providers/rendering.providers.yml` `source: FS.GG.UI.Template::0.1.53-preview.1` + README/comment.

## Composition CI — GREEN against 0.1.53 + the public set ✅

**Run**: https://github.com/FS-GG/FS.GG.Templates/actions/runs/28407366769 — **success**

```
✓ provider pins FS.GG.UI.Template::0.1.53-preview.1
== summary == 33 passed, 0 failed
– SKIP: fsgg-sdd ship emitted no handoff (no ship-ready work item in a bare scaffold)
        — producer seam not exercised; the consumer/enforcement matrix still runs.
```

The composition harness does the real consumer path — install the published template + restore the
whole `FS.GG.UI.*` closure from the org feed + instantiate + build + run the govern/enforcement
matrix. **33 passed / 0 failed** confirms the now-**public** coherent set is consumer-installable with
no exit-103 and the `0.1.53-preview.1` pin is honored end-to-end (SC-004). The one SKIP is honest (the
producer `ship` seam isn't exercised by a bare scaffold), not a false-green.

> **CI-trigger note (review finding):** PR #33 was opened by `github-actions[bot]` via `GITHUB_TOKEN`,
> so by GitHub's recursion guard its `pull_request` events did **not** trigger workflows — the run sat
> `action_required` with zero jobs, and the only prior green run was stale (an old pre-0.1.53 sha).
> Re-opening the PR under a real identity triggered the `reopened` event and produced the green run
> above. Templates should make `upstream-bump.yml` open the PR with an App/PAT token (or auto-rerun) so
> this is not a manual step each release.

## Remaining (Templates-owned)

- Merge **#33** to land the pin on Templates `main` (CI is green). Then **#32** → Done.
- Registry PR **FS-GG/.github#66** awaits review/merge (contract-coherence check passing).
