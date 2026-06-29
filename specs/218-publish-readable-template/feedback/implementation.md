# Feature 218 — implementation-phase feedback

Per-phase fs-gg-ui / Spec Kit friction and generalizable-code candidates captured during
`/speckit-implement`.

## Process friction

- **No REST endpoint for package visibility (HIGH).** The #26 gate (`private → internal`) is an
  org-admin **UI-only** action — `gh api` can read but not write visibility. This makes the visibility
  half of every "publish a private package for cross-repo consumption" feature **un-completable by an
  agent/CI**, and forces a human operator hand-off mid-feature. It is the single hardest-to-automate
  step in the whole composition fabric. *Generalizable note:* any feature that depends on a newly-cut
  package being consumer-readable should front-load this flip as a P0 operator task, not discover it at
  the combined-gate checkpoint.

- **Owner token masks exit-103 (MEDIUM).** The in-session token can read private packages, so a local
  `dotnet new install` of a *private* package falsely succeeds — it cannot reproduce the consumer-side
  103. Honest 103 evidence requires a *foreign* token (the Templates composition CI). The evidence model
  (research R4) correctly anticipated this; worth a reusable "foreign-token probe" helper.

- **Local in-repo template registration shadows the feed package (MEDIUM).** `dotnet new` reports the
  working-tree `.template.config` as the same identity `FS.GG.UI.Template`, taking precedence over the
  installed feed package — muddying any local scaffold probe. Clean-dir / `--force` install discipline is
  needed, reinforcing the Feature-175 "local checks are not authoritative" lesson.

- **Half-landing is structurally easy to mis-report (MEDIUM).** Two independent gates (publish +
  visibility) with a binding conjunction (FR-004) means the producer half can be 100% green while the
  feature is *not done*. The tasks.md coupling note + the combined-gate checkpoint (T019) handled this
  well; the board/registry must resist the temptation to mark Done on the publish alone.

## Generalizable-code candidates

- A **`foreign-token install probe`** helper (mint/borrow a `packages: read`-only token, attempt
  `dotnet new install`, assert exit 0 / classify 103) would make SC-002 reproducible without a full
  Templates CI re-run.
- A **release-tag-set pusher** (`v<V>` + `fs-gg-ui-template/v<V>` + `fs-gg-ui/v<V>` at one ref) is now
  hand-rolled in three features (215/216/218); worth a `scripts/cut-coherent-release.sh <V>`.

## Severity summary

| Item | Severity | Automatable? |
|---|---|---|
| No REST for package visibility | HIGH | ❌ operator-only |
| Owner token masks 103 | MEDIUM | ⚠ needs foreign token |
| Local template registration shadows feed | MEDIUM | ✅ clean-dir discipline |
| Half-landing mis-report risk | MEDIUM | ✅ checkpoint enforced |
