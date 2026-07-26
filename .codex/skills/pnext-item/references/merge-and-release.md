# Merge and release obligations

Review the complete diff and verify changed paths against the issue. Ensure required checks reported on
the current head; green from an earlier SHA is not evidence. Address all actionable review threads and
re-run affected gates before merging.

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
