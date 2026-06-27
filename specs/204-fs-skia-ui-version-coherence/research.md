# Phase 0 Research: fs-skia-ui-version Coherence

## R1 — Reproducible-snapshot mechanism (US2 / FR-003)

**Decision**: **Git tag + committed lockfile + recorded manifest.** Cut an annotated git tag
`fs-skia-ui/v<version>` at the resolution commit; commit `packages.lock.json` for the template
(with `RestoreLockedMode` on locked restores) so two restores resolve byte-for-byte the same set;
record the 16 `FS.GG.UI.*` IDs @ the pinned version in `contracts/snapshot-manifest.md`.

**Rationale**: Issue #1's literal complaint is "there are no git tags, so no coherent release snapshot
exists to pin against." A tag at the resolution commit makes the framework source state that produced
the set re-checkout-able; the committed lockfile makes the *restore* reproducible (not just the
source); the manifest is the human-readable record the registry can reference. This is fully in-repo,
adds no publishing infrastructure or external dependency, and matches the spec assumption ("a git tag
of the framework source … or an equivalent recorded snapshot"). Chosen by the maintainer over the
alternatives.

**Alternatives considered**:
- *Publish to an external feed (GitHub Packages / nuget.org)* — would let cross-repo consumers restore
  without the local feed, but adds a publishing pipeline + an external dependency and runs against the
  repo's current local-feed model. Rejected as out of proportion to the request.
- *Tag the historical Feature-201 commit that produced 0.1.49* — minimal version churn, but tags an
  archived commit and separates the snapshot from the phantom-pin fix. Rejected for a cleaner,
  current resolution commit.

## R2 — Phantom pins: `FS.GG.UI.Color` and `FS.GG.UI.SkillSupport`

**Decision**: **Remove both `<PackageVersion>` pins (and their now-false explanatory comments) from
`template/base/Directory.Packages.props`.** The template will pin only packages that actually ship.

**Rationale**: Evidence gathered during planning —
- `FS.GG.UI.Color` was **retired in Feature 179**: `src/ColorPolicy/ColorPolicy.fsproj` is
  `<IsPackable>false</IsPackable>` and only *preserves the `FS.GG.UI.Color` namespace in-assembly* for
  source consumers. No `FS.GG.UI.Color` NuGet package is produced; it is absent from
  `~/.local/share/nuget-local/` at every version.
- `FS.GG.UI.SkillSupport` has **no producing project** (`grep PackageId src/*/*.fsproj` yields no such
  ID) and is absent from the feed at every version.
- Both are pinned *unconditionally* (all profiles), and both carry comments asserting they let skills'
  `.fsi` references / `Contrast`/`Palettes` "resolve in any generated project" — assertions that are
  now false.

They do not break `restore` today **only because** the seed `Product.fsproj` never
`<PackageReference>`s them (under Central Package Management an orphan `<PackageVersion>` is inert).
That is exactly why `201` was green. But a consumer who follows the skills and adds
`<PackageReference Include="FS.GG.UI.Color" />` hits NU1101 — a latent incoherence (FR-001/FR-004,
edge case "more than one … set is not coherent"). Removing the dead pins is in scope and required for a
clean `coherent: true`.

**Alternatives considered**:
- *Re-introduce the packages* (re-enable Color packing, add a SkillSupport project) — out of scope:
  reverses an accepted framework decision (179) and adds product surface this feature explicitly
  excludes. Rejected.
- *Leave them as harmless orphans* — leaves stale pins to non-existent packages and false comments in
  the contract; fails FR-008 ("no stale reference") and the spec's single-coherent-set invariant.
  Rejected.

## R3 — Which version to pin

**Decision**: **Bump the pin to the framework's next coherent pack at the resolution commit**
(expected `0.1.51-preview.1`; `0.1.50-preview.1` is already the latest set in the feed, so the normal
merge bump produces `0.1.51`). The packer fixes the exact value at pack time; the plan does not
hard-code an integer. Pin the template to exactly that value and tag that commit.

**Rationale**: Tagging the *current* resolution commit (rather than an archived one) keeps the snapshot
and the phantom-pin fix together and makes "the version the template pins == the tagged snapshot ==
HEAD-at-resolution" a single coherent story. The exact integer is whatever the packer assigns; what
matters for FR-003 is that it is immutable (tagged) and reproducible (lockfile), not its value.

**Re-drift guard (edge case)**: once tagged, the pin references the *tag's* set. If framework HEAD
advances past it later, the pin must keep referencing the recorded snapshot — re-drift is a **new**
cross-repo request, not a reason to leave #1 open (Out of Scope).

**Alternatives considered**: keeping `0.1.49` (what 201 set) — viable, but 0.1.50 already supersedes it
in the feed and re-pinning to the freshly verified resolution set is cleaner. Either way the gate is
the same per-profile verification; the version is a label on a verified set.

## R4 — Per-profile coherence verification (US1 / FR-001, FR-002)

**Decision**: For **each** of the four profiles (`app`, `headless-scene`, `governed`, `sample-pack`):
re-pack the feed → `dotnet new fs-gg-ui --profile <p>` → `dotnet restore` (locked) → `dotnet build` →
run the profile's evidence/governance. A profile is coherent only when restore reports no NU1101/version
conflict, build reports no Scene-API compile error, and evidence emits. **All four** must pass under the
single pin before the contract is coherent (edge case: partial success does not justify a flip).

**Rationale**: `template/base` is not directly compilable (the seed `.fs` carry both profile branches
under `//#if`). Real generate→restore→build→evidence is the only trustworthy coherence signal; the raw
`.fsi`/pin diff is not (Feature-175 standing assumption + 201's lesson). This is the Foundational gate
that produces the evidence US3 records.

## R5 — Cross-repo reconciliation feasibility (US3 / FR-005, FR-006)

**Decision**: Do the registry flip and issue close **directly via `gh`**, gated on US1/US2 evidence.

**Rationale (verified during planning)**: `gh auth status` shows the active account `EHotwagner`
authenticated for github.com; `gh api repos/FS-GG/.github` resolves; `gh issue view 1 --repo
FS-GG/FS.GG.Rendering` shows the issue **OPEN**. So the stated "write access or coordinated owner"
dependency is satisfied directly — no coordinated-owner round-trip is required. Steps follow the
canonical protocol (`FS-GG/.github` → `docs/coordination/README.md`; the `cross-repo-coordination`
skill): update `registry/dependencies.yml` + its `docs/registry/compatibility.md` projection to
`coherent: true` referencing the resolving change, then `gh issue comment 1 --body "## Response …"` and
`gh issue close 1`. **Hard ordering**: US1+US2 evidence first; the flip/close MUST NOT precede it
(FR-007).

**Open item for tasks**: confirm the exact YAML key/shape of the `fs-skia-ui-version` row in
`registry/dependencies.yml` (clone/read `FS-GG/.github` at task time) before editing — the projection
`docs/registry/compatibility.md` must be regenerated/edited consistently so the two never disagree
(edge case "registry and issue disagree").

## R6 — `docs/UPGRADING.md` illustrative literal

**Decision**: Confirm `0.1.68-preview.1` in `template/base/docs/UPGRADING.md` is an **illustrative
example** (it carries the inline comment "the ONLY FS.GG.UI version literal" in a sample `<...>` block),
not a governed pin, and that `GovernanceTests` scopes the single-source invariant to `build.fsx` /
`Directory.Packages.props` (the engine `#r` literal + the `$(FsSkiaUiVersion)` property), not to doc
prose. If GovernanceTests does flag it, update the doc example to use a placeholder so SC-003 ("exactly
one FS.GG.UI version literal … not `0.1.0-preview.1`") holds unambiguously.

**Rationale**: SC-003 counts version *literals* across "pins, docs, and seed code." A doc example that
differs from the real pin is at worst a documentation smell; the governing single source remains
`$(FsSkiaUiVersion)`. This is a confirm-or-tidy item, not a blocker, but it is in the FR-008
"no stale reference" sweep.

## Resolved unknowns

| Unknown | Resolution |
|---------|------------|
| Snapshot mechanism (NEEDS CLARIFICATION) | R1 — git tag + committed lockfile + manifest (maintainer-selected). |
| Are Color/SkillSupport real packages? | R2 — no; both retired/absent; pins are phantom and removed. |
| Which version to pin | R3 — next coherent pack at resolution commit (expected `0.1.51-preview.1`). |
| Coherence signal | R4 — per-profile generate→restore→build→evidence; all four required. |
| Can US3 be done directly? | R5 — yes; `gh` authenticated with access; issue #1 OPEN. |
| UPGRADING.md literal | R6 — illustrative; confirm-or-tidy under FR-008. |
