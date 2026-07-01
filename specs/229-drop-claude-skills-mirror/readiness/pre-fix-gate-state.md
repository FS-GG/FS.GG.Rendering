# Pre-fix gate state (T006) — Feature 229

The post-Feature-228 Feature 204/219 gates that encode the spec-kit-gated `.claude/skills/` mirror,
which the T007 template edit will turn red (and the T013–T015 corrections turn green again).

## Result on the current (post-228) template

```
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature204|Feature219"
Passed!  - Failed: 0, Passed: 14, Skipped: 0, Total: 14
```

Green — confirming the gates currently assert the Feature 228 shape.

## Assertions that will break when the 9 `.claude/skills/` sources are deleted

**Feature219EmitFrameworkSkillsTests.fs** (`G-EMIT surface test`, ~L144-164):
- `Expect.isTrue (sources.Length >= 18)` — after deletion there are 9 product-skill sources → **red**.
- `.claude/skills/`-targeted sources asserted spec-kit-gated (L153-155) — no such source remains → vacuously ok, but…
- "each id emits under BOTH `.agents/skills/` and `.claude/skills/`" (L156-163) — the `.claude/skills/` existence check → **red**.

**Feature204LifecycleTemplateTests.fs** (GV-2, ~L163-177):
- `Expect.isTrue (workspace >= 15)` — deleting the 9 `.claude/skills/` product mirrors drops workspace to ~6 → **red**.
- Expected `gated-condition:` report string names "`.claude/skills/` product mirror" — the regenerated report (T016) will no longer say that → **red** on the `stringContains`.

**validate-lifecycle-template.fsx** (`assertGating`, ~L175):
- `workspaceChecked >= 15` — same drop → the fsx verdict-core throws, no report written → both gates fail to self-provision → **red**.

These are exactly the assertions the T013–T015 corrections update to the ADR-0011 invariant
(no product-skill source targets `.claude/skills/`; workspace floor `>= 6`).
