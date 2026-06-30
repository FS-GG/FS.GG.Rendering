# T004 — Release / content / registry gate map

The classification/root-cause map every story depends on. Each gate on the critical path, its check,
and the phase that proves it.

| # | Gate | Check (live) | Phase / Task | Status |
|---|---|---|---|---|
| G1 | Monotonic version `V > 0.1.53-preview.1` | next preview `0.1.54-preview.1`; pins bumped both | US2 / T008 | satisfied at bump |
| G2 | Content: `b78e72a` ancestor of the release tag | `git merge-base --is-ancestor b78e72a fs-gg-ui-template/v0.1.54-preview.1` → true | US2/US1 / T009,T013 | proven post-tag |
| G3 | Feed serves `V` for the whole coherent set | `gh api orgs/FS-GG/packages/nuget/<pkg>/versions` lists `0.1.54-preview.1` | US1 / T011,T012 | proven post-CI |
| G4 | Consumer install, **no exit 103** | `dotnet new install FS.GG.UI.Template::0.1.54-preview.1` → exit 0 | US1 / T014 | proven post-CI |
| G5 | `game` scaffold-selectable | `dotnet new fs-gg-ui --profile game` accepted | US1 / T015 | proven post-CI |
| G6 | Generated `game` builds + governance green, **zero** `GovernanceTests` edits | `dotnet build` + `dotnet test` in probe | US1 / T016 | proven post-CI |
| G7 | Non-game profiles unaffected (byte-identical to F220 baseline) | regen + diff | US1 / T017 | proven post-CI |
| G8 | Registry flip **only after** G3 (publish-before-flip, FR-007) | `registry/dependencies.yml` advanced to `V`, `game` released, coherence flipped | US3 / T018-T020 | after G3 |
| G9 | Board closed: #33 (+`V`,+PR), board Done, #31 unblocked, SDD#44 notified | `gh issue`/Projects v2 | US4 / T021-T023 | after G8 |

**Root cause of #33** (why the work is real): the org feed serves `0.1.53-preview.1` which **lacks**
`b78e72a` (verified, see `pre-publish-probe.md`), so the `game` profile is not feed-selectable. The
producer machinery (`release.yml` `publish-packages`, `derive-template-version.sh`, Feature-216
dispatch-sender) is intact and **unchanged** (FR-010); the only missing action is cutting a `> 0.1.53`
coherent-set release that carries `b78e72a`. No `src/` defect — this is a release-cadence gap.

**Append-only safety**: `release.yml` pushes with `--skip-duplicate`; NuGet feeds are append-only —
`0.1.53-preview.1` is never re-tagged, `V` is a new version (G1).
