# Quickstart: verify the scaffold skill-tree leak is fixed

Runnable validation recipe. Establishes the **before** (leak reproduces), applies the fix, then proves the **after** across gates and a live scaffold. Details live in [contracts/](./contracts/) and [data-model.md](./data-model.md).

Prerequisites: repo checked out on `228-fix-scaffold-skill-leak`; .NET SDK (`net10.0`); `dotnet new list fs-gg-ui` resolves (template installed from the local feed).

## 0. Before — reproduce the leak (pre-fix template)

Static (env-free): confirm the 9 `.claude/skills/fs-gg-*/` sources are **not** lifecycle-gated:

```sh
python3 - <<'PY'
import json
d=json.load(open('.template.config/template.json'))
leak=[s for s in d['sources']
      if s.get('target','').startswith('.claude/skills/fs-gg-')
      and 'lifecycle == "spec-kit"' not in s.get('condition','')]
print("ungated .claude/skills product sources:", len(leak))  # pre-fix: 9  → after: 0
PY
```

Live (env-gated): scaffold `game` under `sdd` and see the intrusion:

```sh
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report
# pre-fix: sdd/game scaffold has .claude/skills/fs-gg-* present (8) → SDD would flag providerWroteSddTree
```

Record both under `specs/228-fix-scaffold-skill-leak/readiness/leak-before.md`.

## 1. Apply the fix

- `.template.config/template.json`: append `&& lifecycle == "spec-kit"` to the condition of the 9 `.claude/skills/fs-gg-*/` product-skill sources (leave the 9 `.agents/skills/` siblings untouched). Target end-state e.g.: `(profile == "app" || profile == "game") && lifecycle == "spec-kit"`.
- Correct the gates per [contracts/gate-assertion-contract.md](./contracts/gate-assertion-contract.md): `Feature219…fs` G-EMIT surface-specific lifecycle assertion; `Feature204…fs` `gatedSourceAudit` classifier + GV-2 floors (`framework>=9`, `workspace>=15`) + `gated-condition` string.
- `scripts/validate-lifecycle-template.fsx`: update the `gated-condition` line; add the `.claude/skills/` product-skill count (expect 0 under sdd/none).

## 2. After — gates

```sh
# regenerate the readiness artifact both gates read (live scaffold observation)
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report

# run the two corrected gates (Expecto filter by test-list name)
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature204|Feature219"
# expect: Feature204 GV-2/GV-4/GV-5 green; Feature219 G-EMIT green
```

Expected outcomes:
- Static scan (step 0) now prints `0`.
- Feature 204 `gatedSourceAudit`: `framework=9`, `workspace>=15`, `0` violations.
- Feature 219 G-EMIT: `.agents/skills/` product sources non-spec-kit-gated; `.claude/skills/` product sources spec-kit-gated.

## 3. After — live scaffold (the real artifact, FR-008)

For `profile ∈ {app, game}` and `lifecycle ∈ {sdd, none, spec-kit}` (the script covers these):

| Check | sdd | none | spec-kit |
|---|---|---|---|
| `count(.claude/skills/fs-gg-*)` | **0** | **0** | S(profile) |
| `set(.agents/skills/fs-gg-*)` | S(profile) | S(profile) | S(profile) |

And the end-to-end outcome (SDD-orchestrated):

```sh
# in a scratch dir — the primary acceptance for US1 (SC-001/SC-004)
fsgg-sdd scaffold --provider rendering --param productName=Spaceinvaders --profile game
# expect: outcome success, NO scaffold.providerWroteSddTree; new-sdd-fullstack proceeds to governance + doctor
```

Record the after-observations under `specs/228-fix-scaffold-skill-leak/readiness/` (fixed-scaffold-sdd.md, fixed-scaffold-none.md, agents-tree-intact.md, gate-transcripts.md, success-criteria.md).

## Success mapping

| Criterion | Proven by |
|---|---|
| SC-001 / SC-004 | step 3 SDD-orchestrated scaffold outcome=success + full-stack proceeds |
| SC-002 | step 3 table: 0 in `.claude/skills/`, S(profile) in `.agents/skills/` under sdd |
| SC-003 | Feature 204 GV-3 (spec-kit byte-identical) + step 3 (agents intact all lifecycles; sdd≡none) |
| SC-005 | step 0 vs step 2: gate fails pre-fix, passes post-fix, across all profiles |
