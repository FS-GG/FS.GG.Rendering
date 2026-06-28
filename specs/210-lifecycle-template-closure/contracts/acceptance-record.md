# Contract: Epic Acceptance Record

The single consolidated artifact (`specs/210-lifecycle-template-closure/readiness/epic-acceptance.md`) that
lets a reviewer reach the Rendering-side close/don't-close decision without opening the three child folders
(SC-001). It **rolls up** per-value results into one conclusion — it does not merely link to child reports
(FR-001).

## Required sections

```text
# Epic Acceptance — Lifecycle-Agnostic FS.GG.UI Template

validated_package: FS.GG.UI.Template 0.1.51-preview.1
tag_anchor:        fs-gg-ui/v0.1.51-preview.1
                   (NOTE: no dedicated template tag at 0.1.51; follow-up: tag fs-gg-ui-template/v0.1.51-preview.1)
provenance:        live | verdict-core      # conclusion VALID only when live
profiles:          [app, headless-scene, governed, sample-pack]

## Gated lifecycle set (restated, per Feature 204)
- .specify/, generated constitution, .agents/, .claude/, generated AGENTS.md/CLAUDE.md

## Per-lifecycle results
| lifecycle | app | headless-scene | governed | sample-pack | gated set | product |
| spec-kit  | ... | ...            | ...      | ...         | PRESENT   | present |
| sdd       | ... | ...            | ...      | ...         | ABSENT    | present/buildable |
| none      | ... | ...            | ...      | ...         | ABSENT (no orchestrator marker) | present/buildable |

## Byte-identical default
- baseline: pre-lifecycle template output per profile (Features 204/206)
- scope:    presence AND content compared
- result:   spec-kit (== no-flag default) diff-vs-baseline = none, all 4 profiles

## Build spot-check (FR-003/FR-004 "buildable")
- scope:  `dotnet build` on the `app`-profile output for `sdd` and `none` (spec-kit follows from byte-identity)
- result: buildability = pass (both exit 0) | environment-limited (toolchain absent; disclosed, names unbuilt cell)

## Reproduction
$ dotnet new install FS.GG.UI.Template::0.1.51-preview.1 --add-source ~/.local/share/nuget-local/
$ FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx
$ dotnet new uninstall FS.GG.UI.Template

## Cross-repo remainder (referenced, not resolved here)
- FS-GG/FS.GG.SDD#1 — scaffold-path git-init/chmod obligations (REUSED)
- <decision item> — constitution ownership for lifecycle=sdd

## Conclusion
Rendering-side: CLOSE | DON'T-CLOSE — <one-sentence justification>
Epic-fully-done: false while any remainder item is open
```

## Validation rules

- `validated_package` MUST equal the user-confirmed pin `FS.GG.UI.Template 0.1.51-preview.1`.
- The conclusion is **CLOSE-eligible only if** `provenance == live` AND every per-value result matches the
  data-model rules AND byte-identical = none for all 4 profiles (zero misclassified files — SC-002/SC-003)
  AND the build spot-check is `pass` or a disclosed `environment-limited` (a failed build blocks close — SC-007).
- The record MUST be self-contained: a reviewer needs nothing but this file to reach the decision (SC-001).
- The remainder section MUST link each item exactly once (no duplicates, no untracked blockers — SC-005).
