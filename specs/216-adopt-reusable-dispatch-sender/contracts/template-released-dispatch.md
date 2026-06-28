# Contract: `fs-gg-ui-template-released` repository_dispatch (via reusable App-token sender)

**Contract id**: `fs-gg-ui-template` (cross-repo registry). This document records the **sender** half
**as realized through the org reusable dispatch-sender**, superseding the Feature 214 bespoke-PAT
realization (`specs/214-release-dispatch-sender/contracts/`). The receiver remains the source of
truth for `event_type` + payload shape; the sender MUST match it. This is a **contract realization**
(mechanism change: stored PAT → run-time App token), **not a contract change** — the wire shape the
receiver requires is unchanged.

## Parties

| Role | Repo | Component |
|------|------|-----------|
| Sender (trigger + version) | `FS-GG/FS.GG.Rendering` | `.github/workflows/template-dispatch.yml` (`derive` job) + `scripts/derive-template-version.sh` |
| Sender (auth + POST) | `FS-GG/.github` | `.github/workflows/dispatch-sender.yml` (`workflow_call`, App-token; pinned by SHA) |
| Receiver | `FS-GG/FS.GG.Templates` | `.github/workflows/upstream-bump.yml` (exists; unchanged) |

## Consumption interface (Rendering → reusable workflow)

```yaml
# .github/workflows/template-dispatch.yml  (dispatch job)
jobs:
  derive:
    if: github.repository == 'FS-GG/FS.GG.Rendering'
    runs-on: ubuntu-latest
    permissions: { contents: read }
    outputs:
      version: ${{ steps.v.outputs.version }}
    steps:
      - uses: actions/checkout@v4
      - id: v
        env: { GITHUB_REF: ${{ github.ref }} }
        run: bash scripts/derive-template-version.sh

  dispatch:
    needs: derive
    uses: FS-GG/.github/.github/workflows/dispatch-sender.yml@5fed2838f9ed085ffca09f4cc18b4f7bc59c1294  # main @ 2026-06-28
    with:
      target-repo: FS-GG/FS.GG.Templates
      event-type:  fs-gg-ui-template-released
      version:     ${{ needs.derive.outputs.version }}
    secrets:
      app-id:          ${{ secrets.APP_ID }}          # working-assumption name, pending .github#22 (R1)
      app-private-key: ${{ secrets.APP_PRIVATE_KEY }} # working-assumption name, pending .github#22 (R1)
```

The callee's `workflow_call` ports are fixed: inputs `target-repo` / `event-type` / `version` /
`payload` (default `{}`); required secrets `app-id` / `app-private-key`. `secrets: inherit` is
**invalid** here — hyphenated callee ports can't bind to (hyphen-free) repo secret names; map
explicitly.

> **Working-assumption secret names.** `<ORG_APP_ID_SECRET>` / `<ORG_APP_PRIVATE_KEY_SECRET>` are the
> one genuine cross-repo unknown (research R1). The workflow is wired with the conventional
> `create-github-app-token` names `APP_ID` / `APP_PRIVATE_KEY` as a working assumption until the org
> confirms the exact names via the FR-008 cross-repo request (tasks T004/T006). When confirmed, this
> doc and the `secrets:` block above must be updated together so they don't diverge.

## Wire payload (emitted by the callee)

```jsonc
{
  "event_type": "fs-gg-ui-template-released",                 // constant — MUST match exactly (FR-001)
  "client_payload": {
    "version": "0.1.52-preview.1",                            // required; ^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$ (FR-002)
    "source_repo": "FS-GG/FS.GG.Rendering",                   // additive; inert at receiver (R5)
    "source_sha":  "…", "source_ref": "refs/tags/fs-gg-ui-template/v…"
  }
}
```

The receiver reads only `client_payload.version`; additive keys are backward-compatible.

## Preconditions (sender)

1. Trigger tag matches `fs-gg-ui-template/v*`. (FR-001/FR-006)
2. Running on `FS-GG/FS.GG.Rendering` (guard on `derive`; fork → both jobs skipped). (FR-004/US3)
3. `version` derivable + valid — else `derive` fails, `dispatch` never runs. (FR-005)
4. Org App secrets present — else the callee's preflight fails closed with a pointer to `.github#21`;
   no token minted, nothing sent. (FR-002/FR-005)

If any precondition fails, **no dispatch is sent**; for 3 and 4 the run fails visibly (FR-005).

> The workflow also exposes `workflow_dispatch` for operator inspection. A manual run lands on a
> non-tag ref, so precondition 3 fails and the run errors loudly — never a spurious send.

## Postconditions (receiver — out of scope, documented for the round trip)

- A successful dispatch causes `upstream-bump.yml` to open or update a pin-bump PR repointing the
  `fs-gg-ui-template` pin to the dispatched version, with no manual `workflow_dispatch` (FR-007).

## Difference from the Feature 214 realization

| Aspect | Feature 214 (retired) | Feature 216 (this) |
|--------|----------------------|--------------------|
| Auth | stored `secrets.TEMPLATES_DISPATCH_TOKEN` (long-lived PAT) | run-time App installation token, scoped to target |
| Who POSTs | `scripts/template-released-dispatch.sh` (`gh api`) | `FS-GG/.github` reusable workflow |
| Rendering keeps | trigger + guard + derive + validate + send | trigger + guard + **derive + validate only** |
| Stored cross-repo secret on Rendering | yes (the failure cause) | **none** (SC-003) |
