# Research: Skill Parity and Evidence Guidance

## Decision: Treat repository-local wrappers as the supported agent surfaces

**Rationale**: The checkout has two complete wrapper directories:
`.agents/skills` for Codex/local-agent use and `.claude/skills` for Claude use.
Both contain short wrappers that route to canonical package, template, or local
skill sources. Installed global Codex skills under user-specific paths are not a
stable repository artifact and cannot be required for a repo-local checker.

**Alternatives considered**:

- Check global `$CODEX_HOME/skills`: rejected for the MVP because it is
  machine-local, may not exist on CI or reviewer machines, and would make the
  report non-reproducible.
- Treat `.claude/skills` as canonical for every skill: rejected because most
  Claude files are wrappers just like `.agents/skills`.

## Decision: Identify canonical skill sources before editing wrappers

**Rationale**: The wrapper files already name their routed source path in a
standard "Before acting, read..." section. Package-owned skills under
`src/*/skill/SKILL.md`, generated-product skills under `template/**`, and the
Ant Design skill under `.claude/skills/fs-gg-ant-design/SKILL.md` contain the
actual guidance. The checker can normalize each wrapper target path and compare
wrapper metadata against the target.

**Alternatives considered**:

- Infer canonical sources from skill names alone: rejected because product,
  package, and command skills with similar names route to different locations.
- Require a separate manifest before implementation: rejected for the MVP
  because the existing wrapper target lines already provide enough structure.

## Decision: Implement the checker in `Rendering.Harness.SkillParity`

**Rationale**: `Rendering.Harness` already owns repository validation tooling,
including package-feed proof, validation lanes, summary rendering, and CLI
subcommands. Adding a dedicated F# module keeps parsing and report generation
testable through `.fsi`, Expecto, and fixture runs while allowing a thin
`scripts/check-agent-skill-parity.fsx` wrapper.

**Alternatives considered**:

- Shell-only script: rejected because robust relative path resolution, front
  matter parsing, fixture cases, JSON output, and coverage reports are easier to
  test in F#.
- New production package: rejected because this is maintainer tooling and should
  not add public runtime package surface.

## Decision: Keep the first checker non-destructive by default

**Rationale**: The spec requires reporting missing, stale, wrapper-only, broken,
and drift findings without modifying files unless an explicit update mode is
introduced later. Non-destructive output lets maintainers review intentional
exceptions and avoids accidentally overwriting nuanced guidance.

**Alternatives considered**:

- Auto-rewrite wrappers on every run: rejected for the MVP because it can hide
  intentional differences and makes evidence less reviewable.
- Fail only on high-severity findings: rejected as the only behavior because
  warning-level guidance gaps still need reviewer visibility.

## Decision: Use seven required guidance themes

**Rationale**: The success criteria define seven durable guidance themes:
package-pin drift with package-feed proof, readiness evidence allowlisting,
validation output isolation, visual readiness, responsiveness diagnostics,
post-merge package bump validation, and evidence honesty. The checker should
report coverage by these themes rather than by brittle exact paragraphs.

**Alternatives considered**:

- Exact text hash for each guidance paragraph: rejected because canonical skills
  differ by domain and should be allowed to phrase the same rule naturally.
- Free-form reviewer-only checklist: rejected because the parity report needs
  machine-readable coverage status.

## Decision: Use deterministic rule matching plus explicit exceptions

**Rationale**: Each guidance rule can define required phrase families and
required references, such as `refresh-local-feed-and-samples.fsx`,
`git check-ignore`, `scripts/run-validation-lanes.fsx`, `VisualReadiness`,
`responsiveness`, and evidence caveat tokens. The report can classify a rule as
covered, missing, not applicable, or excepted, and every exception must be
explicit with owner/reason/review date.

**Alternatives considered**:

- Semantic language-model comparison: rejected because the checker must be
  deterministic, offline, and reproducible.
- Single global keyword search across the repository: rejected because coverage
  must be tied to relevant skills and wrappers.

## Decision: Generate both durable and feature-readiness reports

**Rationale**: `docs/reports/skills-parity.md` gives maintainers a durable entry
point. Feature readiness files under `specs/168-skill-parity-evidence/readiness/`
prove the implementation result for this feature and can include JSON summaries,
fixture results, validation logs, and caveats.

**Alternatives considered**:

- Only write `docs/reports/skills-parity.md`: rejected because implementation
  readiness needs feature-scoped evidence.
- Only write readiness artifacts: rejected because future reviewers need a
  stable report location outside one feature directory.

## Decision: Prove finding types with controlled fixture mode

**Rationale**: Fixture mode can construct small canonical/wrapper surfaces that
deliberately include a missing wrapper, wrapper-only skill, stale description,
broken target path, canonical drift, and guidance-rule gap. This proves the
checker detects each required class without corrupting real repository skills.

**Alternatives considered**:

- Mutate real wrappers during tests: rejected because tests should not dirty the
  worktree or risk hiding real guidance.
- Rely only on the current repository state: rejected because a clean repository
  cannot prove negative cases.

## Decision: Preserve metadata and wrapper minimalism

**Rationale**: Skill discovery depends on front matter such as `name`,
`description`, `user-invocable`, and compatibility fields. Wrapper files should
remain short route pointers rather than duplicating canonical guidance, so parity
checking can flag stale metadata without introducing divergent guidance bodies.

**Alternatives considered**:

- Copy full canonical guidance into every wrapper: rejected because it creates
  multiple sources of truth.
- Strip wrapper metadata and rely only on target metadata: rejected because
  agent skill discovery uses wrapper metadata.

## Decision: Require readiness allowlist proof when feature evidence is committed

**Rationale**: `.gitignore` ignores `specs/*/readiness/` by default and prior
features are allowlisted one by one. This feature's readiness artifacts are
deliverables, so implementation tasks must add a feature-specific allowlist and
record `git check-ignore -v` proof before treating evidence as committed.

**Alternatives considered**:

- Remove the broad readiness ignore rule: rejected because most readiness output
  is transient.
- Skip readiness artifacts: rejected because the spec requires parity-check
  output, generated report, and guidance coverage evidence.

## Decision: Validate through focused tests and the existing validation lane

**Rationale**: Focused Feature 168 tests should prove checker behavior quickly.
The existing `rendering-harness` validation lane then exercises the harness
contract in the repository's standard lane runner. Package-consuming sample
guidance can reference the existing package-feed proof workflow instead of
adding new package validation functionality.

**Alternatives considered**:

- Full-solution test as the only proof: rejected because Feature 166 documented
  why the full solution lane is aggregate evidence, not the only authoritative
  readiness signal.
- New validation framework: rejected because Expecto and the harness CLI are
  already sufficient.
