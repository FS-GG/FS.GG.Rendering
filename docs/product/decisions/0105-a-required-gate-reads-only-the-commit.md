# 0105 — A required gate's verdict is a function of the commit alone

**Status**: Proposed · **Date**: 2026-07-13 · **Issue**: [FS.GG.Rendering#738](https://github.com/FS-GG/FS.GG.Rendering/issues/738)

> **Numbering.** Repo-local ADRs resume at `0100` (see [`../README.md`](../README.md)). This one
> follows [ADR-0104](./0104-canvas-loop-is-a-simulation-primitive.md).

> **Refines.** [ADR-0103](./0103-gate-is-fully-enforced.md) decided *which* contexts are required and
> turned `enforce_admins` on. It did not say what a context must be in order to be **eligible** to be
> required. This ADR supplies that missing precondition. Nothing in ADR-0103 is reversed.
>
> **Sibling.** [ADR-0032](https://github.com/FS-GG/.github/blob/main/docs/adr/0032-the-lock-hash-must-not-depend-on-the-machine.md)
> (org) — *the lock file's `contentHash` must not depend on the machine.* Same family: a verdict must
> not be a function of mutable state outside the artifact it judges. ADR-0032 covers the restore; this
> covers the gate.

## Context

The reasoning this ADR ratifies is **already written down in this repo, correctly, and applied to only
one gate.** `gate.yml`, on the template-payload restore gate, verbatim:

> The restore-grounded half — does the payload actually resolve, and to a stable graph — needs
> nuget.org and runs in `template-payload-restore-gate` below, which stays NON-required. […] The
> restore gate's SUBJECT *is* the feed — "the payload did not restore" is the verdict, and a
> nuget.org outage is indistinguishable from a bad pin. **It must stay unable to wedge the repo.**

That is the right rule. It was never generalized, and the gate immediately above it in the same job
breaks it.

**`scripts/check-frozen-mirrors.fsx` runs inside the required `Deterministic gate` job, and reads
another repository's default branch.** At `:85-86`:

```
[ "api"
  "repos/FS-GG/.github/contents/registry/skills.yml"
```

No `?ref=` is pinned, so the call resolves against `FS-GG/.github`'s **current `main`**. The verdict
this gate returns for a commit in *this* repo is therefore a function of a commit in *another* repo,
made later, by someone else.

The consequence is not theoretical and is not rare:

- **[#738](https://github.com/FS-GG/FS.GG.Rendering/issues/738)** — a canonical body moved in
  `FS.GG.Game`; the required gate went red on `main` and on every open PR; no change in this repo was
  involved, and no change in this repo could have prevented it. Every merge was blocked until a
  human hand-copied bytes.
- **[#696](https://github.com/FS-GG/FS.GG.Rendering/issues/696)** records the rate: *"Nine 'main is
  RED, nothing can merge' incidents in three days; this is one of the recurring sources."* Commit
  `a9e3850c` is one of them — `FS.GG.Game` landed `Persistence.interpret`, and Rendering's required
  gate reddened with no Rendering change involved.
- The job is named **"Deterministic gate."** It is not deterministic. It cannot be: it has an input
  that is not in the tree.

Note what this ADR does **not** claim. A required gate may touch the network. `api-compatibility-gate`
is required (ADR-0103) and reads the published feed — and it is **compliant**, because
`scripts/apicompat-check.sh:41-49` classifies a silent feed as `FeedUnavailable → exit 0`: the feed is
read only to *find a baseline*, and its absence cannot turn a green commit red. The distinction is not
*network / no network*. It is *whose commit decides the verdict*.

## Decision

**A context may be `required` only if its verdict on a given commit is stable: re-running it on an
unchanged commit, at any later time, with no change in this repository, must return the same verdict.**

The operational test, which any reviewer can apply to a proposed gate in one sentence:

> **Could this gate turn an already-green commit red without anyone changing this repository?**
> If yes, it may not be required.

Three ways to satisfy it, in order of preference:

1. **Pin the external input into the tree.** The gate reads a digest, ref, or version *committed here*.
   The external thing moving then produces a **PR in this repo** — reviewable, schedulable, batchable —
   instead of a red `main`. This is the remedy for `check-frozen-mirrors`: the org registry already
   carries a `sha256` per skill row, and the guard already has a hand-maintained waiver mechanism
   (`:154-234`) that pins a digest by hand. Committing the expected digest turns that hand-maintained
   lie into the mechanism.
2. **Classify "could not determine" as a non-verdict that does not fail.** The `apicompat-check.sh`
   pattern: the check names what it could not do (`FeedUnavailable`), exits 0, and proves nothing —
   which is honest, and which is *not* a pass in any sense that another gate may rely on.
3. **Leave it non-required.** Correct when the external thing *is* the subject of the check, as
   `gate.yml` already argues for the restore gate.

Option (2) is available only where absence is distinguishable from failure. Where it is not — where "I
could not read it" and "it is wrong" produce the same observation — the check must take option (1) or
option (3). `check-frozen-mirrors.fsx:76-79` currently reasons that an unreadable registry is a HARD
FAIL, *"not a skip: a check that did not run has proved nothing."* That reasoning is sound **and it is
an argument against the gate being required**, not for it: a gate that must fail closed on an input it
does not own is a gate that hands the merge button to another repo.

## Consequences

**Immediately.** `check-frozen-mirrors` either pins the canonical digest in-tree (preferred — it
converges with [ADR-0014](https://github.com/FS-GG/.github/blob/main/docs/adr/0014-skill-vendoring-one-manifest-one-materialize-verify.md),
which already decided that skills are content-addressed with one canonical body) or leaves the required
set. It may not stay as it is.

**Standing.** Every new gate proposed for the required set must answer the one-sentence test above in
its PR description. This is the part of the ADR that does work after today: the reason the rule is
worth writing is not that one gate violates it, but that **nothing currently stops the next one from
doing so.** Two gates were added in the two days before this ADR (`gate(#655)` and `gate(#698)`, both
the skill-refs gate) — neither was asked the question above, because nothing required it.

**Not covered.** This ADR says nothing about whether a gate should exist, only about whether it may be
*required*. The question of gates that check hand-maintained copies of a fact against each other —
which is most of them — belongs to [ADR-0106](./0106-governance-is-generated-not-compared.md).

**A verdict that depends on the world is a verdict about the world, not about the commit.** Branch
protection asks one question — *may this commit merge?* — and the only honest inputs to that question
are the commit and things the commit names.
