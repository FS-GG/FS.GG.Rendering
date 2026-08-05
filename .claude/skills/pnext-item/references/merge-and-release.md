# Merge and release obligations

Verify the independent-review marker and ordered confirmation chain against the current head, then review the complete
diff and changed paths against the issue. Ensure required checks reported on that head; green from an
earlier SHA is not evidence. Address all actionable review threads and re-run affected gates before
merging.

After merge, fetch the default branch and verify the PR's merge commit is reachable. If packable or
deployable artifacts changed, choose a coherent SemVer bump, pack from the merged commit, publish
byte-identical artifacts to every required feed/channel in dependency order, and verify public
download/install. Update generated manifests, dependency/compatibility registries, locks, and downstream
consumers as required by repository policy before the done stamp.

## Release column precedence

<!-- BEGIN GENERATED: fsgg-protocol:release-columns -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  This precedence was hand-authored here, and it is the single most-repaired behaviour in the
  org: seven issues (#331/#354/#531/#867/#911/#914/#921) corrected a prose copy of it while the
  engine's `unclaimColumn` was, eventually, right. #889/#900 proved the cure — the two exit-code
  tables that were GENERATED have never drifted since. This is the third. Edit Protocol.fs and
  regenerate.
-->

| release sees | the column becomes | writes? | stdout — the tell |
|---|---|---|---|
| You pass an explicit `--status <col>`. It BEATS the recorded restore and the `Ready` fallback alike — the caller naming the deliberate end state (#867/#914), which is why parking an item into a column is `release <n> --status <col>`. | `<col>` — the column you named. | yes | `released <ref> → <col>` |
| No `--status`; the live column is still the `In progress` the claim wrote, and the marker recorded NO other column (or recorded `In progress` — the same footprint written twice). | `Ready` — the fallback for a claim with nothing to restore (#481). | yes | `released <ref> → Ready` |
| No `--status`; the live column is the claim's own `In progress`, and the marker recorded a DIFFERENT column at claim time — what the claim overwrote. | the recorded column, RESTORED — a `Backlog` item returns to `Backlog`, not `Ready` (#481). | yes | `released <ref> → <recorded>` |
| No `--status`; the live column is anything OTHER than the claim's `In progress` — it was chosen DURING the lease (you parked it `Blocked`, say). `reap` asks the same question, so a lapsed lease does not revert it either (#331/#911). | that column, PRESERVED — the write is skipped, and the absence of the write is what says the column was nobody's to change. | no | `released <ref> (column left at <col>)` |
| No `--status`; the item has no `Status` set, or is not on this board — so there is no column to reset. | nothing to set. | no | `released <ref> (no column to reset — not on this board, or no Status set)` |
| The live column could not be READ (unresolvable board, or a transient failure), OR a column `release` chose to write was DEFERRED on an exhausted budget or FAILED. A column it cannot read is one it will not overwrite (#266/#331). | UNCHANGED — left exactly as it is; the lock is dropped regardless. The BARE line — no `→`, no `(...)` — is the tell, and stderr immediately above it names the repair. | no | `released <ref>` |

<!-- END GENERATED: fsgg-protocol:release-columns -->

Do not release the claim early: it is the exclusive ownership boundary for post-merge obligations.

## An engine release that becomes owed is FILED by the worker who owed it

If your merged PR touched `src/FS.GG.Coord.Cli`, `src/FS.GG.Coord.Core` or `src/FS.GG.Coord.GitHub`,
run the freshness gate against the default branch after the merge:

```sh
python3 scripts/check-engine-freshness.py --repo . --ref origin/main --report /tmp/freshness.json
```

It exits **0** when unreleased commits touch no wire surface — correctly, because no receiver can be
refused by a verb the cut does not introduce. `releaseOwed` in the report is `true` anyway. **When it
is true and no open row carries the debt, file the row.** File it; do not cut the release inside your
item — a release is separate work with its own obligations, which is why the two agents who hit this
before you were right to stop. What they had no instruction to do was leave a destination behind.

Dedupe over REST first, then file with `Class: hardening` and a narrow `Paths:` declaration, and board
it with `fsgg-coord add <n>` in the same breath. **The boarding step is the whole point and it is not
optional:** an unboarded row is invisible to `take`, which is the same silence as no row at all.

This obligation is a WORKER's because no automated actor can currently discharge it, and that was
measured rather than assumed (`.github#2231`):

- A workflow that files the row files it as a **bot**, and the off-board reconcile net excludes
  `.user.type == "Bot"` by construction (`check-board/references/deep-detail.md`). The row would be
  filed, never boarded, and never scheduled — the original defect reproduced one level down with a
  green check over it.
- Boarding it from CI instead would be the org's first Projects write from a runner; no workflow does
  one today, and `graphql-monopoly` exists to keep raw board mutations out of exactly those places.
- Making the gate itself red is rejected on measurement, not taste: engine commits land continuously,
  so a `drift > 0` bar is red on the happy path and teaches that red is noise. The gate's own
  docstring records that trade.

So the destination is a person or agent already holding board write at a release checkpoint — you.
Two occurrences on 2026-08-04 reached the board only because an agent mentioned the debt in passing;
this paragraph is what replaces that luck. If you conclude the obligation is misplaced, say so on
`.github#2231` rather than dropping it silently.
