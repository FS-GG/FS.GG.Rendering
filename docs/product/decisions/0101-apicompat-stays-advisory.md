# 0101 — `API compatibility gate` stays advisory, on one named precondition; cadence-map §5 amended

**Status**: Accepted · **Superseded in part by**
[ADR-0103](./0103-gate-is-fully-enforced.md) · **Date**: 2026-07-09 · **Issue**:
[FS.GG.Rendering#219](https://github.com/FS-GG/FS.GG.Rendering/issues/219)

> **Numbering.** Repo-local ADRs resume at `0100` (see [`../README.md`](../README.md)). This one
> follows [ADR-0100](./0100-gate-is-a-required-check.md), which required `Deterministic gate` and
> deferred this question.

> **⚠ The title and the Decision below no longer describe the enforced policy.** The precondition
> they name was discharged the same day (see the Amendment at the foot of this ADR), and
> `API compatibility gate (breaking-change → SemVer major)` **is a required status check on `main`**.
> A `CP####` break **blocks** your merge, and `enforce_admins` is on, so `--admin` does not bypass it.
> [ADR-0103](./0103-gate-is-fully-enforced.md) records the enforced state; this ADR is kept as
> written because it records the state that produced the decision. The Context, the Evidence, and the
> `latest_version()` SemVer-max fix stand unamended — that fix is why the check can be green at all.
> **Read the Consequences against the Amendment**: its `Indeterminate`-bounds-the-feed-risk bullet is
> retracted there (#216/#227 split out `FeedUnavailable`, which carries the bound; `Indeterminate`
> now exits 3 and *blocks*).

## Context

[ADR-0100](./0100-gate-is-a-required-check.md) made **`Deterministic gate`** a required status check
on `main` and left `gate.yml`'s sibling **`API compatibility gate (breaking-change → SemVer major)`**
advisory. It deferred to `docs/ci/cadence-map.md` §5, which said to select **only** the
`Deterministic gate` context. §5 was written when `gate.yml` had one job; the ApiCompat job was added
later, and its own comment asked to be required. Branch protection has since been adopted, so the
question is live.

ADR-0100 also observed that ApiCompat was **red on `main`**, and recorded a cause: *"the
`FS.GG.UI.Canvas.Audio` surface removal from `specs/252`."* Issue #219 repeated it.

**That cause is wrong**, and the error mattered: it framed the red as a *single*, already-understood,
self-clearing removal, which made "require it once specs/252 ships" look like the whole answer. What
the run log actually shows, on `f978c5f` / `7a5f751` / `3b0605b`:

- `FS.GG.UI.Canvas` reports **OK**. `Audio` appears in **none** of the 13 `CP####` lines.
- **Five** packables reported BREAK, not one.
- Every one of the 17 packables was baselined against **`0.1.52-preview.1`** — a version from before
  *two* intentional major bumps (`0.2.0-preview.1`, `0.3.0-preview.1`) that were cut, published, and
  are on the feed.

Investigating that uniform stale baseline found a defect in `scripts/apicompat-check.sh`.
`latest_version()` read the feed's flat-container `versions` array and took **`tail -1`**. The NuGet
API does not guarantee that array's order; nuget.org returns it oldest-first, **GitHub Packages
returns it newest-first**. So on this feed `tail -1` selected the **oldest** published version.

This was **predicted and filed**. The 2026-07-02 repo code-quality review listed, under *Low*:
*"`apicompat-check.sh` parses feed JSON with grep and assumes ordering (`:104-108`)."* It was rated
low because it read as a style complaint about parsing JSON with `grep`. The severity was in the
second clause, and the ordering assumption was already false on the feed the script actually queries.
Ranked as cosmetic, it went unactioned for a week and silently voided the gate it underpins.

The consequence is worse than a stale comparison. Every already-shipped major re-reports as a fresh
break on every subsequent run, forever. `FS.GG.UI.Scene` was flagged for a `Path.combine` change and
`FS.GG.UI.Themes.Default` for a `RolePalette` constructor change — both discharged long ago by the
majors that shipped them. **A check that re-reports every historical major is permanently red after
the first one, and therefore can never become a required check.** ADR-0100's "discharge the break,
then elevate" path was unreachable, and nothing said so.

## Evidence

Re-running the detector against the correctly-selected baseline (`0.3.0-preview.1`) separates the
two populations cleanly:

| packable | vs `0.1.52-preview.1` (buggy) | vs `0.3.0-preview.1` (correct) |
|---|---|---|
| `FS.GG.UI.Scene` | BREAK | **OK** — `Path.combine`, discharged by 0.2.0 |
| `FS.GG.UI.Themes.Default` | BREAK | **OK** — `RolePalette` ctor, discharged by a prior major |
| `FS.GG.UI.Controls` | BREAK | **BREAK** — real |
| `FS.GG.UI.DesignSystem` | BREAK | **BREAK** — real |
| `FS.GG.UI.Themes.AntDesign` | BREAK | **BREAK** — real |
| *(the other 12)* | OK | OK |

`summary: OK=14  BREAK=3  NoBaselineYet=0  Indeterminate=0  (total 17)`

The three surviving breaks are genuine, undischarged public-API removals against the latest published
version:

- `FS.GG.UI.Controls` — `CustomControlDefinition<'msg>` lost its type parameter (`CP0001`), taking
  `CustomControl.create` / `CustomControl.validate` with it (`CP0002`).
- `FS.GG.UI.DesignSystem` — `IntentPolicy` moved out of the `StyleResolver` module (`CP0001`), the
  `Theme` and `ResolvedStyle` constructors changed shape (`CP0002`), and `Theme` no longer implements
  `IComparable<Theme>` (`CP0008`).
- `FS.GG.UI.Themes.AntDesign` — `AntIntentPolicyModule.policy` re-exports the moved type (`CP0002`).

These are exactly what the gate exists to catch. They force the next `FS.GG.UI` release to be a
**SemVer major**.

## Decision (superseded 2026-07-09 — see the Amendment, and [ADR-0103](./0103-gate-is-fully-enforced.md))

> Parts 1 and 2 below landed and stand. Part 3 was true for one day. The check **is required**; read
> this section as the reasoning that made elevation reachable, not as current policy.

**`API compatibility gate` does NOT join the required set now. It stays advisory, and this ADR
records why — not as a judgement against the check, but as a statement that its one precondition is
unmet.** Three parts:

1. **Fix the baseline selection.** `latest_version()` now computes a **SemVer max** instead of
   reading either end of an unordered array. It maps the prerelease separator `-` to `~` (the one
   character GNU `sort -V` orders before end-of-string) so that `1.0.0-preview.1 < 1.0.0`, matching
   SemVer, then takes the max. This is a prerequisite for the check ever being requirable, so it
   lands with this decision rather than after it.

2. **Amend `docs/ci/cadence-map.md` §5** (new §5.1). Its "select **only** the `Deterministic gate`"
   wording was the stated blocker. It is replaced by an explicit authorization with one precondition:

   > The `API compatibility gate` job must be **green on `main` at the moment it is added** to
   > branch protection.

   §5.1 records the exact context string to add. Elevation is then a branch-protection change with
   no workflow or script edit. FR-007 is untouched: it constrains `release` and `capability`, which
   are advisory *by design*, and says nothing about `gate.yml`'s own jobs.

3. **The precondition is not met today** (3 real breaks), so the check stays advisory until the major
   that discharges them is published to the feed.

### Why the precondition, and not simply "require it"

A required check that is red on the base branch blocks **every** PR in the repo — including the
release PR that would discharge the red. The only escape is an admin bypass, i.e. a wedged `main`.
ADR-0100 flipped `enforce_admins` **off** for precisely this class of hazard, and its own text notes
that requiring both contexts on the day it landed *"would have wedged `main` on the spot."* That
remains true, now for a cause that is understood rather than mis-attributed.

### Why not require it and suppress the three breaks

`ApiCompatGenerateSuppressionFile` would make the job green immediately. Rejected: the three breaks
are real and the *only* signal forcing the major bump. Suppressing them to satisfy a gate inverts the
gate — it would ship a silent breaking change under a preview-patch version, which is the exact
failure `apicompat-publicapi-gate` is registered to prevent.

## Consequences

- ApiCompat's red on `main` now has a **true, specific, checkable** cause. The `Canvas.Audio` story
  is corrected in ADR-0100, in `gate.yml`'s comment, and here.
- The check no longer re-reports discharged majors, so **it will actually go green** once the pending
  major publishes — making elevation reachable for the first time.
- Requiring it accepts one structural cost `Deterministic gate` does not carry: the check reads the
  package feed, so a feed outage becomes merge-blocking. The detector's `Indeterminate` classification
  bounds this (a feed hiccup exits 0, it does not fail), which is why the cost is acceptable rather
  than disqualifying.
- The next `FS.GG.UI` release **must** be a SemVer major. It is not optional and not a judgement call:
  three packables have removed public API since `0.3.0-preview.1`.
- §5's "Require branches to be up to date" recommendation is corrected to match what ADR-0100 actually
  configured (deliberately **off**, because it serializes merges under ADR-0021 parallel work). The
  doc previously recommended the opposite of the deployed setting.

## Amendment (2026-07-09) — the precondition was discharged, same day

This ADR's decision was "advisory **now**, on a named precondition". The precondition is met and the
check **is required**; the decision above is left as written rather than rewritten, since it records
the state that produced it. Two corrections it could not have anticipated:

- **The major shipped.** #225 cut `0.4.0-preview.1` and published it. The gate then reported
  `OK=17 BREAK=0 (total 17, compared 17)` on `main`, and
  `API compatibility gate (breaking-change → SemVer major)` joined the required set. Note the green
  followed the **publish**, not the merge: the detector baselines off the feed, never off the repo's
  own version, so the bump alone changed nothing until the packages landed.
- **`Indeterminate` no longer carries the bound.** The Consequences bullet above justifies requiring
  the check partly on "a feed hiccup exits 0, it does not fail". #216/#227 then split
  **`FeedUnavailable`** (the feed did not answer → exit 0, informs a merge) out of **`Indeterminate`**
  (`dotnet pack` failed, so the gate never ran → exit 3, blocks). The bound still holds — a feed
  outage cannot block a merge, and a tokenless fork resolves every packable to `FeedUnavailable` — but
  it is `FeedUnavailable` that holds it. A gate that could not execute now correctly refuses to report
  a pass. See cadence-map §5.1 for the exit-code table.

## Open follow-ups

- ~~**Elevate after the major.**~~ Done 2026-07-09 (#225); the context is on branch protection.
- A gate that must be green on `main` before it can be required has no automated check that anyone
  ever revisits it. The `Blocked by` field cannot express "blocked on a release." This one was
  discharged within a day, so nothing was built for it; if a future elevation waits on a release
  again, that gap is still open.
- **The release lane published nothing on its first real run after #186.** `release.yml` added its
  runner-local feed with `dotnet nuget add source`, which writes to the nearest `nuget.config` — the
  repo's own since #186 — so the generated product could not restore the unpublished coherent set and
  the publish job (which `needs:` it) never ran, *after* the tag triple was already cut. Fixed in
  #228. The shape is worth remembering: a release lane's own validation steps can be broken by a
  change that no non-release run exercises.
