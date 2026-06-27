# Contract: Snapshot Tag Namespace (`fs-skia-ui/v<V>` → `fs-gg-ui/v<V>`)

Covers User Story 2 (P2) and FR-004/005. A reproducibility/audit lookup must find each published
coherent snapshot under the new namespace, at the same commit, with the legacy namespace empty.

## Obligations

| Action | Detail |
|--------|--------|
| Re-tag | Create annotated `fs-gg-ui/v0.1.50-preview.1` at commit `57be86c` and `fs-gg-ui/v0.1.51-preview.1` at commit `d9f4c81` — the exact commits the legacy tags point at. Preserve the snapshot subject. |
| Delete legacy | Remove `fs-skia-ui/v0.1.50-preview.1` and `fs-skia-ui/v0.1.51-preview.1` (clean break, no alias). |
| Publish | Push the two new tags and push the two tag deletions to the remote. |

## Invariants

- **Same commit (FR-004)**: `git rev-list -n1 fs-gg-ui/v<V>` equals the pre-rename
  `git rev-list -n1 fs-skia-ui/v<V>` for each version.
- **Legacy empty (FR-005)**: `git tag -l 'fs-skia-ui/v*'` returns nothing, locally and on the remote.
- **Untouched neighbor**: the `fs-gg-ui-template/v0.1.50-preview.1` tag (a different namespace) is not
  modified.

## Acceptance (US2)

1. A reproducibility lookup querying `fs-gg-ui/v<V>` finds the snapshot for each previously published
   coherent version and resolves to the same commit it did before. *(AS1 / SC-003)*
2. Querying the legacy `fs-skia-ui/v*` namespace returns zero tags. *(AS2 / SC-003)*

## Verification commands

```bash
git tag -l 'fs-gg-ui/v*'                       # → the two versions
git tag -l 'fs-skia-ui/v*'                     # → (empty)
git rev-list -n1 fs-gg-ui/v0.1.50-preview.1    # → 57be86c…
git rev-list -n1 fs-gg-ui/v0.1.51-preview.1    # → d9f4c81…
git ls-remote --tags origin 'fs-skia-ui/v*'    # → (empty) after push
```
