# Contract Pin — Feature 214 sender non-negotiables (T004 / T005)

Pinned from the receiver source of truth
[`contracts/template-released-dispatch.md`](../contracts/template-released-dispatch.md) and
[`data-model.md`](../data-model.md). The sender MUST hit these exactly.

## Non-negotiables the sender must realize (T004)

| Item | Value | Source |
|------|-------|--------|
| `event_type` | `fs-gg-ui-template-released` (literal, exact) | FR-001 |
| `client_payload.version` form | `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$` | FR-002 |
| Target repo | `FS-GG/FS.GG.Templates` | data-model |
| Transport | `POST /repos/FS-GG/FS.GG.Templates/dispatches` (via `gh api`) | contract |
| Tag-prefix to strip | `refs/tags/fs-gg-ui-template/v` | FR-002/FR-003 |
| Credential | `secrets.TEMPLATES_DISPATCH_TOKEN` → `GH_TOKEN` | FR-004 |
| Canonical-repo guard | `github.repository == 'FS-GG/FS.GG.Rendering'` | FR-005 |

## Trigger signal exists & is distinct from `release.yml` (T005 — FR-007/FR-008 basis)

- `git tag -l 'fs-gg-ui-template/*'` →

  ```
  fs-gg-ui-template/v0.1.50-preview.1
  ```

  The template-scoped trigger tag exists (Feature 206 cadence).

- `release.yml`'s full trigger set is `release: types:[published]` + `push: tags:['v*']` +
  `workflow_dispatch`. **None of these produce an `fs-gg-ui-template/v*` push:**
  - GitHub Actions tag filter patterns treat `*` as **not** crossing `/`, and more simply the tag
    `fs-gg-ui-template/v…` does not even *start* with `v` — so `tags: ['v*']` cannot match it.
  - Neither `release: published` nor `workflow_dispatch` creates that tag.

  Therefore the new `template-dispatch.yml` trigger (`tags: ['fs-gg-ui-template/v*']`) is **disjoint**
  from every `release.yml` trigger. A template release fires the sender and **not** the release-only
  packaging job, and vice-versa — `release.yml` need not (and does not) change (FR-008), and the
  trigger pattern itself is the FR-007 "only genuine template releases" guard.

## `v*` vs `fs-gg-ui-template/v*` distinction (T015 note)

The sender's trigger MUST be exactly `fs-gg-ui-template/v*`, never `v*`. `release.yml` owns `v*`
(product/library releases); this feature owns the template-coherent-set tag namespace only. Mixing
them would either (a) fire the sender on ordinary `v*` releases (false template-released sends,
FR-007 violation) or (b) fire release packaging on template tags. Keeping the two patterns disjoint
is what makes the two workflows non-interfering.
