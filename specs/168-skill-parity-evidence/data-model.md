# Data Model: Skill Parity and Evidence Guidance

## Skill Surface

Represents one supported directory or source class that contains skill files.

**Fields**

- `SurfaceId`: stable id such as `codex-local`, `claude`, `package-canonical`,
  `template-canonical`, `ant-canonical`, or `spec-kit-command`.
- `DisplayName`: reviewer-facing name.
- `RootPath`: repository-relative root path.
- `SurfaceKind`: `canonical`, `wrapper`, or `mixed`.
- `Agent`: `codex`, `claude`, `generated-product`, `package`, `spec-kit`, or
  `repository`.
- `IsRequired`: whether this surface participates in required parity.
- `Notes`: caveats, including machine-local surfaces intentionally excluded.

**Validation Rules**

- `SurfaceId` is unique.
- `RootPath` is repository-relative unless fixture mode supplies a temporary
  absolute root.
- Required wrapper surfaces must be readable before parity status can pass.
- Mixed surfaces must classify each skill entry as canonical or wrapper.

## Skill Entry

Represents one `SKILL.md` file.

**Fields**

- `SkillName`: front-matter `name`.
- `Description`: front-matter `description`.
- `Path`: repository-relative file path.
- `SurfaceId`: owning surface.
- `EntryKind`: `canonical`, `wrapper`, `command`, or `wrapper-only`.
- `Metadata`: parsed front-matter key/value pairs that must be preserved.
- `BodyHash`: normalized body hash for drift checks.
- `GuidanceRules`: coverage results for the required guidance themes.
- `WrapperTarget`: optional resolved target when this entry routes to another
  skill.

**Validation Rules**

- `SkillName` is non-empty.
- `Path` points to an existing `SKILL.md`.
- `Description` is non-empty for discoverable skills.
- Public wrapper metadata needed for discovery is preserved unless an intentional
  exception is recorded.
- A wrapper with a target must resolve to an existing canonical source or produce
  a `broken-target` finding.

## Wrapper Target

Represents a routed canonical source referenced by a wrapper.

**Fields**

- `RawTarget`: target text as written in the wrapper.
- `ResolvedPath`: normalized absolute or repository-relative path.
- `Exists`: whether the target exists.
- `CanonicalSkillName`: parsed target skill name when readable.
- `CanonicalDescription`: parsed target description when readable.
- `TargetHash`: normalized target body hash.

**Validation Rules**

- Relative targets resolve from the wrapper file directory.
- Targets outside the repository are allowed only when explicitly marked as
  external and excluded from required repository parity.
- Broken targets are high-severity findings.

## Guidance Rule

Represents one required repository trap that must be visible in relevant skills.

**Fields**

- `RuleId`: stable id.
- `Theme`: one of the seven required themes.
- `Description`: reviewer-facing rule.
- `RequiredReferences`: scripts, commands, paths, or terms that prove concrete
  guidance.
- `ApplicableSkillPatterns`: skill names or source paths where the rule applies.
- `MinimumCoverage`: `required`, `recommended`, or `not-applicable`.

**Required Rule Themes**

- `package-pin-drift`: package-consuming samples compare current `FS.GG.UI.*`
  package versions and use `scripts/refresh-local-feed-and-samples.fsx` or the
  package-feed proof workflow.
- `readiness-allowlisting`: committed feature readiness evidence is ignored by
  default until allowlisted, and `git check-ignore -v` proof is required.
- `validation-output-isolation`: same project/configuration test runs are not
  run concurrently unless output paths are explicitly isolated.
- `visual-readiness`: real screenshots are preferred, degraded capture is
  disclosed, reviewer classification gates accepted readiness, and generated
  summaries preserve manual caveats.
- `responsiveness-diagnostics`: interactive samples validate pointer and
  keyboard activation separately from screenshot readiness and distinguish input
  routing from update/render/present latency.
- `post-merge-package-bump`: merge/post-merge work records package bump
  evidence, packs to the local feed, aligns sample package pins, restores or
  validates package-consuming samples, and updates readiness ledgers.
- `evidence-honesty`: canceled, timed-out, skipped, synthetic, substitute,
  degraded, pending-review, or environment-limited checks are never reported as
  fully green without visible caveats.

**Validation Rules**

- Each rule has at least one applicable canonical skill.
- A wrapper can inherit a rule from its canonical target only when the target is
  valid and the wrapper does not contradict the target.
- Exceptions are explicit and do not hide unrelated findings.

## Guidance Coverage

Represents coverage for one rule on one skill entry.

**Fields**

- `RuleId`
- `SkillName`
- `SurfaceId`
- `Status`: `covered`, `missing`, `partial`, `not-applicable`, or `excepted`.
- `Evidence`: snippets, paths, or normalized reference ids that triggered
  coverage.
- `MissingReferences`: expected references not found.
- `ExceptionId`: optional intentional exception.

**Validation Rules**

- Required applicable rules cannot be `missing` in passing parity.
- `partial` coverage is at least warning severity.
- `excepted` coverage requires an `IntentionalException`.

## Intentional Exception

Represents a documented, reviewable divergence.

**Fields**

- `ExceptionId`
- `SkillName`
- `SurfaceId`
- `Category`
- `Reason`
- `Owner`
- `ReviewDate`
- `Scope`

**Validation Rules**

- Exceptions are specific to one finding or rule.
- Exceptions never suppress broken target paths.
- Expired or ownerless exceptions are findings.

## Parity Check Request

Represents one checker invocation.

**Fields**

- `RepositoryRoot`
- `CanonicalSurfaces`
- `WrapperSurfaces`
- `OutDir`
- `ReportPath`
- `FixtureMode`
- `FailOnSeverity`
- `AllowedExceptionIds`
- `ListRulesOnly`
- `JsonOutput`

**Validation Rules**

- `RepositoryRoot` exists.
- `OutDir` is writable when report generation is requested.
- `ReportPath` must not point inside an ignored readiness directory unless that
  directory is intentionally readiness output.
- Fixture mode cannot modify repository skill files.

## Parity Finding

Represents one synchronization or coverage issue.

**Fields**

- `FindingId`
- `SkillName`
- `SurfaceId`
- `Category`
- `Severity`: `info`, `warning`, `high`, or `critical`.
- `CanonicalPath`
- `WrapperPath`
- `RuleId`
- `Message`
- `Remediation`
- `ExceptionId`

**Finding Categories**

- `missing-wrapper`
- `wrapper-only`
- `stale-description`
- `broken-target`
- `canonical-drift`
- `guidance-rule-gap`
- `metadata-drift`
- `intentional-exception`
- `unreadable-surface`

**Validation Rules**

- Every finding includes a remediation hint.
- `broken-target`, unreadable required surface, and required guidance gaps are
  high or critical severity.
- Findings remain visible even when an exception downgrades severity.

## Parity Report

Represents the reviewer-readable and structured checker output.

**Fields**

- `CheckedAtUtc`
- `RepositoryRoot`
- `OverallStatus`: `passed`, `warning`, or `failed`.
- `SupportedSurfaces`
- `CanonicalSourceCount`
- `WrapperCount`
- `FindingCountsBySeverity`
- `GuidanceRuleCoverage`
- `Findings`
- `IntentionalExceptions`
- `GeneratedReportPath`
- `StructuredSummaryPath`
- `Caveats`

**Validation Rules**

- Markdown and JSON summaries agree on status, counts, findings, and coverage.
- Passing status requires zero unresolved high or critical findings.
- The report lists checked surfaces and explicitly excluded external surfaces.
- The checked date is present.

## Checker Model

Represents pure checker state for the MVU boundary.

**Fields**

- `Request`
- `Surfaces`
- `Entries`
- `Findings`
- `Coverage`
- `Report`
- `Diagnostics`

## Checker Messages

| Message | Meaning |
|---------|---------|
| `RunRequested` | Operator requested a repository or fixture parity check. |
| `InventoryLoaded` | Skill surfaces and entries were read. |
| `InventoryFailed` | Required surface or skill file could not be read. |
| `TargetsResolved` | Wrapper targets were normalized and classified. |
| `CoverageEvaluated` | Required guidance themes were evaluated. |
| `FindingsClassified` | Findings were assigned category and severity. |
| `ReportRequested` | Markdown/JSON output should be rendered. |
| `ReportWritten` | Output files were written. |

## Checker Effects

| Effect | Interpreter responsibility |
|--------|----------------------------|
| `ReadSkillSurfaces` | Enumerate supported `SKILL.md` files. |
| `ReadSkillFile` | Read and parse front matter/body. |
| `ResolveWrapperTarget` | Normalize target path and read target metadata. |
| `ReadFixtureCase` | Load controlled dry-run fixture. |
| `CreateOutputDirectory` | Create readiness/report output safely. |
| `WriteMarkdownReport` | Write reviewer report. |
| `WriteJsonSummary` | Write machine-readable report. |

## State Rules

- `RunRequested` performs request validation before any output file is written.
- Inventory failures on required surfaces produce fail-closed findings.
- Target resolution happens before wrapper drift or inherited coverage checks.
- Report writing occurs only after findings and coverage have been classified.
- A failed parity status is still a successful tool run when reports are written.
