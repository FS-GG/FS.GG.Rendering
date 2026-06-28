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
| 3 | `c74e9ab` (name fix) | ✅ **success** | Mapped to the real org secrets `FSGG_DISPATCH_APP_ID`/`FSGG_DISPATCH_APP_PRIVATE_KEY`. Sender green; receiver delivered but its auto-PR step blocked by a Templates Actions setting (PR opened manually as #21). |
| 4 | `90e4f86` (re-fire) | ✅ **fully automated** | After enabling Templates "Allow Actions to create PRs", the receiver auto-opened the PR with no manual step. |

Diagnosis confirmed by org-admin screenshot: org Actions secrets are
`FSGG_DISPATCH_APP_ID` / `FSGG_DISPATCH_APP_PRIVATE_KEY` (App `fs-gg-cross-repo-dispatch`, app_id
`4166418`). Both repos are **public**, so org secrets are consumable on the free org plan (the
"private repos" plan restriction does not apply).

## Evidence URLs

**Fully-automated run (fire #4, the canonical proof):**
- **Sender run (Rendering, green)**: https://github.com/FS-GG/FS.GG.Rendering/actions/runs/28331280883
  - `derive` ✅ (version `0.1.52-preview.1`) · `dispatch` ✅ (App token minted, `repository_dispatch` accepted)
- **Receiver run (Templates, all steps green incl. Open PR)**: https://github.com/FS-GG/FS.GG.Templates/actions/runs/28331286765
- **Auto-opened pin-bump PR (Templates)**: https://github.com/FS-GG/FS.GG.Templates/pull/23
  - Author `github-actions[bot]` — opened by the receiver itself, no manual step. Bumps `FS.GG.UI.Template` to `0.1.52-preview.1`.

**Earlier run (fire #3) — sender proven, receiver auto-PR blocked:**
- Sender https://github.com/FS-GG/FS.GG.Rendering/actions/runs/28330983926 ✅;
  receiver https://github.com/FS-GG/FS.GG.Templates/actions/runs/28330993001 delivered + pushed the branch,
  but the auto-PR step failed (`GitHub Actions is not permitted to create or approve pull requests`);
  PR opened manually as #21 (since closed/superseded by the automated #23).

## Residual follow-up — RESOLVED

The Templates Actions PR-creation setting was enabled and proven by fire #4. `FS-GG/FS.GG.Templates#22`
closed.

## Closure

`FS-GG/FS.GG.Rendering#10` closed on this evidence; Coordination board item moved Blocked → Done.
The sender deliverable (#10's scope) is proven end-to-end; the receiver auto-PR permission is a
distinct Templates concern.
