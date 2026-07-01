# Quickstart — verify Feature 229 (drop the `.claude/skills/` UI mirror)

Runnable recipe proving the provider writes UI skills to `.agents/skills/` only. Run from repo root.

## Prerequisites

- .NET `net10.0` SDK; the `fs-gg-ui` template installed from the working tree (pack + `dotnet new install`) so the live scaffold exercises the edited `template.json`, not a stale installed package. See the memory note "Template live-test workflow".

## 1. Deterministic gates (env-free, no `dotnet new`)

```sh
dotnet test tests/Package.Tests/Package.Tests.fsproj \
  --filter "FullyQualifiedName~Feature219|FullyQualifiedName~Feature204"
```

Expected **after** the fix: green. The corrected gates assert `sources.Length >= 9`, product-skill sources target `.agents/skills/` only, no source targets `.claude/skills/`, workspace floor `>= 6`, and the new `gated-condition:` string. **Before** the fix (post-228 template, corrected gates) they MUST be red — run the gates against the un-edited `template.json` once to see the failure, per Constitution V.

## 2. Live scaffold observation (before → after, per lifecycle)

Regenerate the lifecycle-validation report against the installed edited template:

```sh
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx
```

Then inspect the report:

```sh
grep -E "claude-product-skills|framework-skills-present|result:" \
  specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md
```

Expected lines (each covered profile):
- `spec-kit/<p>: claude-product-skills=0`  ← the Feature 229 change (was non-zero under 228)
- `sdd/<p>: claude-product-skills=0`, `none/<p>: claude-product-skills=0`
- `sdd/<p>: framework-skills-present=ok`, `none/<p>: framework-skills-present=ok`
- `result: pass`

## 3. Direct scaffold spot-check (the "app to drive")

```sh
# after: no UI skills under .claude/skills/ for a spec-kit scaffold
dotnet new fs-gg-ui -o /tmp/fx229-speckit --profile game --lifecycle spec-kit
ls /tmp/fx229-speckit/.claude/skills/ | grep -c '^fs-gg-\(scene\|symbology\|skiaviewer\|elmish\|keyboard-input\|ui-widgets\|styling\|layout\)$'   # expect 0
ls /tmp/fx229-speckit/.agents/skills/ | grep -c '^fs-gg-'                                                                                          # expect the full UI set

# sdd: provider writes nothing to .claude/skills/ UI skills
dotnet new fs-gg-ui -o /tmp/fx229-sdd --profile game --lifecycle sdd
ls /tmp/fx229-sdd/.claude/skills/ 2>/dev/null | grep -c '^fs-gg-\(scene\|symbology\)'   # expect 0
```

The SDD-orchestrated end-to-end check (`fsgg-sdd scaffold --provider rendering …` returns success, no `providerWroteSddTree`) belongs to the cross-repo composition and is verified once the orchestrator half (SDD#57) and this re-release are both published (publish-before-flip).

## 4. Provider-tree-unchanged check (FR-002/SC-003)

Diff the `.agents/skills/` UI set of a post-fix `spec-kit` scaffold against the pre-fix baseline — MUST be identical (0 added, 0 removed). `sdd` and `none` scaffolds MUST produce identical skill-tree output to each other.

## Evidence to capture under `readiness/`

- `leak-before-speckit.md` — pre-fix `spec-kit` scaffold showing UI skills under `.claude/skills/`.
- `fixed-scaffold-{speckit,sdd,none}.md` — post-fix scaffolds showing `.claude/skills/` UI = 0, `.agents/skills/` intact.
- `agents-tree-intact.md` — `.agents/skills/` set identical to baseline across lifecycles.
- `gate-transcripts.md` — Feature 204/219 red-before / green-after.
- `success-criteria.md` — SC-001..SC-006 mapped to evidence.
