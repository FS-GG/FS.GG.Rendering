# T025 — Downstream unblock (SC-004, INV-16) — PARTIAL (dispatch landed; full unblock awaits visibility)

**Captured**: 2026-06-30. This feature **confirms and links** the downstream; it does not perform the
Templates re-pin (spec Assumption, contract §4).

## What landed automatically ✅

The `fs-gg-ui-template/v0.1.53-preview.1` dispatch (run `28404668533`, green) notified
`FS-GG/FS.GG.Templates`, whose `upstream-bump.yml` **auto-opened a pin-bump PR**:

- **FS.GG.Templates#33** — "chore: bump FS.GG.UI.Template to 0.1.53-preview.1"
  (branch `chore/bump-fs-gg-ui-template`).

So the producer→consumer notification path is proven working end-to-end (Feature 216 sender +
Templates receiver).

## What is still blocked ❌

- **FS.GG.Templates#32** ("Bump FS.GG.UI.Template pin … unblocks full composition path") is still
  `state: OPEN`, labels `blocked`, `roadmap`.
- It cannot fully unblock until `FS.GG.UI.Template` is **org-readable** — the composition CI
  (`FSGG_COMPOSITION_FULL=1 tests/composition/run.sh`) installs the template with a consumer token and
  would still hit **exit 103** while the package is `private` (see `no-103-install.md`,
  `visibility-internal.md`). PR #33 can be opened, but the composition gate it must pass is the very
  `packages: read` install that 103s.

## Acceptance (Templates-owned, after the visibility flip)

```bash
# in FS.GG.Templates, once FS.GG.UI.Template is internal:
gh pr checks 33 --repo FS-GG/FS.GG.Templates      # composition install no longer 103s
# FSGG_COMPOSITION_FULL=1 tests/composition/run.sh -> 29/29
```
Link that `29/29` run here when Templates re-pins (SC-004). **Until visibility flips, this stays
`environment-limited`.**
