# Contract: Cross-Repo Resolution (registry flip + request response + board transition)

Executed **last**, only after PV-1..PV-6 are green and the tag is pushed (US1+US2 complete). All
edits to shared cross-repo state go through the coordination protocol — never by editing another
repo's files directly. Mirrors feature 204's US3 (`gh` authenticated as `EHotwagner`;
`FS-GG/.github` resolvable).

## XR-A — Registry row (`FS-GG/.github`)

Update the `fs-gg-ui-template` row in `registry/dependencies.yml` to record the coherent release:

- recorded version → `0.1.50-preview.1`
- coherent state → recorded as a coherent release
- tag reference → `fs-gg-ui-template/v0.1.50-preview.1`
- `resolved_by` → publishing commit on `206-publish-template-coherent-set` + the tag
- `tracking` → the rendering tracking issue / this feature

Commit prefix `registry:` (as 204 used for the `.github` commit, not the `206:` in-repo prefix).

## XR-B — Compatibility projection (`FS-GG/.github`)

Update the `fs-gg-ui-template` row in `docs/registry/compatibility.md` so the projection states the
**same** version/tag/coherent state as XR-A (invariant: authoritative row and projection agree —
FR-007).

## XR-C — Dependent request response (`FS-GG/FS.GG.SDD#1`)

Post a `## Response` on `FS-GG/FS.GG.SDD#1` citing:

- the published package `FS.GG.UI.Template 0.1.50-preview.1` (installable, side-effect-free by
  default, `lifecycle` + `initGit` surface);
- the coherent-set tag `fs-gg-ui-template/v0.1.50-preview.1`;
- a pointer to the published §5 generation contract (205) the scaffold path fulfils.

```sh
gh issue comment 1 --repo FS-GG/FS.GG.SDD --body "## Response
The side-effect-free + lifecycle template surface is now installable: FS.GG.UI.Template
0.1.50-preview.1 on the feed, snapshotted by the coherent-set tag fs-gg-ui-template/v0.1.50-preview.1
(over FS.GG.UI.* 0.1.50-preview.1 / fs-skia-ui/v0.1.50-preview.1). Generation is side-effect-free by
default (initGit opt-in, no skipGitInit, no auto post-actions); lifecycle=spec-kit is byte-identical
to the prior baseline. SDD can now own repo-init + chmod on the scaffold path per
specs/205-scaffold-git-init-chmod/contracts/fs-gg-ui-template-generation.md §5 (S1–S3)."
```

The request is **responded** (and closed if SDD confirms / via linked PR); leave open with the
response if SDD's side is still pending — the rendering precondition is satisfied either way (FR-008).

## XR-D — Board transition (Coordination Projects v2)

Move the P1 Rendering item "Publish FS.GG.UI.Template carrying the new parameter; tag the coherent
set" to **Done**, and clear the "blocked by lifecycle symbol" relationship (FR-011) — only once
XR-A..XR-C and PV-1..PV-6 hold.

## Partial-failure rule (FR-010)

If publish, tag, or any of XR-A..XR-D cannot complete, the cross-repo record MUST show the release as
**not yet coherent / in-progress** — never falsely coherent. Record the in-progress state and the
blocker in `readiness/cross-repo-resolution.md`.
