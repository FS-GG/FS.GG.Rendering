# Disclosed Deferred Verification — live cross-repo send (T021)

**Status**: BLOCKED — NOT run, NOT faked green.

## What is deferred

The end-to-end cross-repo dispatch (sender → receiver pin-bump PR) covering **SC-001, SC-002,
SC-005**:

1. Rendering pushes `fs-gg-ui-template/v<version>` → `template-dispatch.yml` runs the send step.
2. The send step echoes target/event/version and `gh api … /dispatches` succeeds (US2 SC-004).
3. In `FS-GG/FS.GG.Templates`, `upstream-bump.yml` fires and opens a pin-bump PR for **that exact
   version**, with **no** manual `workflow_dispatch` (US1 SC-001/SC-005), and the version arriving
   equals the version sent (SC-002, no drift).

## Why it is blocked

The send requires `secrets.TEMPLATES_DISPATCH_TOKEN`, backed by the **org-owned cross-repo
credential** tracked in **FS-GG/.github#21 / #22**. That credential is not provisioned in this repo
and is owned outside it, so the authenticated round-trip cannot run here.

## What WAS proven locally (the standing substitute)

Real, runnable evidence — not a stand-in for the contract shape, only for the network round-trip:

- **Layer 1** `actionlint .github/workflows/template-dispatch.yml` → exit 0
  ([`actionlint.txt`](./actionlint.txt)). Confirms trigger `fs-gg-ui-template/v*`, the
  `github.repository == 'FS-GG/FS.GG.Rendering'` guard, and step expressions parse.
- **Layer 2** dispatch-script `DRY_RUN=1` harness → all four scenarios pass
  ([`dry-run-us1.txt`](./dry-run-us1.txt), [`dry-run-us2.txt`](./dry-run-us2.txt)): correct version
  derivation + payload shape, and fail-loud on empty/malformed version and missing credential.
- **FR-008** `release.yml` byte-unchanged vs `origin/main` ([`baseline.md`](./baseline.md),
  [`quickstart-run.md`](./quickstart-run.md)).

## How to run it once unblocked (do NOT fake)

Per [`quickstart.md`](../quickstart.md) Layer 3, once FS-GG/.github#21 lands and
`TEMPLATES_DISPATCH_TOKEN` is configured on `FS-GG/FS.GG.Rendering`:

```sh
git tag fs-gg-ui-template/v<version>
git push origin fs-gg-ui-template/v<version>
```

Then confirm the `template-dispatch` run's send step succeeds AND a matching pin-bump PR appears in
`FS-GG/FS.GG.Templates` with no manual trigger. Record the result here and flip this file's status.

### Negative checks (US3, SC-003) — also deferred to the live environment

- From a fork, the same tag push runs nothing (the canonical-repo `if:` guard skips the job).
- A non-template `v*` release tag does not match the `fs-gg-ui-template/v*` trigger → no send.
