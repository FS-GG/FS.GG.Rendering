# Phase 0 Research: Shared ReadinessStatus

All NEEDS CLARIFICATION items from the spec's assumptions are resolved below against the current tree.

## R1 — Shared module home (resolves spec "Shared module home: … To be confirmed in planning")

**Decision**: Place the shared `ReadinessStatus` vocabulary, its single `statusToken`/`blocksAcceptance`
functions, and the shared Markdown/JSON formatting helpers in **`FS.GG.UI.Diagnostics`**
(`src/Diagnostics/Diagnostics.fs` + `Diagnostics.fsi`).

**Rationale**:
- Verified dependency edges (from each `.fsproj`):
  - `Diagnostics.fsproj` → **no** `ProjectReference`s (leaf).
  - `Scene.fsproj` → no `ProjectReference`s (leaf).
  - `Testing.fsproj` → `Diagnostics`, `Scene`.
  - `SkiaViewer.fsproj` → `KeyboardInput`, `Diagnostics`, `Scene`.
- `Diagnostics` is a leaf reachable by every current readiness consumer (`Testing`, `SkiaViewer`), so a
  shared type there is **cycle-free** by construction.
- `Diagnostics` already owns readiness taxonomy (`ReadinessDiagnosticStatus`, `readinessStatusToken`,
  `tryParseReadinessStatus`), so the shared vocabulary is semantically at home and can *absorb* the
  existing Diagnostics status type as its first migration.

**Alternatives considered**:
- `Scene` (also a leaf): rejected — not the semantic owner of readiness/diagnostic concepts; would
  scatter the taxonomy.
- A new low-level `FS.GG.UI.Readiness` project: rejected — introduces a new package/surface and build
  unit for a handful of types; violates the "minimize structure / minimize dependencies" bias and the
  Phase-1 precedent of extending an existing low-tier project.

## R2 — Status vocabulary: what unifies vs. what stays domain-specific

**Inventory (verified)** — readiness-verdict DUs and their mappers:

| DU | Location | Shared-conceptual cases | Domain-specific cases |
|----|----------|-------------------------|-----------------------|
| `ReadinessDiagnosticStatus` | `Diagnostics.fs:27` | Accepted, Blocked, ReviewRequired, EnvironmentLimited | — |
| `VisualReadinessStatus` | `Testing.fs:197` | Accepted, Blocked, EnvironmentLimited, Incomplete | PendingReview |
| `LayoutReadinessStatus` | `Testing.fs:493` | Accepted, Incomplete, Failed, EnvironmentLimited, MissingEvidence | Skipped, SyntheticOnly, CompatibilityBlocked |
| `CompositorReadinessStatus` | `Testing.fs:535` | Accepted, Failed, EnvironmentLimited, MissingEvidence | FallbackGated, CompatibilityBlocked |
| `CompositorDamageReadinessStatus` | `Testing.fs:602` | Accepted, Rejected, EnvironmentLimited | FallbackOnly |
| `Feature159ReadinessStatus` | `Testing.fs:634` | Accepted, Rejected, FallbackOnly, EnvironmentLimited | NonBeneficial |
| `Feature160ThroughputReadinessStatus` | `Testing.fs:672` | Accepted, Blocked, Rejected, FallbackOnly, EnvironmentLimited | — |
| `Feature161HostLaneReadinessStatus` | `Testing.fs:709` | Accepted, Blocked, Rejected, FallbackOnly, EnvironmentLimited, MissingEvidence | — |

**Decision**: the shared `ReadinessStatus` covers the conceptual union actually observed across domains:
`Accepted | Rejected | Blocked | Missing | Unsupported | EnvironmentLimited | Degraded | Pending |
Unknown` (FR-001 minimum), plus the additional repeated cases the inventory shows are *shared*, not
domain-specific — at minimum `Incomplete`, `Failed`, `FallbackOnly`. Cases that are genuinely
single-domain (`PendingReview`, `Skipped`, `SyntheticOnly`, `CompatibilityBlocked`, `FallbackGated`,
`NonBeneficial`) are **preserved on their per-domain DU** (spec edge case: domain-specific cases are not
forced into the shared type).

**Key finding — `statusText` tokens are identical, so the string table fully unifies.** Verified:
`Accepted → "accepted"`, `EnvironmentLimited → "environment-limited"`, `Blocked → "blocked"`,
`Rejected → "rejected"`, `FallbackOnly → "fallback-only"`, `Incomplete → "incomplete"`,
`Failed → "failed"`, `MissingEvidence → "missing-evidence"` recur byte-for-byte across
`Feature159Readiness`, `LayoutReadiness`, `VisualReadiness`, etc. The ~9 copies of this table collapse
to **one** `statusToken : ReadinessStatus -> string`. Each per-domain DU keeps a tiny
`toShared : DomainStatus -> ReadinessStatus` projection (and projects its own domain-specific cases to
their existing literal strings), so each domain's serialized text stays byte-identical (FR-006, SC-004).

## R3 — `blocksAcceptance` divergence (the binding nuance)

**Finding**: the accept/block rule is **not** uniform for the same conceptual case:
- `Feature159Readiness.statusBlocksAcceptance`: only `Accepted` is non-blocking → **`EnvironmentLimited`
  blocks**.
- `LayoutReadiness.blocksAcceptance`: `Accepted` **and** `EnvironmentLimited` are non-blocking.
- `CompositorDamageReadiness.statusBlocksAcceptance`: similar Accepted+EnvironmentLimited tolerance.

**Decision**: expose **one canonical `blocksAcceptance : ReadinessStatus -> bool`** in Diagnostics
encoding the dominant rule (`Accepted` and `EnvironmentLimited` are non-blocking; everything else
blocks), satisfying SC-001's "exactly one blocks-acceptance rule" for the shared vocabulary. The (few)
domains whose historical rule **differs** (notably Feature159, which blocks on `EnvironmentLimited`)
keep a **thin, explicitly-commented per-domain override** rather than adopting the shared default —
because **behavior-stability wins over de-duplication** (spec assumption). The override is a one-line
function documented at the use site (`// per-domain: F159 blocks on environment-limited, unlike shared
default`), so the divergence is loud, not silent.

**Rationale**: forcing every domain onto a single rule would change which final status is derived for
environment-limited runs, changing serialized output — a byte-stability violation. SC-001 is read as
"one shared rule + documented exceptions," not "delete all domain logic."

**Alternative considered**: parameterize `blocksAcceptance` with a per-domain "environment-limited
blocks?" flag. Rejected for Story 1 as over-engineering for two call sites; the thin override is plainer
(Constitution III). May be revisited if more domains diverge.

## R4 — Formatting helpers: which copies truly merge

**Finding**:
- **`Testing.fs` has 3 byte-identical copies** of `esc`, `q`, `jsonStringArray`, `jsonCounts`, and
  `countsText`/`statusCountsText` — in `VisualReadinessMarkdown` (≈1235), `VisualInspectionMarkdown`
  (≈1883), and `RetainedInspectionMarkdown` (≈2583). These merge cleanly into **one** shared module.
  - `esc`: `.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")`
  - `q`: `"\"" + esc text + "\""`
  - `jsonStringArray`: `"[" + (values |> List.map q |> String.concat ", ") + "]"` (note: **", " with
    space** after comma).
- **`Diagnostics.fs` is a different implementation**, not a copy: it builds JSON via
  `System.Text.Json.JsonSerializer.Serialize` (`json`, `jsonOption`, `jsonDate`), and its
  `jsonStringArray` is `"[" + (values |> List.map json |> String.concat ",") + "]"` (**no space** after
  comma) with `jsonCounts`/`countsText` carrying an extra `tokenOf` projection parameter.

**Decision**:
- Story 3 extracts the **three identical `Testing.fs` copies** into one shared module
  (home: `Diagnostics`, consumed by `Testing`), removing the duplicates (SC-003). All `Testing.fs` call
  sites are byte-stable because the shared definitions are the same bytes.
- The **`Diagnostics.fs` variant is reconciled only where it does not change emitted bytes.** Because its
  `jsonStringArray` uses no comma-space and routes through `System.Text.Json`, it is **behaviorally
  distinct** and is **left intact** (documented in `contracts/formatting-helpers.md`) unless a
  byte-equivalent unification is demonstrable. Reconciliation that would change any caller's bytes is
  out of scope (spec edge case: "behaviorally-different helper copies … must not change emitted bytes").

**Rationale**: the spec explicitly subordinates `System.Text.Json` standardization and helper
reconciliation to byte-stability. The high-value, zero-risk win (3 identical copies → 1) is taken; the
risky cross-file merge is gated behind a byte-equality proof.

## R5 — Public surface & source-compatibility (FR-007)

**Finding**: the per-domain status DUs are declared in `Testing.fsi` (public surface; tracked by
`readiness/surface-baselines/FS.GG.UI.Testing.txt`). The formatting helpers are **not** in any `.fsi`
(private). There is **no `InternalsVisibleTo`** reaching these symbols.

**Decision**:
- **Preserve the per-domain DUs and their case names** in `Testing.fsi` unchanged → `FS.GG.UI.Testing.txt`
  stays byte-identical, and all current call sites (`VisualReadinessAccepted`, etc.) keep compiling.
  The migration collapses the *mapper bodies* (delegate to shared `statusToken`/`toShared`), not the
  public DU shapes.
- The shared `ReadinessStatus` + `statusToken` + `blocksAcceptance` + parse are **additive** public
  surface in `Diagnostics.fsi` → regenerate `FS.GG.UI.Diagnostics.txt` (Tier 1 surface-baseline update),
  documented as an additive, non-breaking change.

**Rationale**: satisfies FR-007 (no breaking surface change) with the minimal-tier footprint the spec
asks for (only the additive Diagnostics surface moves).

## R6 — Build/test, baseline capture, and the byte-diff oracle

**Decision** (commands the tasks will use):
- Build: `dotnet build FS.GG.Rendering.slnx -c Release`
- Test: `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release`
- Comprehensive baseline (globs every `*.Tests.fsproj`, incl. release-only / sample lanes):
  `dotnet fsi scripts/baseline-tests.fsx --out specs/180-shared-readiness-status/readiness/baseline.md`
  and `… --config Release --out …/post-change.md`.
- Surface drift: `dotnet fsi scripts/refresh-surface-baselines.fsx` (regenerates the `*.txt` records).

**Allowed pre-existing reds** (baseline, not regressions): `tests/Package.Tests` (release-only feed
gate) and `samples/ControlsGallery/ControlsGallery.Tests` (stale feed pins), per the feature-179
baseline. The bar is "no new failures vs. captured baseline," not "all green from zero."

**Byte-diff oracle**: snapshot the serialized readiness/evidence artifacts (the JSON/Markdown the
readiness modules emit, as asserted by the suites) before any edit; after each story, the golden
assertions in the test suite are the byte-stability check (they fail if any byte moves). `target=180`
adds no new golden files — it relies on the existing golden coverage staying green.

**Net rationale**: this is the same evidence model used by features 177/178/179 (baseline-first,
single-baseline diff per story, polish-phase full sweep), so it composes with the established Phase-3
plan and tooling.
