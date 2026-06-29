# T020 / T021 / T022 — Registry landing (FR-008, ADR-0001) — DEFERRED until both gates hold

**Status (2026-06-29):** ⏸ **Prepared, not yet landed.** The registry PR records the contract as
*resolved* — version/coordinates **and** consumer-readability. The publish coordinates are now true
(`V` on the feed), but **org-readability is not** (`FS.GG.UI.Template` is still `private`, pending the
operator visibility flip — see `visibility-internal.md`). Per FR-004 "no half-landing" and the task
dependency (T020/T021 depend on US1/T019, the combined gate), landing a registry edit that *records
org-readability* while the package is still private would be **dishonest**. So the PR is held until
`gh api … --jq .visibility == internal`, then applied as below.

Target repo: **`FS-GG/.github`** (cross-repo; not in this session's workspace — lands as a PR).

## `registry/dependencies.yml` — `fs-gg-ui-template` entry

| Field | From | To |
|---|---|---|
| `version` | `0.1.52-preview.1` | `0.1.53-preview.1` |
| `package-version` | `0.1.52-preview.1` | `0.1.53-preview.1` |
| `package-tag` | `fs-gg-ui-template/v0.1.52-preview.1` | `fs-gg-ui-template/v0.1.53-preview.1` |
| `productName` feed-note | "UNRELEASED on the feed (lands next release)" | **"released in 0.1.53-preview.1"** |

## Coherence block — `- id: fs-gg-ui-template` (`coherent: true`)

- `resolved_by` → `fs-gg-ui-template/v0.1.53-preview.1`
- Record org-readability (`visibility: internal`) so the Templates-CI consumer half is no longer
  auth-blocked. **← apply only after the visibility flip is verified.**
- No coherence flag flips `true → false`; this *advances* the entry (INV-12).
- No contract **surface** field changes (FR-009, INV-13) — `productName` was specified in Feature 217.

## `docs/registry/compatibility.md`

Regenerate the projection to match the `dependencies.yml` delta above.

## Apply command sketch (after visibility verified)

```bash
# in a clone of FS-GG/.github, on a contract-change branch:
#   edit registry/dependencies.yml + docs/registry/compatibility.md per the table above
gh pr create --repo FS-GG/.github \
  --title "registry: fs-gg-ui-template -> 0.1.53-preview.1 (productName released; org-readable)" \
  --body "Resolves the fs-gg-ui-template contract-change for FS.GG.Rendering#29 + #26.
Published coordinates V=0.1.53-preview.1; productName released; visibility internal. ADR-0001."
```
