# Data Model: Host Performance Lane Ledger

## Host Performance Lane

**Purpose**: Named environment scope for compositor timing evidence and performance claims.

**Fields**:

- Lane id.
- Display server.
- Display identity.
- Renderer identity.
- Direct rendering status.
- Driver identity.
- Refresh behavior.
- Package version set.
- Host profile id.
- Scenario definition id.
- Timing policy id.
- Environment notes.

**Validation rules**:

- The current report-defined lane is X11 `:1` with direct OpenGL on AMD Radeon/Mesa and profile
  `probe-08a47c01`.
- A lane is accepted only when collected timing-run facts confirm the lane.
- Different display servers, renderers, direct-rendering modes, drivers, package versions, host
  profiles, scenario definitions, or timing policies define different lanes.

## Host Fact Set

**Purpose**: Required facts that make a timing run comparable and reviewable for a lane.

**Fields**:

- Display server.
- Display identity.
- Renderer identity.
- Direct rendering status.
- Refresh rate or reason unavailable.
- Driver identity.
- Package version set.
- CPU/GPU load notes.
- Known environment limits.
- Host profile.
- Run identity.
- Scenario identity.
- Timing policy identity.
- Collection time.
- Artifact locations.

**Validation rules**:

- Accepted lane-scoped evidence requires every required field to be present or, for refresh rate,
  an explicit unavailable reason.
- Missing, ambiguous, contradictory, stale, unreadable, or cross-run facts prevent acceptance.
- Facts must be tied to the timing run identity, not only the later readiness assembly environment.

## Lane Ledger Entry

**Purpose**: Durable record connecting one timing run to its host facts and acceptance status.

**Fields**:

- Entry id.
- Timing run id.
- Host fact set.
- Lane id.
- Scenario coverage.
- Timing policy id.
- Prior gate links.
- Inclusion status.
- Primary exclusion reason when not accepted.
- Secondary diagnostics.
- Artifact paths.

**Validation rules**:

- Every timing run considered by Feature 161 readiness produces a ledger entry or an excluded
  evidence record.
- Accepted entries require a complete host fact set and no cross-lane aggregation.
- Rejected entries preserve facts when available so noisy or excluded runs remain auditable.

## Timing Run

**Purpose**: Measured compositor performance run that may be accepted, rejected, noisy,
environment-limited, fallback-only, or contextual for a lane.

**Fields**:

- Run id.
- Host profile id.
- Policy id.
- Scenario ids.
- Package version set.
- Timing status.
- Noise status.
- Sample artifact paths.
- Lane ledger entry id.

**Validation rules**:

- Timing runs with different profiles, policies, scenarios, packages, or run identities cannot be
  combined into one accepted lane result.
- Noisy timing can retain complete lane facts but cannot accept the shipped performance claim.
- Unsupported, missing-display, indirect-rendering, software-raster, and unknown-renderer runs
  contribute zero accepted performance artifacts.

## Prior Gate Link

**Purpose**: Evidence that Feature 161 is evaluating host lane facts alongside the earlier P7
performance gates.

**Fields**:

- Correctness acceptance status.
- Damage-scissored readiness status.
- Proof/readback separation status.
- Reuse/promotion evidence status.
- Throughput status.
- Artifact references.
- Staleness marker.

**Validation rules**:

- Accepted claim scope requires links to current prior-gate artifacts.
- Missing, stale, failing, interrupted, or undocumented prior gates keep the final performance claim
  `performance-not-accepted`.
- Prior-gate failure does not erase lane facts; it blocks claim acceptance.

## Claim Scope

**Purpose**: Reviewer-visible statement of which host lane a performance result applies to.

**Fields**:

- Claim status.
- Accepted lane id when available.
- Non-generalized lanes.
- Remaining blockers.
- Supporting ledger entries.
- Compatibility notes.

**Validation rules**:

- The claim scope must name the accepted lane before any performance claim can be accepted.
- It must state that the accepted lane does not generalize to Wayland, indirect GL,
  missing-display, software-raster, virtualized, or unknown lanes unless separately accepted.
- `performance-accepted` is valid only when timing is not noisy, Feature 159 counters are
  net-positive, Feature 160 throughput is accepted, and Feature 161 lane facts are complete for the
  claimed lane.

## Environment Limit

**Purpose**: Host condition that prevents accepted performance evidence.

**Reason tokens**:

- `missing-display`
- `indirect-rendering`
- `software-raster`
- `unknown-renderer`
- `virtualized-presentation`
- `ambiguous-gpu`
- `refresh-rate-unavailable`
- `package-version-mismatch`
- `load-non-representative`
- `host-facts-missing`
- `host-facts-contradictory`
- `cross-lane-evidence`
- `stale-evidence`

**Validation rules**:

- Every environment-limited or excluded entry has exactly one primary reason token.
- Environment-limited entries record zero accepted lane-scoped performance artifacts.
- Secondary diagnostics may add detail without changing the primary reason.

## Package Version Set

**Purpose**: Package and source identity associated with a timing run.

**Fields**:

- Source package versions.
- Template package version when relevant.
- Harness version.
- Surface-baseline identity.
- Commit or implementation state reference.

**Validation rules**:

- Mixed or stale package version sets cannot be accepted as one lane result.
- Package version drift must be visible in compatibility and package validation artifacts.

## Load Note

**Purpose**: CPU/GPU load, thermal, power, and concurrent-workload context that helps reviewers
interpret timing noise.

**Fields**:

- CPU load note.
- GPU load note.
- Power or thermal note.
- Concurrent workload note.
- Representative or non-representative classification.

**Validation rules**:

- Missing load notes prevent accepted lane-scoped evidence.
- Non-representative load keeps the run auditable but prevents performance claim acceptance.

## Readiness Result

**Purpose**: Feature 161 host-lane scoping status.

**Values**:

- `accepted`
- `rejected`
- `fallback-only`
- `environment-limited`
- `blocked`

**Fields**:

- Status.
- Accepted lane id.
- Complete ledger entry count.
- Excluded entry count.
- Unsupported-host result.
- Prior gate status.
- Performance claim status.
- Artifact paths.
- Limitations.

**Validation rules**:

- `accepted` means the host-lane facts are complete and scoped for the claimed lane, not that a
  shipped performance claim is necessarily accepted.
- `blocked` is used when lane facts are complete but another required P7 gate or full validation
  blocks claim/release readiness.
- `environment-limited` records zero accepted performance artifacts.

## Workflow State Transitions

```text
initialized -> timing-run-discovered -> lane-facts-collected -> lane-facts-validated
            -> prior-gates-linked -> entry-classified -> entries-aggregated
            -> claim-scope-rendered -> summary-published
            -> accepted | rejected | fallback-only | environment-limited | blocked
```

Invalid transitions record diagnostics and leave accepted lane-scoped artifact counts unchanged.
