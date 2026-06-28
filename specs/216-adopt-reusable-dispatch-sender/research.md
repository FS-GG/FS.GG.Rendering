# Phase 0 Research: Adopt Reusable App-Token Dispatch-Sender

Resolves every NEEDS CLARIFICATION before design. The decisive finding is up front.

## R0 — Does the reusable dispatch-sender exist, and what is its interface?

**Decision**: Consume `FS-GG/.github/.github/workflows/dispatch-sender.yml` via `workflow_call`. It
**exists on `main`** (commit `5fed283`) despite `.github#22` being OPEN — the issue tracks the
broader Renovate-managers scope; the workflow itself is delivered.

**Interface (read from the file, source of truth):**

```yaml
on:
  workflow_call:
    inputs:
      target-repo:  { type: string, required: true }   # owner/name, e.g. FS-GG/FS.GG.Templates
      event-type:   { type: string, required: true }   # e.g. fs-gg-ui-template-released
      version:      { type: string, required: true }   # becomes client_payload.version
      payload:      { type: string, default: "{}" }    # extra JSON object merged into client_payload
    secrets:
      app-id:           { required: true }             # org cross-repo App id
      app-private-key:  { required: true }             # PEM private key for the same App
```

It preflights that both App secrets are non-empty (else `::error::` + exit 1 with a pointer to
`.github#21`), mints an installation token **scoped to `target-repo` only** via
`actions/create-github-app-token@v1`, then `jq`-builds and POSTs
`{event_type, client_payload:{version, source_repo, source_sha, source_ref, ...payload}}`.

**Rationale**: Reading the real `workflow_call` contract turns this from a speculative adoption into
a concrete wiring job, and confirms FR-002/FR-003 (App-token-only, no stored PAT) are met by the
callee, not us. **Alternatives rejected**: (a) keep the bespoke PAT sender — the exact failure mode
of Feature 214; (b) author our own `create-github-app-token` step — duplicates org-maintained auth
logic, violating FR-003 and the "maintain once org-wide" intent.

## R1 — What are the org secret names to map onto `app-id` / `app-private-key`?

**Decision**: Map **explicitly** in the caller's `secrets:` block:

```yaml
secrets:
  app-id:          ${{ secrets.<ORG_APP_ID_SECRET> }}
  app-private-key: ${{ secrets.<ORG_APP_PRIVATE_KEY_SECRET> }}
```

The two `<…>` names are the **one genuine cross-repo unknown**. They are not locally discoverable
(no org-admin/secrets-read permission from this checkout; `.github#21`'s body does not name them;
**no existing caller in the org** reveals a precedent — Rendering is the first consumer). Treat the
exact names as the content of the FR-008 cross-repo request to `.github#22`/org-admin; the
conventional `create-github-app-token` names are `APP_ID` and `APP_PRIVATE_KEY`, used as the working
assumption until confirmed.

**`secrets: inherit` is rejected**: inherit matches caller→callee secrets **by identical name**, but
the callee's secret ports are `app-id`/`app-private-key` (hyphenated `workflow_call` ids), and real
GitHub secret names cannot contain hyphens — so inherit can never bind them. Explicit mapping is
mandatory, which is also why the exact source names must be confirmed.

**Rationale**: Explicit mapping is the only mechanism that works and it documents the dependency at
the call site. **Alternative rejected**: `secrets: inherit` — cannot bind hyphenated callee ports.

## R2 — How is the reusable workflow pinned (`@ref`)?

**Decision**: Pin by **commit SHA**: `uses: FS-GG/.github/.github/workflows/dispatch-sender.yml@5fed283…`
(full 40-char SHA in the file), with a trailing `# main as of 2026-06-28` comment and a note that
Renovate (org-managed) may bump it.

**Rationale**: SHA pinning is the supply-chain-safe default for third-party-*shaped* `uses:` refs and
matches the spec's "ready to go green the moment the dependency lands" intent without silently
absorbing unrelated changes to the reusable workflow. **Alternatives rejected**: `@main` (mutable —
an unrelated edit to the org workflow could change Rendering's release behavior without review);
`@v<tag>` (the reusable workflow is not tag-released today — no tag to pin).

## R3 — What happens to `scripts/template-released-dispatch.sh`?

**Decision**: **Repurpose** its derivation+validation half into a new single-responsibility helper
`scripts/derive-template-version.sh` (strip `refs/tags/fs-gg-ui-template/v`, assert
`^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$`, emit `version` to stdout **and** `$GITHUB_OUTPUT`),
and **retire** the old script's credential-guard + `gh api` POST steps (the reusable workflow now
owns the send). Retarget the `test-template-released-dispatch.sh` harness to
`test-derive-template-version.sh`, keeping the non-tag / empty / malformed edge assertions and
dropping the now-irrelevant credential-missing and DRY_RUN-payload cases.

**Rationale**: Preserves Feature 214's tested derivation logic (reuse, Principle V) while deleting
the dead send path so the helper's name no longer lies about its job (Principle III). The
`DRY_RUN`/`GH_TOKEN` machinery is obsolete once the POST moves to the callee. **Alternatives
rejected**: (a) keep the old script and call it with `DRY_RUN=1` just to print a version — keeps dead
send code and a misleading name; (b) inline derivation as a raw workflow step — loses the
unit-testable harness that gives fail-before/pass-after evidence without a live run.

## R4 — Two-job shape and the canonical-repo guard

**Decision**: Two jobs in `template-dispatch.yml`:

1. `derive` — `runs-on: ubuntu-latest`, `if: github.repository == 'FS-GG/FS.GG.Rendering'`,
   `permissions: contents: read`; `actions/checkout@v4` → run `derive-template-version.sh` → expose
   `outputs.version`.
2. `dispatch` — `needs: derive`, `uses: FS-GG/.github/.github/workflows/dispatch-sender.yml@<sha>`,
   `with: { target-repo: FS-GG/FS.GG.Templates, event-type: fs-gg-ui-template-released,
   version: ${{ needs.derive.outputs.version }} }`, `secrets: { app-id, app-private-key }`.

The canonical-repo guard lives on `derive`; because `dispatch` `needs: derive`, a fork (where
`derive` is skipped) never reaches `dispatch`, so forks neither send nor touch the credential
(FR-004, US3). A caller job that `uses:` a reusable workflow **cannot also run steps**, which is
exactly why derivation must be its own preceding job rather than a step before the `uses:`.

**Rationale**: Minimal job graph that satisfies the `workflow_call` "no steps in a `uses:` job"
constraint, keeps the guard authoritative, and threads the derived version across the boundary via a
job output. **Alternative rejected**: a single job — impossible, `uses:` and `steps:` are mutually
exclusive at the job level.

## R5 — Receiver compatibility (extra `client_payload` fields)

**Decision**: No receiver change; no Rendering action needed. The reusable workflow adds
`source_repo`/`source_sha`/`source_ref` alongside `version`. `FS-GG/FS.GG.Templates`
`upstream-bump.yml` reads **only** `client_payload.version` (confirmed by reading its head), so the
extra fields are inert. The `fs-gg-ui-template` contract's required shape (`event_type` +
`client_payload.version` matching the semver regex) is unchanged — this remains a contract
*realization*, not a contract change.

**Rationale**: The receiver is the contract's source of truth and ignores unknown fields; additive
payload keys are backward-compatible. **Alternative rejected**: passing a stripped payload via the
`payload` input — unnecessary; the defaults are harmless and removing them would fight the callee.

## R6 — Disjoint triggers / `release.yml` untouched

**Decision**: Keep `on: push: tags: ['fs-gg-ui-template/v*']` (+ inspection-only `workflow_dispatch`).
`release.yml` triggers on `v*`; the two glob patterns cannot match the same tag
(`fs-gg-ui-template/v…` has a slash a bare `v…` tag never carries), so the release pipeline is
untouched and a byte-diff of `release.yml` vs `origin/main` must show **no change** (SC-004/FR-006).

**Rationale**: Trigger disjointness is the structural guarantee that adopting the new sender cannot
regress package release gating. **Alternative rejected**: folding the dispatch into `release.yml` —
would mutate the byte-frozen release workflow and couple two independently-failing concerns.

## Open dependency (carried to FR-008)

`R1`'s exact secret names + the runtime fact that `.github#21`'s App secrets are actually set and
authenticate are confirmable **only by a live run**. Until a real `fs-gg-ui-template/v*` tag is
pushed and the receiver PR appears, board item `Rendering#10` stays **Blocked** on live evidence —
the Rendering-side adoption is otherwise complete and ready (FR-008/FR-009).
