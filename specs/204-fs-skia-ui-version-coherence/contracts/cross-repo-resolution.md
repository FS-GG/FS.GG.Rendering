# Contract: Cross-Repo Record Reconciliation (US3 — FR-005, FR-006, FR-007)

Bring the cross-repo "single source of truth" into agreement with the verified reality. **Gated**: do
nothing here until US1 (coherence-verification) **and** US2 (snapshot-manifest) pass.

Protocol authority: `FS-GG/.github` → `docs/coordination/README.md` and the `cross-repo-coordination`
skill. Access verified at plan time: `gh` is authenticated (`EHotwagner`), `FS-GG/.github` resolves,
issue #1 is OPEN.

## Hard ordering (FR-007 — no premature closure)

```
[US1 all four profiles green] ∧ [US2 snapshot reproducible]   ──required──►   registry flip + issue close
```

If either precondition fails, **stop**: leave `coherent: false`, leave #1 OPEN, record the blocker.

## XR-1 — Registry row (`FS-GG/.github`)

Update the `fs-skia-ui-version` row in **both**:
- `registry/dependencies.yml` (authoritative): `coherent: true`, reference the resolving change
  (commit/tag/PR).
- `docs/registry/compatibility.md` (projection): regenerate/edit consistently.

Both MUST change **together** — they must never disagree (edge case "registry and issue disagree").

> Task-time step: clone/read `FS-GG/.github`, confirm the exact YAML key/shape of the row before
> editing (research R5 open item).

## XR-2 — Request issue `FS-GG/FS.GG.Rendering#1`

```sh
gh issue comment 1 --repo FS-GG/FS.GG.Rendering --body "## Response
Resolved via the **git tag + committed lockfile** option. Pinned FsSkiaUiVersion=<version>;
immutable snapshot tag fs-skia-ui/v<version>; all four template profiles (app, headless-scene,
governed, sample-pack) generate→restore→build→evidence green; phantom FS.GG.UI.Color /
FS.GG.UI.SkillSupport pins removed. Registry fs-skia-ui-version flipped coherent: true.
Evidence: <links/refs>."
gh issue close 1 --repo FS-GG/FS.GG.Rendering
```

## Pass conditions

| ID | Condition | Maps to |
|----|-----------|---------|
| XR-A | `registry/dependencies.yml` row reads `coherent: true` and references the resolving change. | FR-005, SC-004 |
| XR-B | `docs/registry/compatibility.md` projection agrees with XR-A. | FR-005, edge case |
| XR-C | Issue #1 carries a comment beginning `## Response` naming the option + linking evidence, and is CLOSED. | FR-006, SC-004 |
| XR-D | The flip/close happened **after** US1+US2 passed (never premature). | FR-007, SC-004 |
| XR-E | No FS-GG repo is left with a stale "blocked / coherent: false" signal for this contract. | SC-005 |

## Confirmation

The requester (FS.GG.Templates side) can confirm the resolution on #1 — the requester confirms,
per the protocol's resolve step.
