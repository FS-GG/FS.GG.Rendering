---
phase: implement
date: 2026-06-28
severity: minor
---

## Process friction

The plan/tasks assumed the org reusable dispatch-sender *and* its App secrets were the open question
("does the workflow exist? are the secrets set?"). Reading `FS-GG/.github#22`'s own progress log
during T006 collapsed most of that uncertainty: the comment thread already recorded the sender as
**smoke-tested end-to-end (App token → `repository_dispatch` accepted)**. So the App + its two secrets
demonstrably exist and authenticate — the *only* residual unknown is the **exact org secret names**
the caller must reference in its explicit `secrets:` block (and the App's install scope on these two
repos). The Foundational tasks framed this as a larger "does the dependency exist" gate than it
actually was. What would have helped: a Foundational step that reads the *tracking issue's comment
history* (not just the issue body / the workflow file) before sizing the cross-repo request — the
dependency's runtime state was already documented there.

## Generalizable code

Skill family/topic: **cross-repo dependency sizing** (`cross-repo-coordination`).
Candidate helper: before filing an FR-008 request, fetch the target tracker's latest progress/
verification comment and diff its claimed state against the local assumption, so the request asks
only for the genuinely-unknown delta (here: secret *names* + install scope) instead of re-asking
about already-verified capability. The `gh issue view <n> --comments --jq '.comments[-1].body'`
read-the-latest-verification-note step is a reusable move.

Also reusable: the derive/validate half of a tag-driven sender is cleanly separable from the POST
half. `scripts/derive-template-version.sh` (strip prefix → assert semver → emit to stdout +
`$GITHUB_OUTPUT`) + its harness is a pattern any "tag → reusable-workflow caller" can copy: the
`derive` job owns the repo-unique logic, the reusable `dispatch` job owns auth+POST, and the two are
bridged by a single job output. The "a `uses:` job can't also run `steps:`" constraint forcing a
two-job split is worth capturing as a lint/guidance note.

## Skill gaps

Topic: **first-consumer wiring of a reusable `workflow_call`**. A short guidance note that (a)
`secrets: inherit` cannot bind hyphenated callee secret ports (real secret names have no hyphens), so
explicit mapping is mandatory; and (b) SHA-pin the `uses:` ref with a `# main as of <date>` comment.
Both were resolved correctly here via research R1/R2, but they are exactly the two traps a second
consumer would re-hit.

## Research links

- Reusable workflows — passing secrets / `inherit`: <https://docs.github.com/en/actions/using-workflows/reusing-workflows#passing-inputs-and-secrets-to-a-reusable-workflow>
- `actions/create-github-app-token`: <https://github.com/actions/create-github-app-token>
- Pinning third-party actions/workflows by SHA (supply-chain): <https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions#using-third-party-actions>
