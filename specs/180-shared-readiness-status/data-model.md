# Phase 1 Data Model: Shared ReadinessStatus

Three entities, all internal classification/formatting types (no persistence, no DB). Field details and
exact signatures live in [`contracts/`](./contracts/); this file fixes the conceptual model and the
invariants the migration must preserve.

## Entity: `ReadinessStatus` (shared vocabulary)

The single canonical set of readiness verdicts, home: `FS.GG.UI.Diagnostics`.

**Cases** (the conceptual union observed across all domains; FR-001):

| Case | Display token (`statusToken`) | Blocks acceptance (default) |
|------|-------------------------------|-----------------------------|
| `Accepted` | `accepted` | no |
| `Rejected` | `rejected` | yes |
| `Blocked` | `blocked` | yes |
| `Missing` | `missing-evidence` | yes |
| `Unsupported` | `unsupported` | yes |
| `EnvironmentLimited` | `environment-limited` | **no** (canonical default) |
| `Degraded` | `degraded` | yes |
| `Incomplete` | `incomplete` | yes |
| `Failed` | `failed` | yes |
| `FallbackOnly` | `fallback-only` | yes |
| `Pending` | `pending` | yes |
| `Unknown` | `unknown` | yes |

> Exact token strings are fixed by the **existing** per-domain `statusText` tables (see research R2). The
> table above MUST match those byte-for-byte; any token whose existing string differs from this default
> is emitted via a per-domain text projection, not by changing the shared token.

**Operations** (exactly one of each in the whole repo — SC-001):
- `statusToken : ReadinessStatus -> string` — the single display-text projection (FR-002).
- `blocksAcceptance : ReadinessStatus -> bool` — the single canonical accept/block rule (FR-002).
- `tryParse : string -> ReadinessStatus option` — inverse of `statusToken` (subsumes the existing
  `tryParseReadinessStatus`).

**Relationships / migration invariants**:
- `ReadinessDiagnosticStatus` (Diagnostics) is migrated to **reuse or alias** the shared cases; its
  `readinessStatusToken` becomes `statusToken` (or a thin wrapper) with **identical output tokens**.
- Each per-domain DU in `Testing.fs` keeps its **public shape unchanged** (FR-007) and gains a private
  `toShared : DomainStatus -> ReadinessStatus` projection for its shared cases; **domain-specific cases**
  (`PendingReview`, `Skipped`, `SyntheticOnly`, `CompatibilityBlocked`, `FallbackGated`, `NonBeneficial`)
  are preserved and project to their existing literal strings (FR-003, spec edge case).
- Domains whose accept/block rule diverges from the default (Feature159: `EnvironmentLimited` blocks)
  keep a one-line documented per-domain override (research R3).

**Validation rules**: `statusToken` is total (every case maps); `tryParse (statusToken s) = Some s` for
every shared case; serialized output for every domain is byte-identical to baseline (FR-006).

## Entity: `ReadinessValidatorConfig` (parameterizes the single validator)

The per-feature configuration record that drives one parameterized validator, replacing
`Feature159Readiness`, `Feature160ThroughputReadiness`, `Feature161HostLaneReadiness` (FR-004).

**Attributes** (conceptual — exact record in `contracts/validator-config.md`):
- **Required scenarios / coverage** — the scenario set each feature demands (159/160/161 differ here).
- **Required artifacts** — artifact identifiers whose absence yields a missing-artifact entry.
- **Domain-specific checks** — a list of predicate→diagnostic rules capturing each feature's unique
  validation (e.g. F160: `WarmupCount = 3`, `MeasuredRepetitions = 5`, sample-policy accepted;
  F161: host-lane facts — display-server, renderer identity, direct-rendering, refresh, driver,
  package-version-set, plus unsupported-environment facts and prior-gate statuses;
  F159: parity-passed, net-saved-work, non-beneficial vs fallback-only decision).
- **Status derivation** — the ordered rule that maps (missing scenarios, missing artifacts, unsupported
  violations, domain checks) → a `ReadinessStatus` (+ any domain-specific case).
- **Evidence fields** — the per-feature evidence payload threaded into diagnostics output.

**Validation rules / invariants**: for each of features 159/160/161, the parameterized validator driven
by its config record MUST produce a **byte-identical** status, diagnostics list, and missing-artifact
list versus baseline (FR-004 acceptance, SC-002). A new same-shaped feature MUST be expressible as a
single config entry with no validator-body copy (SC-006).

**State transitions**: none (pure validation; input evidence → verdict + diagnostics).

## Entity: `FormattingHelper` (shared serialization primitives)

The shared Markdown/JSON serialization helpers consumed by every readiness/evidence emitter (FR-005).

**Members** (exact signatures in `contracts/formatting-helpers.md`):
- `esc : string -> string` — JSON-escape (`\`, `"`, `\r`, `\n`).
- `q : string -> string` — quote = `"\"" + esc x + "\""`.
- `jsonStringArray : string list -> string` — `"[" + (xs |> List.map q |> String.concat ", ") + "]"`
  (**comma-space** form, matching the three `Testing.fs` copies).
- `jsonCounts` / `countsText` — the counts serializers used by the readiness Markdown modules.

**Relationships / invariants**:
- Replaces the **three byte-identical copies** in `Testing.fs` (`VisualReadinessMarkdown`,
  `VisualInspectionMarkdown`, `RetainedInspectionMarkdown`) — one definition each (SC-003).
- The **`Diagnostics.fs` `System.Text.Json`-based variant** (no comma-space `jsonStringArray`,
  `tokenOf`-parameterized `jsonCounts`/`countsText`) is **behaviorally distinct** and is left intact
  unless a byte-equivalent merge is proven (research R4, spec edge case).

**Validation rules**: every call site that switches to the shared helper emits **byte-identical**
output to baseline (FR-006); a change to an escaping rule is now a single edit reflected at all callers
(SC, US3 acceptance #2).
