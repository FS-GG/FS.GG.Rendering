# Phase 0 Research: Optional FS.GG.UI BOM / Metapackage

All NEEDS CLARIFICATION from the Technical Context are resolved below. Each item records the
**Decision**, **Rationale**, and **Alternatives considered**.

## R1 — NuGet mechanism: how does "one reference ⇒ coherent set, deviation is loud" get realized?

**Decision**: Publish a **metapackage** with package ID `FS.GG.UI` whose dependencies are the 16
member packages each at an **exact version** (`[X]`), authored as a hand-written `.nuspec`
(`<dependency id="FS.GG.UI.Scene" version="[$version$]" />` ×16) packed via `dotnet pack` with
`NuspecFile` + `NuspecProperties=version=$(Version)`. The metapackage ships **no assembly**
(`IncludeBuildOutput=false`) — it is dependencies only.

**Rationale**:
- **Exact `[X]` is required for "loud" in both directions.** NuGet's "nearest-wins" plus
  downgrade-detection means:
  - With a **floating** lower bound `[X,)`: a stale direct ref `Y < X` *does* fail (NU1605
    downgrade) — good — but a newer direct ref `Y > X` *satisfies* `[X,)` and silently upgrades one
    member to Y while the rest stay X → a **mixed set**, the exact failure the epic forbids.
  - With **exact `[X]`**: both `Y < X` and `Y > X` are flagged against `[X]` → NU1605 (downgrade) /
    NU1608 (outside-constraint). This is the only range that makes both directions visible (FR-004,
    SC-003).

> **Amendment — corrected against live evidence (T006 smoke; `readiness/bom-consumer-validation.md`).**
> The draft above predicted `NU1605/NU1107` and an unconditional restore failure. The observed
> behavior on a real packed feed is: the exact `[X]` bracket produces **NU1605** (`Y<X`) and
> **NU1608** (`Y>X`) — detection in **both** directions, confirming the exact-bracket rationale — but
> these are NuGet **warnings by default**, so nearest-wins builds a mixed graph unless the consumer
> elevates them (`WarningsAsErrors=NU1605;NU1608` / `TreatWarningsAsErrors`, the FS.GG repo + governed
> `fs-gg-ui` template default). "Loud" is therefore exact-bracket detection **plus** the
> warnings-as-errors posture, not an automatic `NU1107`. The mechanism still delivers the epic's
> structural improvement: drift that previously produced **no** signal now always produces a NuGet
> diagnostic, and a hard stop under the recommended policy. This is exactly the hypothesis the plan
> told `/speckit-tasks` to treat as unverified until the live run — now verified and corrected.
- **Transitive pull gives the "one reference" UX (FR-001/FR-002).** Referencing `FS.GG.UI@X`
  brings every member at X with no second version literal in the consuming project.
- **`.nuspec` avoids the chicken-and-egg.** A nuspec records dependency metadata without needing
  the members restorable at pack time, so the BOM can be packed in the *same* `dotnet pack` pass as
  the members at a version that does not yet exist in any feed. (A `PackageReference [X]` to the
  siblings would fail to restore because X isn't published yet.)
- **Single-source version (FR-009).** Every member dependency and the BOM's own version come from
  the one `$version$` = `-p:Version=V`. No second version literal is introduced anywhere.

**Alternatives considered**:
- **SDK project with `ProjectReference` to all 16 members (auto-membership).** Attractive because
  the dependency list would track the references automatically. **Rejected**: pack turns a
  ProjectReference into a dependency with a *floating* lower-bound range (`X` ⇒ `[X,)`), which fails
  the `Y > X` mixed-set case (not loud). NuGet exposes no supported knob to force a ProjectReference
  dependency to an exact `[X]` bracket. Auto-membership is recovered instead via the R3 parity test.
- **Consumer-side CPM transitive pinning (`<ManagePackageVersionsCentrally>` + a shared props
  file).** This is what `FsSkiaUiVersion` already is. **Rejected** as the BOM mechanism: it is a
  *per-project convention*, not a *distributed artifact* — it cannot be referenced by one ID at one
  version and cannot be recorded in the cross-repo registry as a discoverable package. It is exactly
  the status quo the feature is meant to strengthen.
- **A real (compiled) façade package re-exporting the members.** **Rejected**: adds an F# surface
  (Principle II `.fsi` obligations, surface baselines) and an assembly the BOM does not need; a BOM
  is a pure dependency aggregator.

## R2 — Membership: which packages, and is the bare `FS.GG.UI` ID free?

**Decision**: The BOM enumerates the **16 packable `FS.GG.UI.*` members** recorded in feature 204's
snapshot manifest (`Build, Scene, Canvas, Controls, Controls.Elmish, DesignSystem, Diagnostics,
Elmish, KeyboardInput, Layout, SkiaViewer, Symbology, Symbology.Render, Testing, Themes.AntDesign,
Themes.Default`). The metapackage ID is the **bare `FS.GG.UI`**.

**Rationale**: `src/ColorPolicy` is `IsPackable=false` (folded in-assembly, feature 179) and so is
not a member; the retired `FS.GG.UI.Color` / `FS.GG.UI.SkillSupport` phantom IDs are absent by
design (feature 204). The bare `FS.GG.UI` ID is **not** produced by any source project and is
**absent from the local feed** (all 16 members are `FS.GG.UI.<suffix>`; the only other `FS.GG.UI.*`
feed entry is the unrelated `FS.GG.UI.Template`). So `FS.GG.UI` is free and is the conventional
brand-root metapackage name (cf. `Microsoft.AspNetCore`). `FS.GG.UI.Template` (the `dotnet new`
template) is **not** a runtime member and is excluded.

**Alternatives considered**: `FS.GG.UI.All` / `FS.GG.UI.Bom` — **rejected** as less discoverable;
the bare brand root is the idiomatic metapackage name and is available. (If a future decision wants
the bare ID reserved for something else, `FS.GG.UI.All` is the fallback — recorded here, not chosen.)

## R3 — Keeping membership in lockstep with the published set (FR-003, US2 AS3)

**Decision**: An **always-on parity test** in `tests/Package.Tests`
(`Feature207BomMembershipTests.fs`) asserts that the set of dependency IDs in `FS.GG.UI.nuspec`
equals the set of `IsPackable=true` `FS.GG.UI.*` projects discovered from `src/**` (excluding the
template), that **every** dependency uses the **single** `[$version$]` token (no literal versions),
and that every version uses the **exact bracket** form. A member added/removed without a matching
nuspec edit fails this test loudly.

**Rationale**: The hand-listed nuspec is the one manual surface the mechanism introduces; the parity
test converts "someone added a 17th package and forgot the BOM" from a silent adopter-misses-a-member
bug (the spec's explicit edge case) into a red test. This matches the constitution's endorsement of
narrow, self-paying "package-skew checks" and the existing `Package.Tests` catalog-coverage pattern.

**Alternatives considered**: generating the nuspec deps from the packable projects via an MSBuild
target — **rejected** for this feature as over-engineering (Principle III): it trades a 16-line
reviewed list + a test for build-time codegen complexity. Revisit only if membership starts churning.

## R4 — Version, channel, and same-snapshot publication (FR-005, FR-006)

**Decision**: Pack the BOM **in the same `dotnet pack FS.GG.Rendering.slnx -c Release
-p:Version=V`** that packs the 16 members, at the **next coherent version** `V` (packer-determined;
current published snapshot is `0.1.50-preview.1`, so the next preview, e.g. `0.1.51-preview.1` — the
pack fixes the exact value). Cut/extend the annotated `fs-skia-ui/v<version>` tag (feature 204
mechanism) at the resolution commit so the snapshot now includes `FS.GG.UI@V`.

**Rationale**: Re-packing the whole set + BOM at one `V` guarantees the BOM and the members it pins
are *literally the same snapshot* (FR-006) and share one version, so channel semantics are automatic
(`-preview.N` ⇒ preview; bare `x.y.z` ⇒ stable) and cannot drift (FR-005). Pinning the BOM to the
already-published `0.1.50` members *without* repacking them would violate "published as part of the
same snapshot" — rejected.

**Alternatives considered**: a standalone BOM-only pack at a hand-picked version pointing back at an
older member snapshot — **rejected**: reintroduces a second version literal and a separate snapshot,
the exact drift surface the feature removes (FR-009).

## R5 — Profile footprint (Edge case: full-set BOM over-includes for slim profiles)

**Decision**: Publish a **single full-set BOM** and keep it **optional**; do **not** wire it into
the `fs-gg-ui` template. The template's per-package / `FsSkiaUiVersion` pinning remains the default
for minimal footprint.

**Rationale**: A full-set metapackage that transitively pulls all 16 members over-includes for a
headless/governed profile that needs neither viewer nor controls. The spec resolves this by making
the BOM **opt-in** (FR-007) and leaving the slim default in place; profile-scoped/slim BOMs are
explicitly Out of Scope. Migrating the template to consume the BOM is a deferred, optional follow-up.

**Alternatives considered**: per-profile slim BOMs (`FS.GG.UI.App`, `FS.GG.UI.Governed`, …) —
**rejected** (Out of Scope; one full-set BOM only for this feature).

## R6 — Verification harness (how US1/US2/US3 are proven with real evidence)

**Decision**: Mirror the repo's existing two-layer package-validation pattern
(`GeneratedConsumerValidationTests` + `Feature163PackageFeedValidationTests`):
- **Always-on gate** (deterministic, no pack/restore): the membership-parity test (R3) +
  assertions over a committed `specs/207-ui-bom-metapackage/readiness/bom-consumer-validation.md`
  report (tokens: `bom-version:`, `resolved-members-at-version:`, `forced-mismatch:`, `result: pass`).
- **Env-gated live proof** behind `FS_GG_RUN_BOM_CONSUMER_SMOKE=1`: pack the snapshot to a temp
  feed; restore a **clean** consumer whose only FS.GG.UI declaration is `FS.GG.UI@V`; assert every
  resolved `FS.GG.UI.*` is at V and it builds (US1/SC-001); restore **twice** from a clean cache and
  assert identical resolved set (US3/SC-004); then add a member at `Y≠V` and assert a real
  NU1605/NU1107 conflict (US2/SC-003). The regenerator writes the report the gate asserts.

**Rationale**: Honors the standing assumption (coherence unverified until a real consumer restores)
and the constitution's "prefer real evidence; disclose synthetic" — the conflict is reproduced
against a real feed, not asserted from a string. The env gate keeps CI cheap while the report keeps
the gate honest, exactly as the existing consumer-smoke tests do.

**Alternatives considered**: asserting pinning purely from the nuspec text (no restore) — **rejected**
as synthetic; a nuspec can be well-formed yet still resolve wrong (NuGet edge cases), which is
precisely the failure mode the feature exists to prevent.

## R7 — Cross-repo record timing (FR-008)

**Decision**: Record the BOM in the `fs-skia-ui-version` compatibility registry
(`FS-GG/.github`: `registry/dependencies.yml` + `docs/registry/compatibility.md`) via the
GitHub-native cross-repo coordination protocol (`gh` + the `cross-repo-coordination` skill),
**only after** US1 and US2 are verified against the packed snapshot.

**Rationale**: FR-008 forbids recording before the behavior holds (a registry that advertises an
unverified BOM just relocates the staleness problem). Cross-repo state is not a file in this repo;
it is mutated through the protocol in `FS-GG/.github`.

**Alternatives considered**: writing the registry early and verifying later — **rejected** (FR-008
explicitly: the record MUST NOT be made before the behavior holds).
