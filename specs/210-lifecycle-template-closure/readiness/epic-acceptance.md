# Epic Acceptance — Lifecycle-Agnostic FS.GG.UI Template

> GENERATED — do not edit. Regenerate via:
> FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx

validated_package: FS.GG.UI.Template 0.1.51-preview.1
tag_anchor:        fs-gg-ui/v0.1.51-preview.1
                   (NOTE: no dedicated template tag at 0.1.51; follow-up: tag fs-gg-ui-template/v0.1.51-preview.1)
provenance:        live      # conclusion VALID only when live
profiles:          [app, headless-scene, governed, sample-pack]

## Gated lifecycle set (restated, per Feature 204)
- .specify/, generated constitution, .agents/, .claude/, generated AGENTS.md/CLAUDE.md
- present only under lifecycle == "spec-kit"; suppressed under sdd/none. The three ungated
  PRODUCT sources (base -> ./, samples -> samples/, ant overlay) are present for all values.

## Per-lifecycle results
| lifecycle | app | headless-scene | governed | sample-pack | gated set | product |
|---|---|---|---|---|---|---|
| spec-kit  | pass | pass | pass | pass | PRESENT | present |
| sdd       | pass | pass | pass | pass | ABSENT  | present/buildable |
| none      | pass | pass | pass | pass | ABSENT (no orchestrator marker) | present/buildable |

Per-profile detail (all four profiles asserted live):
- spec-kit/app: gated-present=ok product-present=ok diff-vs-baseline=none
- spec-kit/headless-scene: gated-present=ok product-present=ok diff-vs-baseline=none
- spec-kit/governed: gated-present=ok product-present=ok diff-vs-baseline=none
- spec-kit/sample-pack: gated-present=ok product-present=ok diff-vs-baseline=none
- sdd/app: gated-absent=ok product-present=ok diff-vs-default=gated-only
- sdd/headless-scene: gated-absent=ok product-present=ok diff-vs-default=gated-only
- sdd/governed: gated-absent=ok product-present=ok diff-vs-default=gated-only
- sdd/sample-pack: gated-absent=ok product-present=ok diff-vs-default=gated-only
- none/app: gated-absent=ok no-orchestrator-marker=ok none==sdd
- none/headless-scene: gated-absent=ok no-orchestrator-marker=ok none==sdd
- none/governed: gated-absent=ok no-orchestrator-marker=ok none==sdd
- none/sample-pack: gated-absent=ok no-orchestrator-marker=ok none==sdd
- unknown-lifecycle-value: rejected

## Byte-identical default
- baseline: pre-lifecycle template output per profile (Features 204/206); the default value is spec-kit.
- scope:    presence AND content compared, across all four profiles.
- result:   spec-kit (== no-flag default) diff-vs-baseline = none, all 4 profiles.
  (204 proved baseline == today's spec-kit default; this run proves no-flag default == explicit
   spec-kit byte-for-byte against the installed PACKAGE, the reproducible stand-in for that baseline.)

## Build spot-check (FR-003/FR-004 "buildable")
- scope:  dotnet build on the app-profile output for sdd and none (spec-kit follows from byte-identity).
- result: buildability = pass
          sdd/app exit 0; none/app exit 0

## Reproduction
```bash
dotnet new install FS.GG.UI.Template::0.1.51-preview.1 --add-source ~/.local/share/nuget-local/
FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx
dotnet new uninstall FS.GG.UI.Template
```

## Cross-repo remainder (tracked; both resolved at implementation time)
- FS-GG/FS.GG.SDD#1 (https://github.com/FS-GG/FS.GG.SDD/issues/1) — scaffold-path git-init/chmod obligations — RESOLVED (closed 2026-06-27; Coordination board item Done)
- Coordination board decision "P0 · cross-repo — Constitution ownership for lifecycle=sdd (Rendering vs SDD)" (DI_lADOEYAWY84Bb08WzgKrVHM) — RESOLVED (status Done; downstream P2 implementation Done). Reused per FR-010 dedupe — not re-filed.
No open cross-repo remainder item blocks full closure: each is tracked exactly once on the
FS-GG Coordination board and both are Done (dedupe per FR-010 — neither re-filed).

## Conclusion
Rendering-side: **CLOSE** — the published 0.1.51-preview.1 package emits the full Spec Kit
lifecycle surface only under spec-kit, suppresses it (product intact) under sdd/none, the
no-flag default is byte-identical to spec-kit across all four profiles, and the app-profile
sdd/none outputs build (or buildability is disclosed environment-limited).

Cross-repo remainder state (US3): both SDD-owned items the spec/204 assumed open are, at
implementation time, already tracked once and **Done** on the Coordination board — so no open
remainder blocks full closure. Epic-fully-done is therefore achievable; the board is updated
to Rendering-side complete with the (resolved) remainder attributed to its owning repo.
Epic-fully-done: ACHIEVABLE — no open cross-repo remainder item remains (both Done on the
Coordination board). The invariant "false while any remainder is open" holds vacuously.
