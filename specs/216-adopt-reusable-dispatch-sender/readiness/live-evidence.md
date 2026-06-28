# T012 — Live end-to-end evidence (FR-009 / SC-005)

**Date**: 2026-06-28
**Outcome**: ✅ GREEN — cross-repo dispatch authenticated and delivered; receiver pin-bump PR opened.

This is the deferred Layer-3 live proof for `FS-GG/FS.GG.Rendering#10`. It was **not** faked:
the first two live fires failed honestly and each exposed a real org-config gap, which the third
(post-fix) fire resolved.

## The three live fires (the "do NOT fake green" trail)

| # | Tag commit | Sender result | Root cause exposed |
|---|---|---|---|
| 1 | `11846e7` (Feature 215) | ❌ failure | Tag predated Feature 216 → ran the old Feature 214 bespoke-PAT sender (`TEMPLATES_DISPATCH_TOKEN` empty). |
| 2 | `a34117d` (Feature 216) | ❌ failure | Reusable sender ran, but `secrets.APP_ID`/`APP_PRIVATE_KEY` resolved **empty** — wrong secret names. |
| 3 | `c74e9ab` (name fix) | ✅ **success** | Mapped to the real org secrets `FSGG_DISPATCH_APP_ID`/`FSGG_DISPATCH_APP_PRIVATE_KEY`. |

Diagnosis confirmed by org-admin screenshot: org Actions secrets are
`FSGG_DISPATCH_APP_ID` / `FSGG_DISPATCH_APP_PRIVATE_KEY` (App `fs-gg-cross-repo-dispatch`, app_id
`4166418`). Both repos are **public**, so org secrets are consumable on the free org plan (the
"private repos" plan restriction does not apply).

## Evidence URLs

- **Sender run (Rendering, green)**: https://github.com/FS-GG/FS.GG.Rendering/actions/runs/28330983926
  - `derive` ✅ (version `0.1.52-preview.1`) · `dispatch` ✅ (App token minted, `repository_dispatch` accepted)
- **Receiver run (Templates)**: https://github.com/FS-GG/FS.GG.Templates/actions/runs/28330993001
  - `upstream-bump` fired on `repository_dispatch`; resolved version, re-pinned provider + README, pushed
    `chore/bump-fs-gg-ui-template`. The final auto-PR step was blocked by a Templates repo setting
    (`GitHub Actions is not permitted to create or approve pull requests`).
- **Pin-bump PR (Templates)**: https://github.com/FS-GG/FS.GG.Templates/pull/21
  - Opened manually from the receiver-pushed branch (branch + re-pin are the receiver's own output;
    only PR-create was done by hand). Bumps the `FS.GG.UI.Template` pin to `0.1.52-preview.1`.

## Residual follow-up (Templates-side, not #10)

Enable **Settings → Actions → General → Workflow permissions → "Allow GitHub Actions to create and
approve pull requests"** in FS.GG.Templates (or org-wide) so future bumps open automatically. Tracked
as a separate Templates issue.

## Closure

`FS-GG/FS.GG.Rendering#10` closed on this evidence; Coordination board item moved Blocked → Done.
The sender deliverable (#10's scope) is proven end-to-end; the receiver auto-PR permission is a
distinct Templates concern.
