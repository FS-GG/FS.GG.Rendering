# 0103 — The pre-merge gate is fully enforced: both contexts required, `enforce_admins` on

**Status**: Accepted · **Date**: 2026-07-10 · **Issue**: [FS.GG.Rendering#289](https://github.com/FS-GG/FS.GG.Rendering/issues/289)

> **Numbering.** Repo-local ADRs resume at `0100` (see [`../README.md`](../README.md)). This one
> follows [ADR-0102](./0102-symbology-secondary-heading-channel.md).

> **Supersedes in part.** The title and **Decision** of
> [ADR-0101](./0101-apicompat-stays-advisory.md) — its Context, Evidence and the baseline-selection
> fix stand unamended. Corrects the `enforce_admins` bullet of
> [ADR-0100](./0100-gate-is-a-required-check.md).

## Context

Two ADRs each authorized a future act of enforcement and named its precondition. Both preconditions
were discharged. **Neither act closed its own record**, and the documents went on describing the
state that preceded them.

- **ADR-0100** flipped `enforce_admins` **off**, calling admin enforcement *"the faithful reading of
  this decision, and where the repo should end up"*, deferred only because the release tag-window
  freeze was still a manual step: *"**Flip it to `true` when the tag-cutter lands**; that is part of
  the follow-up, not a separate judgement call."*
- **ADR-0101** authorized `API compatibility gate (breaking-change → SemVer major)` to join the
  required set on one precondition — the job must be green on `main` at the moment it is added — and
  recorded that the precondition was unmet (three real breaks).

Both preconditions were then met, and both settings now read as the ADRs said they should:

| act | precondition | how it was met | when the setting changed |
|---|---|---|---|
| ApiCompat joins the required set | green on `main` | [#225](https://github.com/FS-GG/FS.GG.Rendering/issues/225) cut and published `0.4.0-preview.1`; the gate reported `OK=17 BREAK=0 (total 17, compared 17)` | **2026-07-09**, recorded by ADR-0101's Amendment and cadence-map §5.1 at the time |
| `enforce_admins` → `true` | the tag-cutter lands | [#218](https://github.com/FS-GG/FS.GG.Rendering/issues/218) closed 2026-07-09; `7b22f9a` landed `.github/workflows/release-tags.yml` | **not recorded anywhere** — see below |

**The `enforce_admins` flip has no provenance, and this ADR does not invent one.** Branch protection
lives outside the repo tree and keeps no history the API will show, so what can be checked is only:
`enforce_admins` reads `true` today; ADR-0100 pre-authorized the flip *"when the tag-cutter lands"*;
and the tag-cutter landed. **When** the flip happened, and **who** made it, are not knowable from
here. ADR-0101 caught ADR-0100 attributing a red run to the wrong cause and wrote *"Read the run log
before restating a cause."* There is no run log for a branch-protection edit. This ADR therefore
records the flip as a **state**, not as an event with a date and an author.

Read live, branch protection on `main` now says:

```console
$ gh api repos/FS-GG/FS.GG.Rendering/branches/main/protection \
    --jq '{contexts:.required_status_checks.contexts, enforce_admins:.enforce_admins.enabled}'
{
  "contexts": [
    "Deterministic gate",
    "API compatibility gate (breaking-change → SemVer major)"
  ],
  "enforce_admins": true
}
```

### What was, and was not, already recorded

The elevation **was** written down twice — ADR-0101 grew an *Amendment (2026-07-09)* and
`cadence-map.md` §5.1 is titled *"required as of 2026-07-09"* and reads **Status: DONE**. What
neither of them did was retract the sentences that still assert the opposite, in four places:

| Where | What it still said |
|---|---|
| ADR-0101 title + **Decision** | *"stays advisory"* · *"does NOT join the required set now"* |
| `.github/workflows/gate.yml:8-9` | *"stays ADVISORY: cadence-map §5 names the one precondition it has not yet met"* |
| `scripts/apicompat-check.sh:22-23` | *"That job is not in branch protection's required set, so today a break informs a merge"* |
| `docs/ci/cadence-map.md` §3, invariant 4 | *"Branch protection is not yet enabled (§5), so no check blocks a merge today."* |

The `enforce_admins` flip was never recorded **as a decision**. [ADR-0102](./0102-symbology-secondary-heading-channel.md)
observes in passing that it is on — while reporting this very drift — but no ADR retracts ADR-0100,
which still reads *"`enforce_admins` is OFF, for now."*

### Why the drift cost something

It misleads at exactly the moment the document is reached for. ADR-0101 is what you open when a
`CP####` reddens your PR, and it tells you the red is informational. It is not: the PR is wedged,
and because `enforce_admins` is on, `gh pr merge --admin` does not bypass it either. On
[#260](https://github.com/FS-GG/FS.GG.Rendering/issues/260) / PR #283 the whole change was planned —
and written into [ADR-0102](./0102-symbology-secondary-heading-channel.md) — on the premise that the
gate could not block a merge. The merge was refused.

The false premise was also load-bearing elsewhere. `gate.yml` justified keeping the
template-payload **restore** half non-required *by citing it*: *"for the same reason ADR-0101 keeps
`api-compatibility-gate` out of the required set."* That reason no longer exists.

## Decision

**The pre-merge gate is fully enforced, and this ADR is the record of it.** The checks stay as they
are; only the documents move.

1. **Both `gate.yml` jobs are required status checks on `main`**, and `enforce_admins` is `true`.
   This ADR states the enforced configuration as fact rather than authorization. Neither is a new
   judgement: ADR-0101 §5.1 authorized the first, ADR-0100 pre-authorized the second.

2. **ApiCompat stays required.** It caught a genuine binary break on #260 and did the job it is
   registered to do (`apicompat-publicapi-gate`). The bound that makes requiring it safe holds, and
   it is `FeedUnavailable` — not `Indeterminate` — that carries it (#216/#227): a feed outage exits
   0 and informs a merge, a gate that could not execute exits 3 and blocks. See cadence-map §5.1's
   exit-code table.

3. **ADR-0101's title and Decision are superseded, not rewritten.** They record the state that
   produced them and stay legible as history; a status note points here. Its Context, Evidence, and
   the `latest_version()` SemVer-max fix are untouched and still normative — that fix is *why* the
   check can be green at all.

4. **ADR-0100's `enforce_admins` bullet is corrected in place**, because it is a description of a
   deployed setting rather than a decision, and it named its own trigger. Its open follow-up for
   #218 is closed out.

5. **`gate.yml:181` is re-examined on its own merits, and the restore half stays non-required.** Its
   stated reason is replaced with the real one, which cadence-map §5 already gives and which never
   depended on ApiCompat's status: `Template payload restore gate` has **no elevation path**. Its
   subject *is* an external feed, so a nuget.org outage is indistinguishable from a bad pin. ApiCompat
   reads a feed only to *find a baseline*, and can therefore classify a silent feed into
   `FeedUnavailable` and exit 0. The restore gate cannot — "the payload did not restore" is the
   verdict, and there is no way to tell a bad pin from an outage without asking the feed. It must
   stay unable to wedge the repo.

## Consequences

- The four stale sentences are corrected, so the answer to *"can a `CP####` block my merge?"* is
  **yes** in every place a reader would look, including the two the drift had already reached
  (`gate.yml`, `apicompat-check.sh`).
- **An admin bypass is no longer an escape.** ADR-0100 kept `enforce_admins` off so that a wedged
  `main` always had one; the tag-cutter removed the freeze window that made a wedge likely, and
  ADR-0100 traded the bypass away deliberately when it landed. Requiring ApiCompat sharpens the
  trade: a red on `main` now blocks *everyone*, and the release PR that would discharge it too. The
  only remaining escape is to remove the context from branch protection — an explicit, logged act,
  which is the property ADR-0100 wanted from the bypass in the first place.
- **A required feed-reading check is a required dependency on GitHub Packages.** Bounded, not
  eliminated, by `FeedUnavailable`.
- This is the third document to state that ApiCompat is required (with ADR-0101's amendment and
  cadence-map §5.1), and the first to state `enforce_admins` **as a decision** rather than in passing
  (ADR-0102 already notes it). Copies of a fact are places to drift; **cadence-map §5.1 remains the
  single operational reference** — it carries the exact context strings and the exit-code table — and
  the ADRs are the record of *why*.

## Open follow-ups

- ADR-0100's *"[#218] — implement the tag-cutter … Closing it also closes the freeze window and
  flips `enforce_admins` on"* is **discharged** by this ADR. #218 is closed, `release-tags.yml`
  exists, and the flip happened.
- **Nothing checks that branch protection matches what these documents say.** The ApiCompat half of
  the drift ran for a day; the `enforce_admins` half ran undated and unnoticed for longer. It spanned
  five files and seven sites — `gate.yml` alone carried three — and was found by a merge failing, not
  by CI. Branch protection is not in the repo tree, so no `gate` job can read it without a token that
  can. The gap that produced #289 is still open; it is now the only one, and it is why the
  `enforce_admins` flip above has no provenance to record.
- ADR-0101's open follow-up — *"a gate that must be green on `main` before it can be required has no
  automated check that anyone ever revisits it"* — is now a special case of the bullet above.
