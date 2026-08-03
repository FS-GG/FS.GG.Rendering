---
name: check-board
description: Use when the FS-GG Coordination board looks stale, blockers may have cleared, or planning needs ground truth. Reconcile project fields against live issues and surface judgement calls.
---

# check-board (FS-GG)

The board is a projection; issue state, blockers, pull requests, and `fsgg:claim` markers are truth.
Reconcile before planning and after a worker changes the board.

## Run the pass

```bash
scripts/fsgg-coord budget
scripts/fsgg-coord reconcile --json
scripts/fsgg-coord lint --json
```

`reconcile` is always fresh and is a dry-run unless `--apply` is present. Its findings are the typed
mechanical chores the engine can prove and safely repair. Review the JSON, then:

```bash
scripts/fsgg-coord reconcile --apply
scripts/fsgg-coord flush
scripts/fsgg-coord reconcile --json
scripts/fsgg-coord lint --json
```

Never translate a failed read into an empty board. `reconcile --apply` reports a repair as applied only when its own fresh post-mutation scan observes every intended field value; an accepted mutation with a stale or missing row is failed, not clean. Exit 75 means back off until the reported reset.
A queued write is not landed until `flush` and the final fresh pass confirm it.
Run the bounded executable positive, partial-projection, and missing-row receipt examples in
[mechanical reconciliation](references/mechanical-reconciliation.md#executable-receipt-examples) when
validating automation that consumes the apply document.

## Judgement boundary

`lint` findings are report-only. Investigate unreadable/unparseable blockers, undeclared paths,
unclaimed `In progress` items, open issues in `Done`, and epic roll-up questions. Do not change issue
bodies, close work, invent dependencies, or decide an epic from the reconcile pass. File or surface the
decision with evidence.

For the typed rule set, wire vocabulary, output schema, and manual investigation recipes, load
[mechanical-reconciliation](references/mechanical-reconciliation.md). For epic and decision handling,
load [judgement-findings](references/judgement-findings.md).
For rare edge cases, REST recipes, and the incident rationale behind the boundaries, load
[deep detail](references/deep-detail.md).

## Finish

Report mechanical changes, queued/failed writes, judgement findings, and the fresh post-apply result.
If planning follows, use only that final result.
