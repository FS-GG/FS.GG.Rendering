# T013 — Template-released dispatch fired (FR-010, INV-4) — GREEN

**Captured**: 2026-06-29. Triggered by the `fs-gg-ui-template/v0.1.53-preview.1` tag.

**Run**: https://github.com/FS-GG/FS.GG.Rendering/actions/runs/28404668533 — **conclusion: success**

| Job | Result |
|---|---|
| Derive released template version | 🟢 success |
| Notify FS.GG.Templates of template release / dispatch | 🟢 success |

✅ The Feature-216 reusable App-token dispatch-sender minted a scoped token and POSTed the
`repository_dispatch` to `FS-GG/FS.GG.Templates`, so its `upstream-bump.yml` is notified of the
`0.1.53-preview.1` template release. Templates can now open its pin-bump PR (the FR-010 SHOULD path),
in addition to the explicit #29 reply.
