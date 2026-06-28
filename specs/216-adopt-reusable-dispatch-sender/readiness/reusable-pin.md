# Readiness — Reusable dispatch-sender pin & secret-name unknown (T003 / T004)

The consumption seam for Feature 216. Read directly from `FS-GG/.github` `main` on 2026-06-28.

## T003 — Pinned reusable-workflow contract

- **Path**: `FS-GG/.github/.github/workflows/dispatch-sender.yml`
- **Pinned ref (full 40-char SHA)**: `5fed2838f9ed085ffca09f4cc18b4f7bc59c1294` — `main` HEAD as of
  2026-06-28. Wired as `uses: FS-GG/.github/.github/workflows/dispatch-sender.yml@5fed2838f9ed085ffca09f4cc18b4f7bc59c1294  # main as of 2026-06-28`.
- **Last commit touching the file**: `0e73f42be0552ef73d163c505a30ca091afc767b` (2026-06-28T11:47:34Z),
  reachable from the pinned `main` HEAD. SHA-pinning (not `@main`/`@v*`) is the supply-chain-safe
  default (research R2); Renovate (org-managed) may bump it later.

### `workflow_call` interface (read from the file — source of truth)

```yaml
on:
  workflow_call:
    inputs:
      target-repo: { type: string, required: true }   # owner/name, e.g. FS-GG/FS.GG.Templates
      event-type:  { type: string, required: true }   # e.g. fs-gg-ui-template-released
      version:     { type: string, required: true }   # becomes client_payload.version
      payload:     { type: string, default: "{}" }    # extra JSON object merged into client_payload
    secrets:
      app-id:          { required: true }              # org cross-repo App id
      app-private-key: { required: true }              # PEM private key for the same App
```

Behavior confirmed in-file: a **Preflight** step fails closed (`::error::` + `exit 1`, pointing to
`.github#21`) if either App secret is empty or `target-repo` is not `owner/name`; then
`actions/create-github-app-token@v1` mints an installation token scoped to the target repo only
(`owner`/`repositories` from `target-repo`); then it `jq`-builds and POSTs
`{event_type, client_payload:{version, source_repo, source_sha, source_ref, ...payload}}`. This
confirms FR-002/FR-003 (App-token-only, no stored PAT) are satisfied by the callee, not by Rendering.

## T004 — The one genuine cross-repo unknown: the org secret names

The callee's secret **ports** are `app-id` / `app-private-key` (hyphenated `workflow_call` ids). The
exact **org secret names** that must be mapped onto them are not locally discoverable (no
org-admin/secrets-read from this checkout; `.github#21` body does not name them; Rendering is the
first consumer, so no precedent caller). 

- **Working assumption (until confirmed)**: the conventional `create-github-app-token` names
  `APP_ID` / `APP_PRIVATE_KEY`. The workflow is wired with these.
- **`secrets: inherit` cannot be used**: inherit binds caller→callee secrets by *identical name*, but
  the callee ports are hyphenated and real GitHub secret names cannot contain hyphens — so inherit can
  never bind `app-id`/`app-private-key`. Explicit mapping is mandatory (research R1), which is exactly
  why the source names must be confirmed.
- **Filed as the FR-008 cross-repo request** (T006) against `FS-GG/.github#22`/`#21`; `Rendering#10`
  stays Blocked on the Coordination board until the names + App-installation are confirmed and a live
  run authenticates (T012).
