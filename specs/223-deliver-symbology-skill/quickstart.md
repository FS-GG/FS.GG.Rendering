# Quickstart: Validate symbology delivery

Runnable validation scenarios that prove the feature end-to-end. Each maps to a Success Criterion.
See [contracts/](./contracts/) and [data-model.md](./data-model.md) for the underlying records.

## Prerequisites

- Repo restored/built. If the FSharp.Core lockfile blocks restore, apply the documented
  NU1403 workaround first.
- From repo root: `cd /home/developer/projects/FS.GG.Rendering`.

## Scenario 1 — Foundational live run (verify GV-3 neutrality + emission) — **run first**

Confirms the read-derived R1 finding on the real template, before hardening any test.

```bash
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx
```

**Expected after wiring**: for every profile, `spec-kit/<profile>: generate=pass diff-vs-today=none`
(GV-3 still green), `sdd/<profile>: framework-skills-present=ok`, `none/<profile>: …present=ok`, and
the status line `symbology: vendored`. (If GV-3 is *not* `none`, R1 is wrong — stop and reassess
before proceeding.)

## Scenario 2 — Symbology ships in the generated product (SC-001 / US1)

```bash
# scaffold the P1 profile (repeat for --profile app) into a temp dir
dotnet new fs-gg-ui --profile game -o /tmp/qs-game
ls /tmp/qs-game/.claude/skills/fs-gg-symbology/SKILL.md /tmp/qs-game/.agents/skills/fs-gg-symbology/SKILL.md
wc -c /tmp/qs-game/.claude/skills/fs-gg-symbology/SKILL.md   # ~12788, the product skill (not the 506B stub)
```

Also confirm under a non-spec-kit lifecycle (content follows the product, not the lifecycle):

```bash
dotnet new fs-gg-ui --profile game --lifecycle none -o /tmp/qs-game-none
ls /tmp/qs-game-none/.claude/skills/fs-gg-symbology/SKILL.md   # present
```

**Deterministic equivalent (no `dotnet new`)**: the env-free `Feature219EmitFrameworkSkillsTests`
G-EMIT matrix re-derives this from `template.json` and now includes symbology for each shipping
profile.

## Scenario 3 — Reachable via the consumer wrapper (SC-002 / US2)

```bash
ls .claude/skills/ | grep fs-gg-product-symbology   # present
ls .agents/skills/ | grep fs-gg-product-symbology   # present
```

Both wrappers route to `template/product-skills/fs-gg-symbology/SKILL.md`; the framework wrapper
`fs-gg-symbology` (bare name) still resolves to its own target — no collision.

## Scenario 4 — Parity fails honestly when the wrapper is missing (SC-003 / US3)

```bash
# the new focused test (GL-free, fixture-built) — passes
dotnet test tests/Rendering.Harness.Tests --filter Feature223
```

Manual confirmation of the blind-spot closure:

```bash
# temporarily remove the product wrapper and run the repo parity check
mv .claude/skills/fs-gg-product-symbology /tmp/  &&  mv .agents/skills/fs-gg-product-symbology /tmp/
dotnet run --project tools/Rendering.Harness -- skill-parity   # expect a MissingWrapper finding for fs-gg-symbology
# restore
mv /tmp/fs-gg-product-symbology .claude/skills/  &&  mv /tmp/fs-gg-product-symbology .agents/skills/
```

(Before this feature's parity fix, the bare `fs-gg-symbology` framework wrapper masked the hole and
**no** finding was emitted — that is the bug being closed.)

## Scenario 5 — No regressions in parity (SC-004 / FR-006)

```bash
dotnet test tests/Rendering.Harness.Tests   # Feature168 parity fixtures + Feature223: green
```

With all seven `fs-gg-product-*` wrappers present, the full parity run reports **zero**
`MissingWrapper` findings and no new findings for the previously-passing six.

## Scenario 6 — Profile decision asserted (SC-005 / FR-002)

```bash
dotnet test tests/Package.Tests --filter Feature219   # G-EMIT matrix + G-NODANGLE-SYMB green
```

The matrix encodes symbology's inclusion per profile (the documented decision: the `fs-gg-scene`
profile set); G-NODANGLE-SYMB now records the unwired set as empty and `symbology: vendored`.

## Scenario 7 — Delivered to consumers (SC-006 / FR-007/FR-008)

Out-of-repo, via the `cross-repo-coordination` skill on the next `fs-gg-ui-template` republish:
the `fs-gg-ui-template` contract entry in `FS-GG/.github` reflects the new coherent-set version, and
Coordination board item **#35**'s acceptance checklist is satisfied and the item is closed/Done.
