# T006 — Pre-fix gate state (assertions that encode the leak)

`dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature204|Feature219"` on the
**pre-fix** template:

```
Passed!  - Failed: 0, Passed: 14, Skipped: 0, Total: 14 - Package.Tests.dll (net10.0)
```

The gates are **green on the leaky shape** — they will go RED after T007 unless corrected (T013–T015).
The assertions that currently encode the leak:

## Feature 219 (`Feature219EmitFrameworkSkillsTests.fs`)

- **L143** `sources.Length >= 18` — 9 ids × 2 surfaces (kept; structural pairing).
- **L145 (LEAKY)** blanket over *every* product-skill source: `Expect.isFalse (s.Condition.Contains SPEC_KIT_COND)` — asserts **no** product source may be spec-kit-gated, including the `.claude/skills/` mirror. After the fix the 9 `.claude/skills/` sources carry the spec-kit clause, so this fails. Must become **surface-specific** (G-219.2).

## Feature 204 (`Feature204LifecycleTemplateTests.fs`)

- **L118 (LEAKY classifier)** `isFrameworkSkill = source.StartsWith "template/product-skills/"` — classifies **both** `.agents/skills/` and `.claude/skills/` product sources as **framework**, then **L127** flags any framework source carrying the spec-kit clause as a violation. After the fix the `.claude/skills/` sources are spec-kit-gated → violation. Classifier must route `.claude/skills/` product sources to **lifecycle-workspace** (G-204.1).
- **L162 (LEAKY floor)** `framework >= 18` — counts all 18 product sources as framework. After the fix framework = 9. Must become `>= 9`; workspace floor `>= 6` → `>= 15` (G-204.2).
- **L166-169 (report string)** `gated-condition:` expected wording must reflect the corrected rule (G-204.3).

## Live report generator (`scripts/validate-lifecycle-template.fsx`)

- verdict-core `verifyGatedSources` (L149-157) has the **same** leaky classifier (framework by `source` prefix, asserts no spec-kit clause) — it will `failwith` on the fixed template and block `--emit-report`. Must be corrected in lockstep (T015).
