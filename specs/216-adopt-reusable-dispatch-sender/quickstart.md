# Quickstart / Validation: Adopt Reusable App-Token Dispatch-Sender

Runnable validation that the migrated sender is correct. Layers 1–2 + the regression guard run
locally now (no credential needed); Layer 3 is the live cross-repo check, **disclosed-deferred**
until the org App secrets are confirmed and a real release fires (FR-008/FR-009 — never faked green).

See [contracts/template-released-dispatch.md](./contracts/template-released-dispatch.md),
[data-model.md](./data-model.md), and [research.md](./research.md) for the interface, payload, and
the resolved unknowns.

## Prerequisites

- `actionlint` pinned (`go install github.com/rhysd/actionlint/cmd/actionlint@v1.7.7`) — reproducible
  lint evidence (already installed at `~/.local/bin/actionlint`).
- The migrated artifacts present: `.github/workflows/template-dispatch.yml` (two jobs),
  `scripts/derive-template-version.sh`, `scripts/test-derive-template-version.sh`.
- `gh` CLI (Layer 3 only; preinstalled on the runner).

## Layer 1 — Workflow lints (trigger, guard, reusable-call wiring)

```sh
actionlint .github/workflows/template-dispatch.yml
```

**Expected**: exit 0. Confirms `on: push: tags: ['fs-gg-ui-template/v*']`, the
`if: github.repository == 'FS-GG/FS.GG.Rendering'` guard on `derive`, the `dispatch` job's
`uses: …/dispatch-sender.yml@<sha>` reference with `with:`/`secrets:` blocks, and that
`needs: derive` / `${{ needs.derive.outputs.version }}` parse.

## Layer 2 — Version-derivation harness (derive, validate, fail-loud)

```sh
scripts/test-derive-template-version.sh
```

Drives `scripts/derive-template-version.sh` through the boundary cases (no network, no credential):

```sh
# Happy path — derives and emits the version
GITHUB_REF=refs/tags/fs-gg-ui-template/v0.1.52-preview.1 scripts/derive-template-version.sh
# Expected stdout: version=0.1.52-preview.1   (and the same written to $GITHUB_OUTPUT in CI)
# Expected exit:   0

# Edge: non-tag ref (e.g. a manual workflow_dispatch on a branch) → fail, no output
GITHUB_REF=refs/heads/main scripts/derive-template-version.sh ; echo "exit=$?"
# Expected: non-zero, message naming the non-template ref, NO version emitted

# Edge: empty version after strip → fail
GITHUB_REF=refs/tags/fs-gg-ui-template/v scripts/derive-template-version.sh ; echo "exit=$?"
# Expected: non-zero

# Edge: malformed version → fail
GITHUB_REF=refs/tags/fs-gg-ui-template/vNOPE scripts/derive-template-version.sh ; echo "exit=$?"
# Expected: non-zero
```

Maps to FR-005 (fail-loud derivation) and the non-tag / malformed edge cases. The Feature 214
credential-missing and `DRY_RUN` payload cases are intentionally gone — the credential + POST now
live in the reusable workflow (research R3).

## Layer 3 — Live cross-repo send (DEFERRED — gated on org App secrets)

> **Blocked** on confirming the exact `app-id` / `app-private-key` org secret names (research R1) and
> that the `.github#21` App is installed + authenticates. Run once confirmed; do not fake it green.

1. Confirm the two org App secrets are set on/visible to `FS-GG/FS.GG.Rendering` and their names are
   wired into `template-dispatch.yml`'s `secrets:` block.
2. Push (or `workflow_dispatch`-replay onto) a real template tag, e.g.
   `git tag fs-gg-ui-template/v0.1.52-preview.2 && git push origin fs-gg-ui-template/v0.1.52-preview.2`.
3. **Expected**:
   - `derive` succeeds and outputs the version; `dispatch` calls the reusable workflow, which mints a
     scoped token and POSTs the `repository_dispatch` (SC-001/SC-002).
   - In `FS-GG/FS.GG.Templates`, `upstream-bump.yml` fires and a pin-bump PR for that exact version
     appears with **no** manual `workflow_dispatch` (US1, SC-005).
4. Capture **live evidence**: sender run URL + receiver PR URL → close `Rendering#10`, move the board
   item Blocked → Done (FR-009, SC-005).

### Negative checks (US3, SC-003)

- From a fork, the same tag push runs nothing (the `derive` guard skips; `dispatch` `needs: derive`
  so it never starts) — no send, no credential exposure.
- A non-template tag (a `v*` release) does not match `fs-gg-ui-template/v*` → no template-released send.

## Regression guard (FR-006 / SC-004)

`release.yml` MUST be **byte-identical** to `origin/main`:

```sh
git fetch origin main
git diff --exit-code origin/main -- .github/workflows/release.yml && echo "release.yml unchanged ✓"
```

**Expected**: exit 0, no diff. Confirms the migration touched only `template-dispatch.yml` + the
`scripts/` helper, and the `v*` release gating is untouched.

## Cross-repo dependency (FR-008)

If the org App secrets are not yet confirmed/working, file/track the dependency as a
`cross-repo`/`cross-repo:request` issue against `.github#22`/`.github#21` and on the Coordination
board (`Contract: fs-gg-ui-template`), keep `Rendering#10` **Blocked**, and leave this adoption ready
to go green on the first authenticated release. Use the `cross-repo-coordination` skill.
