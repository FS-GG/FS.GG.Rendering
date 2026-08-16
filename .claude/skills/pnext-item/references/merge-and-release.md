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

## Which post-merge acts are AUTOMATED on merge into `main`

Read this table before you write a `fsgg:delivery-obligation`. An obligation is a control only if the
act it names is one a human still has to perform; declaring a manual obligation for something the merge
does for you produces an artifact that is reviewed, gated, and inert.

**This is measured, not cautionary.** `.github#2512` declared obligation 1, `coherent-set-0.50.6-release`,
as a manual act: tag `coord-engine/v0.50.6`, `kit/v0.50.6` and `drivers/v0.50.6` at the merge commit,
then verify both feeds. It was reviewed by two independent critics across three rounds, accumulated two
sub-obligations, and was explicitly host-gated — *"merge, then STOP and report. Do NOT begin obligation
1"* — on the grounds that it was the session's only irreversible act. **The merge performed it.** A
worker verified zero `0.50.6` tags at 15:43:56Z; by 15:46:48Z all three tags existed at the merge commit
and all three release workflows had run. `.github#2533` is that defect.

**A TRIGGER IS NOT AN ACT, and row 1 turns on which version line you are on.** `kit-auto-publish.yml`
fires on every merge. The program it runs, `scripts/kit-auto-publish.py`, deliberately admits **only a
same-line PATCH bump** (`.github#2442`) and terminally refuses anything else. Every coherent-set release
is a MINOR by design (`.github#2402`). So on a minor the workflow fires and cuts nothing, and the
release is genuinely yours.

**AND A TAG IS NOT A PUBLICATION.** This is `.github#2654`, and it is the third correction in the
same series: `.github#2533` moved the question from *"the workflow fires"* to *"the program decides"*,
`.github#2571` from *"the program decides"* to *"the program decides to **tag**"*, and this one from
*"the program decides to tag"* to *"the artifact is **published**"*. Since `.github#2600`'s release
saga, `release-kit` / `release-coord-engine` / `release-drivers` **do not pack anything** — each
downloads the `coherent-set/v<version>` draft release that `release-saga-prepare.yml` prepared. That
workflow was `workflow_dispatch`-only, and `kit-auto-publish` tags with no operator involved, so on
the first PATCH after the saga landed the two rails met for the first time and did not join:
`0.58.1` was tagged in all three namespaces at `a415652f` and **all three release workflows died
within seconds on `release not found`**. Zero packages, three immutable tags. `kit-auto-publish`'s own
run was **green**. `kit-auto-publish` now CALLS `release-saga-prepare` and pushes its tags only from a
job that `needs:` that call, so the bytes exist before the tags that consume them — but read the last
paragraph of this section before you treat any of that as evidence that something shipped.

| act | performed automatically by | trigger | so the obligation is |
|---|---|---|---|
| preparing the coherent set — packing all three members once, preflighting them against both feeds, and placing them in the `coherent-set/v<version>` draft release — and then tagging `kit/v<version>`, `coord-engine/v<version>`, `drivers/v<version>` at the merge commit, thereby starting `release-kit` / `release-coord-engine` / `release-drivers` on bytes that already exist — **when `<Version>` is the next PATCH above both feed frontiers** | `.github/workflows/kit-auto-publish.yml`, which calls `.github/workflows/release-saga-prepare.yml` and pushes the tags only from a job that `needs:` that call (`.github#2654`) | `on: push: branches: [main]`, unfiltered — **every** merge starts it, and on a next-patch candidate `kit-auto-publish.py` answers `tag` | **verify the automatic release**, never perform it — and verify the FEEDS, not the run |
| the same tagging — **when `<Version>` is a MINOR, which every coherent-set release is** | nothing | the same merge starts the same workflow, and `kit-auto-publish.py` refuses it terminally: `candidate-not-next-patch`. Coherent-set versions publish through `.github#2409`'s release path instead | **yours**, and it must be declared — with a token that NAMES it |
| regenerating `registry/coordination-kit-skill-manifest.json` and the other rostered projections | `skill-registry-coherence` / `projections` autofix | scheduled + `push` | none — but a projection you did not regenerate reds `main` until the bot catches up, so regenerate it in your PR |
| recording a published coherent set in `registry/dependencies.yml` / `registry/CHANGELOG.md`, bumping a downstream repo, filing a follow-up row | nothing | — | **yours**, and it must be declared |

**On the PATCH line, the token you want is `kind=release-verification`.** Read that twice, because the
obvious choice is the wrong one: `package-release` — and its artifact-specific spellings `kit-release`,
`coord-engine-release`, `drivers-release`, `coherent-set-release` — are exactly the tokens
`check-kit-published-coherence.py --obligation-arm` **flags there**, because on that line they name an
act the merge performs. Declaring one on a next-patch PR into `main` reds
`kit-published-coherence / pr-arm`, which is the gate doing its job.

**On the MINOR line those same tokens are the RIGHT ones, and the gate accepts them.** This is
`.github#2571`, and it is worth stating plainly because the rule above used to have no exception and the
gate used to have no second question. A worker cutting a coherent set is performing the release; the
token should say so. Do not reach for `release-verification`, and above all do not reach for an unmapped
token like a bare `release` to get past the gate — `.github#2527` / PR #2532 did exactly that, silently,
and choosing a word to evade a control is the thing the control exists to prevent.

| you owe | use | why |
|---|---|---|
| confirming the automatic release published what you meant (patch line) | `release-verification` | comparing published bytes to canonical is not something any workflow does |
| performing a release by hand on a candidate the merge WILL publish (patch line) | `package-release` and friends | **flagged** — the merge already did it; rewrite the obligation as verification |
| performing a coherent-set release by hand on a MINOR the merge will NOT publish | `coherent-set-release`, or the artifact-specific spelling | **accepted** — the gate asks `kit-auto-publish.py` what it would decide, not just whether the workflow fires |

Those tokens are the join key the gate uses, and it restates neither half of the answer: it reads
`kit-auto-publish.yml`'s live `on:` block **and** calls `kit-auto-publish.py`'s own `decide()` over the
version this PR ships and the frontier it would ship above. If you change one half, change the other.
`.github#2533`'s own PR declares `kind=release-verification`; `.github#2571`'s does too, because it ships
a PATCH — each item self-applying rather than exempting itself. Neither is a worked example of the minor
leg, which is by construction: the leg that needs an example is the one you will meet at a coherent-set
cut, and `tests/kit-published-coherence/run.sh` carries it as an executable one.

**Verifying an automatic release means reading the PUBLISHED BYTES against canonical.** A green release
workflow proves the job ran, not that what shipped is what you meant: `release-kit` run `31716998803`
succeeded while publishing an `FS.GG.Kit 0.50.6` whose `pnext-item` was stale, because the tag preceded
an open PR that was correcting it. Compare the artifact, not the check mark.

**And a green `kit-auto-publish` run proves less than that — it does not prove a release workflow even
STARTED successfully, let alone published.** `kit-auto-publish` ends at the tag push; everything after
that is three separate workflows on a separate trigger, and a refusal there is invisible from the run
you were watching. On `0.58.1` the auto-publish run succeeded, the three release runs failed seconds
later, and both feeds sat at `0.58.0` for hours with nobody's check red (`.github#2654`). So the
verification that discharges a `release-verification` obligation is a **feed read**, and it is
two questions, not one:

```sh
# 1. does each feed SERVE the version at all?
curl -fsS https://api.nuget.org/v3-flatcontainer/fs.gg.kit/index.json | jq -r '.versions[-3:]'
gh api 'orgs/FS-GG/packages/nuget/FS.GG.Kit/versions?per_page=100' --paginate --jq '[.[].name] | .[:3]'
# 2. do the bytes it serves match canonical, and does the served nuspec name the tagged commit?
```

Neither question is answered by looking at Actions. If the version is absent, the obligation is not
discharged and the tags are the evidence of what still has to happen — not evidence that it did.

### A pre-act condition on an automated act belongs in a PRE-MERGE gate

If your obligation carries a *stop — do not do X* condition, ask when it can be read. For an automated
act, the answer is **before the merge**, and there is no second answer.

`.github#2512`'s sub-obligation `1b` was written as a stop-do-not-tag condition to be evaluated
immediately before tagging. There was no such moment. Its condition happened to hold at the merge commit,
so nothing shipped that it would have refused — **luck, not process**. Had it not held, the tags would
still have been cut and the gate would have discovered it afterwards, against immutable tags and
`.github#1772`'s sibling-tag precondition, which is what makes `0.50.1` and `0.50.5` permanently
two-of-three.

**A stop condition that can only be read after the act is not a stop condition.** Move it to a gate that
runs on the pull request, or state plainly in the obligation that the condition is a post-hoc
*observation* and name the repair if it fails. Do not write it as a decision point. A useful pre-merge
form asks something answerable while the PR is open — *"does an open PR modify a path this merge is
about to publish?"* — which is exactly the question that would have caught `0.50.6` shipping a stale
`pnext-item`, and which is unanswerable once the tag exists.

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
