---
phase: implement
date: 2026-06-28
severity: major
---

## Process friction

The plan scoped `release.yml` as "DRIVEN, not edited" and treated the release pipeline as a
known-good black box. But this was the **first real end-to-end run** of `release.yml` (the org
GitHub Packages feed was empty; `publish-packages` was added DRAFT in #15 and had never published),
and it failed closed on **three pre-existing pipeline bugs** that the local pre-flight could not
surface:
1. `template-product-tests` had no feed for `FS.GG.UI.*` (NU1101) — the set is only packed by the
   *downstream, gated* publish job (chicken-and-egg).
2. `package-tests` checkout lacked `fetch-depth: 0` → no tags on the runner → the Feature 209
   coherence mirror fails closed.
3. `package-tests` never built the solution → surface-baseline reflection tests saw "assembly
   built = false" (they read `src/<Pkg>/bin/Debug/net10.0/*.dll`).

What would have helped: a planning check that a "DRIVEN, not edited" workflow has **actually run
green end-to-end at least once** before depending on it for a release/closure. A green local
pre-flight (tags + built assemblies + populated local feed all present on the dev box) gave false
confidence — the clean CI runner had none of those. Also: the "closes #9" keyword in the *first*
(failed) release's notes auto-closed #9 at release-create time, before any publish — release notes
should not carry auto-close keywords until the publish is confirmed.

## Generalizable code

Skill family/topic: **release/CI pipeline self-test** (fs-gg-diagnostics / package-feed proof).
Candidate helper: a release-gate **pre-flight that reproduces the CI runner's clean conditions** —
no ambient tags, no built bin/, no populated feed — by packing the coherent set to a throwaway feed
and running `package-tests` + `template-product-tests` against it. This would catch exactly the
three bugs above locally. The `release.yml` `template-product-tests` fix (pack coherent set to
`RUNNER_TEMP` feed before scaffolding) mirrors `scripts/dev-repack.fsx` and is a reusable pattern
worth a shared composite action / script.

## Skill gaps

Topic: **first-publish readiness**. A skill that, before the first real release to an empty feed,
asserts the release workflow's three invariants (tags fetched, SUT assemblies built, consumed
packages resolvable from *some* feed at gate time) and flags any job that depends on an artifact a
later gated job produces. Would have turned a failed live release into a caught pre-flight.

## Research links

- NuGet restore sources / NU1101: <https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1101>
- actions/checkout fetch-depth & tags: <https://github.com/actions/checkout#fetch-all-history-for-all-tags-and-branches>
- GitHub auto-close keywords: <https://docs.github.com/en/issues/tracking-your-work-with-issues/linking-a-pull-request-to-an-issue>
