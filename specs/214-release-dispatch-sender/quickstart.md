# Quickstart / Validation: Release → Templates Dispatch Sender

Runnable validation that the sender works. Two layers run locally now (no credential); the third is
the live cross-repo check, deferred until the org credential lands (FS-GG/.github#21/#22).

See [contracts/template-released-dispatch.md](./contracts/template-released-dispatch.md) and
[data-model.md](./data-model.md) for the payload shape and derivation rules.

## Prerequisites

- `actionlint` (workflow linter) — `go install github.com/rhysd/actionlint/cmd/actionlint@v1.7.7`
  (pin a fixed version, not `@latest`, so lint evidence is reproducible) or the project's pinned tooling.
- `gh` CLI (for the live check only; preinstalled on the runner).
- The new artifacts present: `.github/workflows/template-dispatch.yml`,
  `scripts/template-released-dispatch.sh`.

## Layer 1 — Workflow lints (structure, trigger, guards)

```sh
actionlint .github/workflows/template-dispatch.yml
```

**Expected**: exit 0. Confirms the `on: push: tags: ['fs-gg-ui-template/v*']` trigger, the
`if: github.repository == 'FS-GG/FS.GG.Rendering'` guard, and the step expressions parse.

## Layer 2 — Dispatch script dry-run (derivation, payload, fail-loud)

The script never hits the network when `DRY_RUN=1`; it prints the exact payload it *would* send.

```sh
# Happy path — derives version and prints the payload, no send
DRY_RUN=1 GH_TOKEN=dummy \
  GITHUB_REF=refs/tags/fs-gg-ui-template/v0.1.50-preview.1 \
  scripts/template-released-dispatch.sh
# Expected stdout includes: event_type=fs-gg-ui-template-released
#                           client_payload.version=0.1.50-preview.1
# Expected exit: 0

# Edge: version cannot be determined → fail, no send
DRY_RUN=1 GH_TOKEN=dummy GITHUB_REF=refs/tags/fs-gg-ui-template/v \
  scripts/template-released-dispatch.sh ; echo "exit=$?"
# Expected: non-zero exit, message naming the undeterminable version, NO payload printed

# Edge: malformed version → fail, no send
DRY_RUN=1 GH_TOKEN=dummy GITHUB_REF=refs/tags/fs-gg-ui-template/vNOPE \
  scripts/template-released-dispatch.sh ; echo "exit=$?"
# Expected: non-zero exit

# Edge: missing credential → fail visibly (even in dry-run, the guard fires)
DRY_RUN=1 GH_TOKEN= GITHUB_REF=refs/tags/fs-gg-ui-template/v0.1.50-preview.1 \
  scripts/template-released-dispatch.sh ; echo "exit=$?"
# Expected: non-zero exit, message naming the missing TEMPLATES_DISPATCH_TOKEN/GH_TOKEN
```

Maps to: FR-002/FR-003 (derivation & shape), FR-006 + edge cases (fail-loud), FR-004 (credential).

## Layer 3 — Live cross-repo send (DEFERRED — blocked by org credential)

> **Blocked**: requires `secrets.TEMPLATES_DISPATCH_TOKEN` backed by the org cross-repo credential
> (FS-GG/.github#21/#22). Run this once the credential is provisioned. Do not fake it green.

1. Confirm `TEMPLATES_DISPATCH_TOKEN` is configured on `FS-GG/FS.GG.Rendering`.
2. Push (or re-run via `workflow_dispatch`) a template tag, e.g.
   `git tag fs-gg-ui-template/v0.1.50-preview.1 && git push origin fs-gg-ui-template/v0.1.50-preview.1`.
3. **Expected**:
   - The `template-dispatch` run shows the send step succeeding, echoing repo/event/version (US2 SC-004).
   - In `FS-GG/FS.GG.Templates`, `upstream-bump.yml` fires and a pin-bump PR for that exact version
     appears with **no** manual `workflow_dispatch` (US1 SC-001/SC-005, SC-002 no drift).

### Fork / non-template negative checks (US3, SC-003)

- From a fork, the same tag push runs nothing (the `if: github.repository == …` guard skips the job).
- A non-template tag (e.g. `v*` release) does not match the trigger → no template-released send.

## Regression guard (FR-008)

Confirm `release.yml` is byte-unchanged by this feature (Package.Tests, template-instantiation tests,
and the Feature 212 stock-root build/test/run assertion still gate the release):

```sh
git diff --stat origin/main -- .github/workflows/release.yml   # expect: no output
```
