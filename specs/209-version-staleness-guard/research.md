# Phase 0 Research — Version-Staleness Guard

All "NEEDS CLARIFICATION" from Technical Context are resolved below. Each decision records what was
chosen, why, and the alternatives rejected. Inputs were established by reading the existing machinery
(Features 064/204/206/207/208) and the spec's Assumptions, which pre-resolve several edge cases.

---

## D1 — What is the authority for "the latest published coherent set"?

**Decision**: The `fs-gg-ui/v<V>` **git tag namespace** is the authority, exactly as the spec
Assumptions state. The verdict reads pushed tags (`git tag --list 'fs-gg-ui/v*'`), parses the `<V>`
suffix, and treats the **highest by preview-aware version order** as the latest published coherent
set. The local feed `~/.local/share/nuget-local/` is the dev-side materialization of those tags and
is **reproduced in CI by pack-from-source** (it is not available on a GitHub runner).

**Rationale**: Tags are immutable, push to the remote (so CI can see them), and were chosen in
204/206/208 precisely as the snapshot marker. Re-deriving "coherence" from the feed in CI is
impossible (feed is local); re-deriving it from scratch is what 204 already did manually. Comparing
the pin to the tag namespace is the cheap, reproducible signal.

**Alternatives rejected**:
- *Local feed as authority* — not present in CI; non-reproducible.
- *Repo-root `<Version>`* — decoupled by design (see D5).
- *NuGet.org / a remote feed* — these packages publish to a local feed only; no remote source of truth.

---

## D2 — How does the guard avoid blocking the very PR that cuts a new release?

**Decision**: The guard verifies a **coherent state**, and a version bump is **one atomic operation**
(US2 / FR-005 / FR-009): the bump edits the single literal *and* produces the matching `fs-gg-ui/v<V>`
tag + packs the feed together (the merge/release machinery — `speckit-merge` flow, evidenced by the
208 merge commit "record merge + template package-bump"). Therefore:

- **Ordinary feature PRs do not change `FsGgUiVersion`.** For them the invariant is simply
  `FsGgUiVersion == latest fs-gg-ui/v<V> tag` — trivially true, and a *lag* (framework/tag advanced,
  pin left behind — the 204 case) makes it false → red.
- **A release that bumps** presents the post-bump state (new literal + new tag) to the verdict; pin
  again equals the latest tag. A **half-bump** (literal edited by hand without the tag/feed, or a
  derived location left behind) is exactly what goes red.

So "pin must correspond to an existing tag and not lag the latest" (FR-002) is correct for *all*
PRs without special-casing the release PR: the release tooling makes the tag exist as part of the
same coherent operation it is validating.

**Rationale**: This is the spec's own "a version bump is one coherent operation; a partial bump
cannot ship" (US2). Decoupling the bump from the tag is the half-bump failure mode the guard exists
to catch — so the guard *should* be strict, and the tooling *should* make bump+tag atomic.

**Operational note for CI**: `actions/checkout@v4` with default `fetch-depth: 1` does **not** fetch
tags. The gate step MUST set `fetch-depth: 0` (or `fetch-tags: true`) so `git tag` sees `fs-gg-ui/v*`.
This is a one-line workflow change captured in the contract.

**Alternatives rejected**:
- *Allow pin ahead of tag (pre-tag a release on a branch)* — violates FR-009 (pinned-but-unpublished
  rejected) and re-opens the half-bump window. Rejected.
- *Warn-only on lag* — violates FR-006 (must block merge). Rejected.

---

## D3 — Is the guard a verifier or must it also propagate the bump across locations?

**Decision**: **Verify-centric.** Derivation is already structural in the repo: the 11 template pins
use `$(FsGgUiVersion)`; `build.fsx` reads the same literal by regex; the BOM nuspec uses a single
`[$version$]` token resolved from `-p:Version=V` at pack time. So there is effectively **one literal**
already (`<FsGgUiVersion>`) plus the snapshot tag. FR-005's "propagated **or** verified together" is
satisfied by *verification* because there is no second literal to propagate to. The guard's job is to
prove the derivation has not been broken (a pin hardcoded instead of `$(FsGgUiVersion)`; a new member
unwired; the BOM token altered; the tag missing/lagging).

**Rationale**: Adding a propagation script would duplicate machinery that MSBuild + nuspec token
substitution already perform, and would create a *second* way to set the version — the exact
multiple-source-of-truth hazard this epic eliminates. The early live verification (plan Standing
Assumption) confirms the single-literal hypothesis before the guard is built; if a stray hardcoded
pin is found, the fix is to re-route it through `$(FsGgUiVersion)`, not to add propagation.

**Alternatives rejected**:
- *A `bump.fsx` that rewrites N locations* — reintroduces multiple write sites; rejected unless the
  early verification disproves single-literal derivation.

---

## D4 — Where does the guard run, and how is the FR-008 "real restore" grounded without slowing the gate?

**Decision**: **Two layers, split by cadence**, mirroring `validate-bom-consumer.fsx`:

1. **Structural verdict-core — GATE, every PR, always, fast (text + git only).** Catches every drift
   scenario in the spec: stale/lagging pin (204; SC-001), one-BOM-pin or one-member half-bump
   (SC-002), unwired new member (SC-004), phantom version with no tag, BOM token/bracket drift —
   the last **independently of warnings-as-errors** (FR-004) because it compares pins directly rather
   than relying on NU1605/NU1608 loudness. This is the merge-blocking authority (FR-006).
2. **Restore-grounded proof — GATE, scoped (one pack + one clean restore).** Packs framework + BOM
   from source to a throwaway feed at the pinned `V` and restores `FS.GG.UI@V` in a clean consumer,
   asserting the **complete** `FS.GG.UI.*` member set resolves to exactly `V`. This grounds FR-008's
   "the pin resolves to the complete real coherent set" and makes a text-grep insufficient on its
   own — for an affordable gate cost (comparable to the existing Debug build step). Reuses the
   existing `validate-bom-consumer.fsx` clean-consumer layer.
3. **Full generate→restore→build of a product from the template — RELEASE lane (`release.yml`).**
   The deeper, all-profiles verification already present via Package.Tests / product-from-template
   remains the maximal FR-008 grounding; the guard does not duplicate it in the gate.

**Rationale**: The repo deliberately keeps Package.Tests off the gate to stay "fast, deterministic"
(`docs/ci/cadence-map.md`). The *structural* verdict is what actually detects all spec drift classes
and costs milliseconds, so it belongs in the gate unconditionally. The *scoped restore* is the
minimum real-restore that proves the structural facts correspond to a restorable graph (anti-grep,
FR-008) without a full product build. The *full product build* stays on release where it already
lives. Together: drift is caught "within a single CI run, before any product is scaffolded by a
consumer" (SC-001) and the guard is the "sole authority on coherence" (SC-003).

**Alternatives rejected**:
- *Structural-only in the gate* — fails FR-008 (a green text-grep would be the sole evidence).
- *Full product generate→restore→build in the gate* — adds minutes to every PR against the repo's
  explicit gate-speed value; the scoped restore already grounds the claim. Kept on release instead.
- *Restore proof release-only, structural-only on PRs* — the scoped restore is cheap enough to keep
  on the gate, so a stale pin can never even merge with a green structural check that doesn't restore.

---

## D5 — Does the repo-root `<Version>` (`0.1.0-preview.1`) participate in the lockstep set?

**Decision**: **No — decoupled by default**, per the spec Assumption "Repo-root `<Version>` is
decoupled by default." The verdict does **not** compare `Directory.Build.props` `<Version>` against
`FsGgUiVersion` or the tags. It is repo metadata (authors/repository/license), and publishing bumps
the feed/tag independently via the merge/repack flow.

**Rationale**: Coupling the two would force the repo-root metadata version to track every coherent
package bump for no contract benefit, and the spec explicitly scopes it out unless proven wrong.

**Reversal trigger (documented)**: if a consumer or build ever resolves the coherent version from the
repo-root `<Version>`, that assumption is falsified and `<Version>` joins the lockstep set under
FR-005 — a one-line addition to the verdict. Captured in data-model.md as an explicitly excluded
input so the decision is visible, not silent.

---

## D6 — Member-set parity: how is the published-16 vs consumed-11 distinction handled?

**Decision**: The verdict computes three sets and checks the right relations:
- **Published members** `P` = packable `FS.GG.UI.*` `.fsproj` under `src/**` (IsPackable=true) — 16.
- **BOM dependencies** `B` = `<dependency id>` set in `src/Meta/FS.GG.UI.nuspec` — must equal `P`
  (full-set parity; reuses `validate-bom-consumer.fsx`'s exact check).
- **Template consumed pins** `T` = `FS.GG.UI.*` `PackageVersion` entries in
  `template/base/Directory.Packages.props` — 11, a documented **subset** of `P` (the profile-gated
  packages a generated product consumes). The verdict checks `T ⊆ P`, that every member of `T`
  resolves its version through `$(FsGgUiVersion)` (no hardcoded literal), and that `T` matches the
  expected consumed-set manifest so an *intended* member isn't silently dropped.

A **new member added to `src/**`** changes `P`; if it is not added to `B` the `B == P` check fails
(SC-004); if it should also be consumed but is missing from `T`, the consumed-set manifest check
fails — both name the unwired member (FR-007).

**Rationale**: The 16/11 split is real and intentional (BOM covers the full set; the template
consumes only the product-facing profiles). Treating `T` as a *subset with an expected manifest*
rather than requiring `T == P` avoids false positives on the meta/diagnostics/symbology packages a
product doesn't pin, while still catching a dropped or extra consumed pin.

**Alternatives rejected**:
- *Require `T == P`* — would red on the legitimate 16-vs-11 gap. Rejected.
- *Skip `T` entirely* — would miss a consumed pin hardcoded or dropped (a half-bump class). Rejected.

---

## D7 — Preview-aware version comparison

**Decision**: Compare versions by **SemVer-with-prerelease ordering**, not string compare:
`0.1.9-preview.1 < 0.1.10-preview.1` and `…-preview.1 < …-preview.2`. Implement a small local
comparator (parse `major.minor.patch` numerically, then compare dotted prerelease identifiers
numerically/lexically per SemVer §11) to avoid taking a `NuGet.Versioning` package reference into a
`dotnet fsi` script; if the script already has access to the NuGet assemblies via the SDK, prefer
`NuGet.Versioning.NuGetVersion` and skip the hand-rolled comparator.

**Rationale**: The spec Edge Cases call this out explicitly; string compare would mis-order
`0.1.10` vs `0.1.9` and miss a genuine lag. Keep the comparator tiny and unit-tested with the exact
edge pairs from the spec.

---

## D8 — Cross-repo contract upkeep (FR-010)

**Decision**: Record how the in-repo structural guard upholds the `fs-gg-ui-version` (and
`fs-gg-ui-bom`) registry contract via the **`cross-repo-coordination`** protocol: a short note/ADR in
`FS-GG/.github` stating that drift is now caught by this repo's gate before merge, so the registry's
`coherent` row is enforced structurally rather than by manual reconciliation. No registry *schema*
change; the contract is upheld, not modified (Tier 2). Executed as a task in the implementation
phase, after the guard is verified in-repo (same ordering 208 used: in-repo verification → cross-repo
flip).

**Rationale**: The skill is the canonical channel for cross-repo contract notes; doing it last keeps
the registry claim honest (only assert structural enforcement once it actually works).

---

## Summary of resolved unknowns

| # | Question | Resolution |
|---|----------|-----------|
| D1 | Authority for "latest coherent set" | `fs-gg-ui/v<V>` git tags (feed reproduced by pack-from-source in CI) |
| D2 | Don't block the release PR | Bump is atomic (literal + tag + feed together); verdict checks post-bump state; CI must fetch tags |
| D3 | Propagate vs verify | Verify-centric — derivation already collapses to one literal |
| D4 | Where it runs / FR-008 grounding | Structural verdict + scoped restore in the **gate**; full product build stays on **release** |
| D5 | Repo-root `<Version>` in lockstep? | No — decoupled by default; documented reversal trigger |
| D6 | 16 published vs 11 consumed | `B == P` (full-set), `T ⊆ P` against an expected consumed manifest |
| D7 | Version ordering | Preview-aware SemVer comparison, not string compare |
| D8 | Cross-repo contract | Uphold `fs-gg-ui-version`/`-bom` via `cross-repo-coordination`; in-repo verify first |
