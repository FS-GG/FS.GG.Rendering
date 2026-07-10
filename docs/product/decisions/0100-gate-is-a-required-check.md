# 0100 — `gate` becomes a required check; the release lane stops being expected-red

**Status**: Accepted · **Date**: 2026-07-09 · **Issue**: [FS.GG.Rendering#190](https://github.com/FS-GG/FS.GG.Rendering/issues/190)

> **Numbering.** This folder shares two number spaces (see [`../README.md`](../README.md)):
> `0001`–`0010` were repo-local, `0011`–`0014` are pointer stubs carrying **org** ADR numbers.
> The org sequence has since reached `0027`, so the local sequence cannot simply continue at
> `0015` without colliding with a future stub. Repo-local ADRs therefore resume at **`0100`**.

## Context

`gate.yml` runs a careful, fast suite: locked restore, an slnx-derived deterministic test tier,
`.fsi` surface-baseline drift, the version-coherence guard, the lifecycle-template verdict-core,
strict fsdocs, harness offscreen T0, GL degrade-and-disclose, and a separate ApiCompat job.

None of it blocked a merge. `main` had no branch protection and no rulesets, so a red run only
*informed* a merge. Release PRs #155, #159 and #163 were all merged red, and
`specs/252-retire-canvas-audio` institutionalized that: *"its version-coherence gate is red by
design."* Commit `0c7e091` named the cost — *"A gate that is red exactly when it matters teaches
people to merge past it."* High maintenance cost, no binding authority: the worst quadrant.

Two distinct red-by-construction classes were tangled together under the phrase "red by design".
They have different lifetimes, and separating them is what makes this decision safe:

1. **The release PR itself.** A PR that bumps `<FsGgUiVersion>` / `<Version>` cannot have its own
   tags — they would point at a commit that does not exist yet. **This is already fixed.**
   `scripts/validate-version-coherence.fsx` carries a RELEASE-PENDING waiver (`Inputs.PinPending`,
   `Inputs.TemplateTagPending`, `Inputs.ReleaseTagPending`): a bump waives its own missing tag while
   no successor tag in the mandated push order has been cut, and
   `FS_GG_VERSION_COHERENCE_RELEASE_LANE=1` kills all three waivers on `release.yml` so a missing tag
   at the tag commit is drift, not pending.
   A release PR is green today. `specs/252`'s instruction is **stale**, not merely inconvenient.

2. **The freeze window.** Between a bump landing on `main` and its tag triple being pushed,
   *unrelated* PRs red on `pin-no-tag` / `pkg-no-release-tag`: they did not bump, so no waiver
   applies, and the pin they inherit from `main` resolves to a tag that does not exist yet.
   Nothing in the repo pushes those tags — it is a manual operator step. This window is real, and
   it is the only thing a required gate would actually freeze.

## Decision

**`gate` becomes a required status check on `main`.** The suite stays. Specifically:

- Branch protection on `main` requires the context **`Deterministic gate`** — exactly what
  `docs/ci/cadence-map.md` §5 prescribes (FR-007). `release.yml` and `capability.yml` stay
  advisory and are never required.
- **Strict "require branches to be up to date" is deliberately NOT enabled.** §5 recommends it,
  but under ADR-0021 parallel intra-repo work it serializes every merge: each landing invalidates
  every other open PR's green. `gate` also runs on `push: main`, so a bad interleaving is caught
  post-merge rather than prevented pre-merge. That is the right trade for a repo running four
  workers at once.
- **`enforce_admins` is OFF, for now** — ~~superseded: it is now **ON**~~. Requiring the gate
  *of admins too* is the faithful reading of this decision, and it is where the repo should end up.
  But the freeze window below is not yet automated away, and with admin enforcement on, a release
  window blocks **everyone** with no escape — a wedged `main` is a worse failure than an informed
  bypass. The admin bypass is an explicit, logged action, not a silent one. **Flip it to `true` when
  the tag-cutter lands**; that is part of the follow-up, not a separate judgement call.
  > **Superseded.** `enforce_admins: true` on `main` today — there is no admin bypass. #218 landed
  > `release-tags.yml` and closed the freeze window, which is the trigger this bullet named, but
  > **nothing records when the setting was flipped, or by whom**: branch protection is not in the
  > repo tree. Read this as a state, not an event. See
  > [ADR-0103](./0103-gate-is-fully-enforced.md).
- **Rebase merging is disabled on this repo.** The guard's `bumpedInCommitUnderTest` reads
  `HEAD~1..HEAD`; a rebase-merge splits a release across two commits, so the second is seen as a
  bump-less change and the package lane reds on `main`. Squash and merge-commit both keep the whole
  release in one diff. Once `gate` is required this stops being a footgun and becomes a wedged
  `main`, so the merge method is enforced at the repo setting rather than by convention.

## Designing away the freeze

The freeze must not be permanent, or "expected-red" simply relocates from the release PR to every
PR open during a release. The fix the issue names is right: **cut the tag triple in the same
automation that lands the bump.** The guard already emits the input — on every verdict it prints a
greppable `RELEASE-PENDING:` block naming the pending tags *in the mandated push order*:

```
fs-gg-ui/v<pin>  →  fs-gg-ui-template/v<pkg>  →  v<pkg>
```

So a `push: branches: [main]` job can run the guard, read that block, and push those tags. **It
must not push them with the default `GITHUB_TOKEN`.** Events created by `GITHUB_TOKEN` do not start
new workflow runs, and both downstream lanes are `push`-triggered — `release.yml` on `v*`,
`template-dispatch.yml` on `fs-gg-ui-template/v*`. A naive auto-cutter would land all three tags
and publish nothing: the tags exist, the guard goes green, and no package ever ships. That is
exactly the half-executed publish-before-flip failure (`FS-GG/.github#250`) the waiver bounds were
introduced to stop hiding — reintroduced one layer up.

The cutter must therefore either push with a credential that does trigger downstream workflows (a
GitHub App token or PAT), or push with `GITHUB_TOKEN` and invoke the downstream lanes explicitly.
**Prefer the latter**: converting `release.yml` and `template-dispatch.yml` to `workflow_call` makes
publishing a data dependency of the cut rather than an event race, and stores no secret.

That implementation touches `.github/workflows/release.yml`, so it was sequenced behind
[#188](https://github.com/FS-GG/FS.GG.Rendering/issues/188) (which held that file and merged as
`7a5f751` while this decision was being written) rather than bundled here — this issue is the
decision, not the implementation. It is tracked as
[#218](https://github.com/FS-GG/FS.GG.Rendering/issues/218), now unblocked.

**Until the cutter lands**, the freeze window remains: it opens when a release bump merges and
closes when the operator pushes the triple, which the guard prints as copy-pasteable commands in
the run summary. It is minutes long, it is correct fail-closed behavior, and it is now bounded by a
tracked follow-up rather than by folklore.

## Consequences

- A red `Deterministic gate` blocks the merge button. The habit of merging past it ends.
- Release PRs are **not** expected-red, and no spec should say they are.
- During a release's tag window, unrelated PRs are blocked until the triple is pushed. This is the
  known, chosen cost, and it is scheduled for removal.
- `docs/ci/cadence-map.md` §5's "select **only** the `Deterministic gate`" wording predates
  `gate.yml`'s `api-compatibility-gate` job, whose own comment asks to be required. Elevating
  ApiCompat is a **separate** decision needing a §5 amendment (#219). ApiCompat stays advisory.
  *(As of 2026-07-09 it does not: §5.1 was amended, the check went green, and it is required —
  [ADR-0103](./0103-gate-is-fully-enforced.md).)*

  Deferring to §5 turned out to be load-bearing rather than merely cautious. **ApiCompat is red on
  `main` right now** — commits `3b0605b` and `7a5f751` both show `Deterministic gate` green and
  `API compatibility gate` failing. Had this decision required both contexts, enabling branch
  protection would have wedged `main` on the spot. A check that is red on `main` for a good reason
  cannot be a required check until that reason is discharged — which is precisely the question #219
  has to answer, and precisely the trap this ADR exists to stop repeating.

  > **Correction (2026-07-09, [ADR-0101](./0101-apicompat-stays-advisory.md)).** This paragraph
  > originally attributed that red to *"`FS.GG.UI.Controls BREAK (vs 0.1.52-preview.1)` … the
  > `FS.GG.UI.Canvas.Audio` surface removal from `specs/252`."* **That is wrong.** `FS.GG.UI.Canvas`
  > reports **OK**; `Audio` appears in none of the `CP####` lines; and **five** packables broke, not
  > one. The `vs 0.1.52-preview.1` baseline was itself the tell — a defect in
  > `scripts/apicompat-check.sh` selected the **oldest** published version instead of the latest, so
  > every already-shipped major re-reported as a fresh break in perpetuity. The conclusion above
  > survives (ApiCompat could not be required), but the reachability claim below it did **not**: with
  > that defect, the check would have stayed red forever and "discharge, then elevate" was
  > impossible. ADR-0101 fixes the selection and names the three genuine, undischarged breaks
  > (`Controls`, `DesignSystem`, `Themes.AntDesign`). Read the run log before restating a cause.

## Open follow-ups

- ~~[#218](https://github.com/FS-GG/FS.GG.Rendering/issues/218) — implement the tag-cutter
  (`workflow_call` route). Closing it also closes the freeze window and flips `enforce_admins` on.~~
  **Done**: `release-tags.yml` cuts the tag triple, and `enforce_admins` is `true`
  ([ADR-0103](./0103-gate-is-fully-enforced.md)).
- ~~[#219](https://github.com/FS-GG/FS.GG.Rendering/issues/219) — decide whether `API compatibility
  gate` joins the required set, amending cadence-map §5.~~ **Decided** in
  [ADR-0101](./0101-apicompat-stays-advisory.md): authorized, §5 amended (§5.1), but it stays
  advisory until it is green on `main`. Elevation is then a branch-protection change only.
  It went green and **was elevated the same day** ([ADR-0103](./0103-gate-is-fully-enforced.md)).
