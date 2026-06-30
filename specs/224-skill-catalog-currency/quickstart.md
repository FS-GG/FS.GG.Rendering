# Quickstart: Validate Consumer Skill Catalog Currency

Runnable validation that proves the feature end-to-end. Implementation bodies live in `tasks.md` /
the implementation phase; this is the run/validate guide. See
[contracts/catalog-currency-check.md](./contracts/catalog-currency-check.md) and
[data-model.md](./data-model.md) for the rules referenced below.

## Prerequisites

- .NET `net10.0` SDK; repo restores (see the FSharp.Core lockfile note if restore is blocked).
- The `Rendering.Harness` tool builds (`tools/Rendering.Harness`).
- Local pack feed for the template scaffold step: `~/.local/share/nuget-local/`.

## 1. Live ground-truth run (do this FIRST — standing assumption)

Capture the **actual** produced skill surface before authoring any catalog row.

```sh
# Scaffold a spec-kit product and a non-spec-kit product from the local template, then list skills.
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx   # exercises real scaffolds
# For each scaffold dir, enumerate what actually shipped:
#   ls <scaffold>/.agents/skills  <scaffold>/.claude/skills
#   test -f <scaffold>/docs/skillist-reference.md   # present under spec-kit, absent under sdd/none
```

**Expected**: the emitted `.agents/skills/` / `.claude/skills/` ids match the profile-wired product
skills (+ `speckit-*` under spec-kit); **none** of the 8 defunct `fs-gg-*` ids nor any
`fsdocs-*`/`fsharp-*` id appears. This confirms (or corrects) the research R1 shipping set that the
catalog must list.

## 2. Currency check fails on the current (broken) docs

Before any content fix, the new check must red against today's docs.

```sh
dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency
```

**Expected (pre-fix)**: FAIL, with findings naming `fs-gg-controls-host`, `fs-gg-typed-controls`,
`fs-gg-viewer-host`, … and their doc/line — proving the gate detects the real drift (SC-003 negative
direction, observed on the live defect).

## 3. Correct the docs, then the check passes

Apply the content fix (catalog rows → research R1 set with honest header; `scaffold-map.md` prose
refs repointed to shipping skills), then:

```sh
dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency
```

**Expected (post-fix)**: PASS. Confirms SC-001 (100% of catalog ids resolve), SC-002 (no dangling
scaffold-map ref), SC-005 (defunct id set appears zero times).

## 4. Regression: a freshly-introduced dangling reference is caught

```sh
# Temporarily add a bogus reference, e.g. `fs-gg-does-not-exist`, to skillist-reference.md
dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency   # MUST fail naming it
git checkout -- template/base/docs/skillist-reference.md                  # revert
dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency   # MUST pass again
```

**Expected**: FAIL→PASS across the inject/revert — proves drift-proofing (SC-003).

## 5. Refresh path passes first time (FR-007)

```sh
# Option A (hand-maintained-under-check): edit catalog to satisfy the check — `dotnet test` green.
# Option B (if a generator was chosen): regenerate, then the check asserts committed == regenerated.
dotnet fsi scripts/refresh-skill-catalog.fsx   # only if Option B was selected in tasks
dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency   # PASS, no hand-edits after refresh
```

**Expected**: the documented refresh produces a file that passes the check on the first run (SC-004).

## 6. Gating intact (no regression of Feature 219/204)

```sh
dotnet test tests/Package.Tests --filter Feature219
dotnet test tests/Package.Tests --filter Feature204
```

**Expected**: still green — the catalog remains spec-kit-gated (present under spec-kit, absent under
sdd/none); only its **content** changed, not its emission gating.

## Done / acceptance mapping

| Step | Spec criterion |
|---|---|
| 1 | Produced-surface ground truth (standing assumption; feeds FR-001/R1) |
| 2 | Check detects real drift (SC-003 negative) |
| 3 | Catalog + scaffold-map resolvable (SC-001, SC-002, SC-005) |
| 4 | Drift-proofing (SC-003) |
| 5 | Refresh passes first run (SC-004 / FR-007) |
| 6 | Emission gating preserved (FR: lifecycle gating intact) |
