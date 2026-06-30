# Quickstart: De-leak Product Skill Vocabulary

Runnable validation that proves the feature end-to-end: the produced surface is what the guard
scans, the guard reds on each leak class and greens on the corrected set, the de-leaked prose keeps
its lessons, and the wrapper-vs-canonical parity stays green.

**Prerequisites**

- Repo restores/builds normally (no package-ref changes here; no lockfile work — see CONTRIBUTING).
- From repo root: `/home/developer/projects/FS.GG.Rendering`.
- GL not required (these are content/test gates).

---

## 1. Produced-surface run FIRST (standing assumption, research R1)

Confirm the guard's scan surface equals the 7 shipped product skills, and that a non-spec-kit
scaffold genuinely lacks `specs/<feature>/feedback/` (so the Class-B leak really dead-ends).

```sh
# Enumerate the produced product-skill set the way the guard will (SkillParity discovery).
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature225ProductSkillVocabulary

# Real-scaffold evidence path (produced surface across lifecycles):
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report
```

**Expected**: discovery returns exactly the 7 ids (`fs-gg-elmish`, `fs-gg-keyboard-input`,
`fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-symbology`, `fs-gg-testing`, `fs-gg-ui-widgets`); a
non-spec-kit scaffold vendors all 7 and has **no** `specs/<feature>/feedback/` folder. Recorded in
`readiness/produced-surface.md`.

## 2. Guard reds on the leaks present today (failing-before)

Before any edit, the guard must flag the existing leaks (Class A in testing/ui-widgets/skiaviewer,
Class B in all 7, Class C in symbology).

```sh
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature225ProductSkillVocabulary
```

**Expected**: FAIL — findings name each offending skill, leak class, matched token, and `file:line`
(e.g. `fs-gg-testing … 'refresh-local-feed-and-samples' … :56`). Maps to SC-001/002/003, FR-007.

## 3. Reframe the skill bodies → guard greens (passing-after)

Edit the 7 bodies: Class-A evidence blocks rewritten to product-local "what evidence to record and
where" with no framework path; Class-B feedback line made spec-kit-conditional; Class-C stamps
removed/reworded; headings retitled (`## Evidence Rules`). Front-matter untouched.

```sh
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature225ProductSkillVocabulary
```

**Expected**: PASS — zero findings over the corrected shipped set. Maps to SC-001/002/003/005.

## 4. Lesson-preservation spot check (reframe, not removal)

```sh
# Each de-leaked block still carries its lesson/capability — confirm by eye against research R0.
git diff template/product-skills/
```

**Expected**: every removed token is replaced by product-language guidance carrying the same intent
(record visual/readback/control evidence; verify before claiming done; symbology rich-text /
auto-label / label-bound-motion capabilities); no guidance deleted. Maps to SC-004, FR-004.

## 5. Regression: inject a leak → guard reds; revert → greens

```sh
# The guard's own negative/positive test exercises all three classes in-memory.
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature225ProductSkillVocabulary
```

**Expected**: the injected-synthetic-body test yields exactly three findings (one per class) and the
real-set test yields none — demonstrated failing-before / passing-after. Maps to SC-005, FR-007.

## 6. Wrapper-vs-canonical parity stays green

```sh
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
```

**Expected**: PASS — body-only edits leave `name:`/`description:` front-matter intact, so no
`MetadataDrift` / `BrokenTarget` / coverage failures. Maps to SC-006, FR-006.

---

## Done / acceptance mapping

| Step | Spec criterion |
|---|---|
| 1 produced-surface run | Standing assumption (R1); US2 premise; FR-007 scan surface |
| 2 reds-before | SC-001, SC-002, SC-003; FR-007 |
| 3 greens-after | SC-001, SC-002, SC-003, SC-005; FR-001, FR-002, FR-003 |
| 4 lesson check | SC-004; FR-004 |
| 5 inject/revert regression | SC-005; FR-007 |
| 6 parity green | SC-006; FR-006 |
| (delivery) | FR-008 republish + FS-GG/FS.GG.Templates#8 pin; FR-009 update #37 + epic #34 |
