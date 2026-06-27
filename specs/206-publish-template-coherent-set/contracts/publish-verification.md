# Contract: Publish Verification Gates (PV-1..PV-6)

These gates run **against the installed published package** (not the working tree), except the
post-tag reproduction check (PV-6b) which runs against the cut tag. PV-1..PV-5 and PV-6a must be green
before the coherent-set tag is cut (US2); PV-6b runs immediately after tagging; all gates must be
green before the cross-repo record is reconciled (US3). A red gate blocks the release and leaves the
record in-progress (FR-010).

| Gate | Requirement | How verified | Spec ref |
|---|---|---|---|
| **PV-1** Resolvable new version | Installing the template by id from the feed resolves `0.1.50-preview.1`, strictly newer than `0.1.17-preview.1`. | `dotnet new install FS.GG.UI.Template::0.1.50-preview.1`; `dotnet new fs-gg-ui --help` lists the template; confirm resolved version. | FR-002, FR-004, SC-001 |
| **PV-2** Manifest surfaces present | The packaged `template.json` exposes the `lifecycle` choice symbol and the `initGit` opt-in; `skipGitInit` is absent. | `Feature204LifecycleTemplateTests` + `Feature205TemplateSideEffectTests` (GV-1, GV-2) against the packed manifest. | FR-001, SC-001 |
| **PV-3** Byte-identical default | `lifecycle=spec-kit` (default) output is byte-identical to the prior published baseline for **every** profile. | `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx`; per-profile byte-diff vs baseline. Zero diffs. | FR-005, SC-002 |
| **PV-4** Side-effect-free default | Scaffolding with no git flag in a headless context creates **zero** repos, spawns **zero** processes, returns promptly. | Instantiate each profile from the installed package without `--initGit`; assert no `.git`, no spawned process; `Feature205TemplateSideEffectTests` GV-3..GV-6. | FR-006, SC-003 |
| **PV-5** Lifecycle variants emit correctly | `lifecycle=sdd` and `lifecycle=none` each emit the Spec-Kit-absent file set; `spec-kit` emits the Spec-Kit-present set. | Instantiate per lifecycle value; confirm presence/absence of `.specify/`, constitution, agent-context tree. | FR-004 (US1 AS-4) |
| **PV-6a** Profiles restore & build from the package (pre-tag) | All four profiles, scaffolded from the installed package `FS.GG.UI.Template::0.1.50-preview.1`, restore and build against one consistent `FS.GG.UI.*` version with zero missing-package / version-conflict errors; two installs at different times resolve identically. | Per profile: `dotnet new fs-gg-ui --profile <p>` → `dotnet restore` → `dotnet build` → evidence; double-restore into two clean caches, diff lockfiles. | FR-009, SC-004, SC-005 |
| **PV-6b** From-tag reproduction (post-tag) | A clean checkout of `fs-gg-ui-template/v0.1.50-preview.1` carries the `<Version>` bump and a from-tag repack reproduces `FS.GG.UI.Template.0.1.50-preview.1.nupkg`; the framework packages it pins are already resolvable at `0.1.50-preview.1`. | `git checkout` the tag → `grep '<Version>'` → `dotnet pack` reproduces the package (see `coherent-set.md`). | FR-009 |

> **Gate ordering.** PV-1..PV-5 and **PV-6a** are pre-tag gates — all green before the coherent-set
> tag is cut (US2). **PV-6b** is the post-tag reproduction check (it requires the tag to exist), run
> immediately after tagging. The tag binds the same tree the package was packed from, so PV-6a against
> the installed package and PV-6b against the tag exercise the same artifact.

## Non-regression backstop

`dotnet fsi scripts/baseline-tests.fsx` before and after — no project flips from green to red
(mirrors 204 T001/T025). Disclose any pre-existing reds.

## Failure handling

If any PV gate is red: stop, do not tag, do not reconcile, record the blocker in
`readiness/publish-evidence.md`, and leave the cross-repo record in-progress (FR-010). For a version
or tag collision, pick the next unused identifier per `coherent-set.md` collision rules.
