# Contract: Old-repo redirect notice

**Artifact**: `docs/bridge/old-repo-redirect.md` · **Satisfies**: FR-004, FR-011 ·
**SC**: SC-001, SC-006

Copy-ready content for the **archived** `EHotwagner/FS-Skia-UI` repository and its NuGet package
pages, delivered with an honest recorded-action header because R7 cannot apply changes to a repo it
does not own.

## Required structure

| Block | MUST contain | Acceptance condition |
|---|---|---|
| Recorded-action header | Target = archived old-repo README + `FS.Skia.UI.*` package pages; **Status = NOT yet applied**; note that the old repo is archived/read-only (apply needs un-archive by the owner). | The header is present and says "not yet applied" — no overclaim (FR-011 / SC-006). |
| README banner (copy-ready) | A fenced, paste-ready Markdown block: the product has moved to **FS.GG.Rendering**, with the new-home link, one line on what moved, superseding the stale Vulkan / governed-workflow self-description. | From the old repo's entry point the reader reaches the new home in one hop (SC-001). |
| Package-page block (copy-ready) | Paste-ready deprecation/redirect text for `FS.Skia.UI.*` package descriptions pointing to the new home, **without** asserting a rename (identity retained). | No new package ID; no "renamed to" claim. |
| Apply checklist | The exact owner steps: un-archive → paste banner → re-archive; update package descriptions. | An owner can execute it without further guidance. |

## Honesty rules (Constitution VI)

- The notice MUST NOT be described, here or in any other artifact, as already applied.
- The notice MUST supersede (not merely coexist with) the old repo's current self-description.
- Copy-ready blocks SHOULD be fenced so they paste cleanly.

## Prohibited

- Any rename of package identity (R8 only).
- Any automated cross-repo push step presented as part of R7's in-repo work.
