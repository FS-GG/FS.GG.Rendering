# Data Model: Skill Parity and Evidence Guidance

## Skill Surface

Represents one supported directory or source class that contains skill files.

**Fields**

- `SurfaceId`: stable id such as `codex-local`, `claude`, `package-canonical`,
  `template-canonical`, `ant-canonical`, or `spec-kit-command`.
- `DisplayName`: reviewer-facing name.
- `Roots`: every repository-relative path this surface reads. A root is either a
  directory, scanned recursively for `SKILL.md`, or a single `SKILL.md` file.
  Issue #1092 replaced the single `RootPath` string with this list: four of the
  six default surfaces resolved from a path hard-coded in the resolver while
  `RootPath` was published as the report's `Root` column and read by nothing,
  and `spec-kit-command`'s value was English prose (`.agents/skills/speckit-*
  and .claude/skills/speckit-*`) rather than a path.
- `Selector`: how the bodies under `Roots` are narrowed — `every-skill-body`,
  `area-skill-bodies`, `non-mirrored-bodies`, `agent-wrappers`, or
  `command-wrappers`. A selector may only *narrow* what `Roots` produced; it can
  never introduce a path from outside them.
- `SurfaceKind`: `canonical`, `wrapper`, or `mixed`.
- `Agent`: `codex`, `claude`, `generated-product`, `package`, `spec-kit`, or
  `repository`.
- `IsRequired`: whether this surface participates in required parity.
- `Notes`: caveats, including machine-local surfaces intentionally excluded.

**Validation Rules**

- `SurfaceId` is unique.
- Every entry in `Roots` is repository-relative unless fixture mode or an
  operator override supplies a temporary absolute root.
- A root is a real path: no whitespace, no glob metacharacters. The narrowing a
  glob would express belongs in `Selector`, and prose belongs in `Notes`.
- Every file a surface inventories is beneath one of the roots it declares, so
  the report's `Root` column is a checkable claim about where the gate looks and
  not a comment that happens to agree with it.
- `--surface <id>=<path>` REPLACES the surface set: the run inspects the
  surfaces named on the command line and no others. It reaches *any* surface id,
  including the ids the resolver narrows, but an override is a fresh declaration
  rather than a patch — the resulting surface carries the operator's root, the
  `every-skill-body` selector, and `IsRequired`, and inherits no narrowing from
  the id it shares a name with. A narrowed run states its own coverage: see
  `contracts/skill-parity-cli.md`.
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
- `ApiSymbols`: resolution results for the API symbols this entry documents.
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

## API Symbol

Represents one `Module.member` a canonical or command skill documents inside an
F# code fence. This replaces the substring-matched guidance rules of the
original feature (see #189): a skill can no longer stay green by containing the
right words, because what is checked is the API it actually names.

**Fields**

- `Symbol`: the qualified `Module.member` as written in the fence.
- `SkillName`
- `SurfaceId`
- `Path`: the canonical skill source that documents it.
- `Status`: `exercised`, `unexercised`, or `unresolved`.

**Resolution**

- The **closed world** is the set of modules in the member-granular public
  surface baseline under `readiness/surface-baselines/members/`. A `Module.member`
  whose module is absent is product-local or pseudo-code, and is not judged.
- A symbol whose module is known but whose member is absent from the baseline is
  `unresolved` — the skill documents an API that does not exist.
- A symbol present in the baseline that no test source calls is `unexercised` —
  the seam it documents may be dead.
- A symbol present in the baseline that a test source calls is `exercised`.

**Validation Rules**

- `unresolved` is a high-severity finding; `unexercised` is a warning.
- Comments and string literals are stripped from both skill fences and test sources,
  so *mentioning* an API cannot pass for *documenting* or *exercising* it.
- Both inputs are required. If either the surface baseline or the test corpus is
  absent, no symbol is judged and the report carries a caveat saying so.
- A canonical or command skill that shows F# examples but names no baseline module
  yields no coverage row, so the report names it in a caveat.
- The check's known limitations are contracted in `contracts/api-symbol-coverage.md`.

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
- `ListSymbolsOnly`
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
- `Symbol`
- `Message`
- `Remediation`
- `ExceptionId`

**Finding Categories**

- `missing-wrapper`
- `wrapper-only`
- `stale-description`
- `broken-target`
- `canonical-drift`
- `unresolved-api-symbol`
- `unexercised-api-symbol`
- `metadata-drift`
- `intentional-exception`
- `unreadable-surface`

**Validation Rules**

- Every finding includes a remediation hint.
- `broken-target`, unreadable required surface, and `unresolved-api-symbol` are
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
- `ApiSymbolCoverage`
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
- `Symbols`
- `Report`
- `Diagnostics`

## Checker Messages

| Message | Meaning |
|---------|---------|
| `RunRequested` | Operator requested a repository or fixture parity check. |
| `InventoryLoaded` | Skill surfaces and entries were read. |
| `InventoryFailed` | Required surface or skill file could not be read. |
| `TargetsResolved` | Wrapper targets were normalized and classified. |
| `SymbolsResolved` | Documented API symbols were resolved against the surface baseline and test corpus. |
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
