# Quickstart: Validate the Epic Closure

Runnable validation that proves the published FS.GG.UI template "emits Spec Kit only when asked" and that the
closure record + guidance + coordination are in place. See [contracts/](./contracts/) and
[data-model.md](./data-model.md) for the schemas referenced below.

## Prerequisites

- .NET SDK (`net10.0`), `dotnet new` template engine.
- The published package present in the local feed:
  `~/.local/share/nuget-local/FS.GG.UI.Template.0.1.51-preview.1.nupkg`.
- `git` and `gh` (for the cross-repo / board checks in scenario 4).

## Scenario 1 — Acceptance against the PUBLISHED package (US1 / FR-001..006)

```bash
# Install the pinned published package (NOT the working-tree source template)
dotnet new install FS.GG.UI.Template::0.1.51-preview.1 --add-source ~/.local/share/nuget-local/
dotnet new list | grep fs-gg-ui    # confirm it resolves to the package version

# Run the live acceptance matrix (3 lifecycle × 4 profiles) and write the record
FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx

# Restore the dev environment
dotnet new uninstall FS.GG.UI.Template
```

**Expected**: exit `0`; `specs/210-lifecycle-template-closure/readiness/epic-acceptance.md` written with
`provenance: live`, `validated_package: FS.GG.UI.Template 0.1.51-preview.1`, all four profiles showing gated
set **present** under `spec-kit` (and `diff-vs-baseline=none`), **absent** under `sdd`/`none`, `none` carrying
no orchestrator marker, `buildability: pass` for the `sdd`/`none` `app`-profile build spot-check (or a
disclosed `environment-limited`), and a single Rendering-side **CLOSE** conclusion. (Without the env flag the script
self-checks the verdict core only; `--emit-report` writes a `provenance: verdict-core` fallback.)

## Scenario 2 — Byte-identical default (US1 / FR-005, SC-003)

Covered inside the live run above: the harness asserts `spec-kit` (and the no-flag default) is identical in
**presence and content** to the pre-lifecycle baseline for every profile. **Expected**: zero file or content
differences across `app`, `headless-scene`, `governed`, `sample-pack`.

## Scenario 3 — Consumer guidance & migration note (US2 / FR-007..009, SC-004)

```bash
grep -n "lifecycle" .template.package/README.md
```

**Expected**: the README contains a decision tree (governed→`spec-kit`, SDD-composed→`sdd`, standalone→
`none`), a per-value include/exclude table, the explicit standalone-`none` "no governance/no orchestrator
attached or expected" statement, and a migration note stating the default reproduces prior output. A
first-time reader can pick the right value for all three scenarios from this alone.

## Scenario 4 — Cross-repo remainder & board closure (US3 / FR-010..011, SC-005/006)

```bash
gh issue view FS-GG/FS.GG.SDD#1                       # scaffold-path request still open → REUSE
gh issue list --repo FS-GG/FS.GG.SDD --search "constitution ownership lifecycle"   # decision item exists?
```

**Expected**: `FS-GG/FS.GG.SDD#1` is referenced (not duplicated) from the closure record; the
constitution-ownership decision for `lifecycle=sdd` is tracked as exactly one item; the `FS-GG` Coordination
board shows the P1 epic **Rendering-side complete** with each remainder item attributed to its owning repo and
`epic_fully_done = false` while any remainder is open.

## Done when

- [ ] `epic-acceptance.md` exists with `provenance: live` and a CLOSE conclusion (Scenarios 1–2).
- [ ] `.template.package/README.md` carries the full lifecycle guidance + migration note (Scenario 3).
- [ ] Each remainder item is tracked exactly once and referenced from the record; board updated (Scenario 4).
