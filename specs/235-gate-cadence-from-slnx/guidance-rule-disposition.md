# Disposition of the seven guidance rules

**Created**: 2026-07-09 · **Resolves**: [FS-GG/FS.GG.Rendering#222](https://github.com/FS-GG/FS.GG.Rendering/issues/222)

`SkillParity`'s guidance-rule layer was deleted in #189 (PR #221) and replaced, for **API claims**,
by a symbol check that resolves what a skill names against the public surface baseline. The layer's
other half — **process prose** — was not replaced, and for a while nothing checked it. This note
records, for each of the seven rules, whether it was replaced, narrowed, or accepted as lost.

## Why the old rules were not assurance

Each rule was a list of `RequiredReferences`, matched with `content.Contains token`. A skill
satisfied `package-pin-drift` by containing the phrase `local feed`. That is satisfiable by prose,
so a skill could drift arbitrarily far from the machinery it documents and stay green. #189 was
right to delete it.

## What replaced it

A **guarded theme** (`SkillParity.GuardedTheme`) names the repository artifacts its in-scope skills
must point at. Two closed worlds adjudicate:

| Artifact | Resolves against |
|---|---|
| `HarnessCommand verb` | the dispatch table in `tools/Rendering.Harness/Cli.fs` |
| `RepoPath path` | the filesystem, beneath the repository root |

A skill satisfies a theme only by naming, **in an inline code span**, an artifact that resolves.
Prose cannot satisfy it, because prose cannot create a dispatched verb. Two regressions the deleted
rules could not see are now findings:

- delete the guidance from a skill in scope → `missing-required-artifact`
- rename the artifact underneath intact guidance → `unresolved-artifact-reference`

Both are `High`, so they fail the check at its default `--fail-on`. If `Cli.fs` is unreadable the
layer resolves nothing and says so in the report's caveats, rather than reporting a green it did not
earn — the same degrade the API-symbol layer performs when its baseline is absent.

The scan that *locates* a candidate reference is textual, as any link check must be. The
**verdict is resolution**: the named artifact has to exist. That is the line between this and a
substring rule.

## The seven rules

| Rule | Disposition | Rationale |
|---|---|---|
| `package-pin-drift` | **Replaced** — theme `package-pin-drift` | Its prose pointed at real machinery: the `package-feed` harness verb and `scripts/refresh-local-feed-and-samples.fsx`. Both resolve. Satisfies **FR-006** behaviourally. |
| `post-merge-package-bump` | **Narrowed** — theme `post-merge-package-bump` | Of `package bump`, `local feed`, `sample package pins`, `restore`, `readiness ledger`, only the local-feed proof names an artifact: the `package-feed` verb. The other four references are prose and are no longer required. |
| `readiness-allowlisting` | **Narrowed** — theme `readiness-allowlisting` | `.gitignore` is repository-owned and required. `git check-ignore` is not repository-owned, so it cannot be resolved here and is not required. `specs/*/readiness/` is a product-relative pattern, not a path in this repository, and is not required. |
| `validation-output-isolation` | **Accepted as lost** | Its references — `dotnet test`, `same project/configuration`, `BaseOutputPath` — name an external tool and an MSBuild property. Neither is a repository artifact this checker can resolve. A structural check here would only re-match substrings. |
| `visual-readiness` | **Accepted as lost** | `screenshot`, `degraded`, `reviewer`, `accepted readiness` are vocabulary describing a reviewer's judgement. There is no artifact to resolve. |
| `responsiveness-diagnostics` | **Accepted as lost** | `pointer`, `keyboard`, `routing`, `render`/`present` are vocabulary. Where the skills make an *API* claim about these seams, the symbol layer added in #189 now checks it — which is the honest half of what this rule was reaching for. |
| `evidence-honesty` | **Accepted as lost** | `canceled`, `synthetic`, `caveat`, `pending-review` are vocabulary about how evidence is *narrated*. Whether a caveat is visible in a generated summary is a property of the summary, not of the skill; checking the skill's words never established it. |

Four rules are lost on purpose. A check that cannot fail on a real regression is the false assurance
#189 set out to delete, and reinstating one in structural clothing would repeat the mistake. What
those four guarded is now the reviewer's job in PR, which is where it always actually happened.

## Where this is enforced

- `tools/Rendering.Harness/SkillParity.fs` — `defaultGuardedThemes`, `loadHarnessCommands`,
  `evaluateArtifactReferences`.
- `tests/Rendering.Harness.Tests/Feature222GuardedThemeTests.fs` — including a test that the
  `fs-gg-samples` skill still points at the local-feed proof workflow (FR-006), and one that every
  theme covers at least one skill, so a theme whose scope stopped matching cannot read as a pass.
- The `Guarded Theme Coverage` table in `docs/reports/skills-parity.md` and the
  `guardedThemeCoverage` array in the summary JSON.
