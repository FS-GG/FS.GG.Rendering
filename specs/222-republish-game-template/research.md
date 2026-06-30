# Phase 0 Research: Republish the `game`-Profile-Bearing Template (Feature 222)

All NEEDS CLARIFICATION from Technical Context resolved below. This is a release-cadence + cross-repo
registry feature; "research" is confirming the existing machinery, the version/content gates, the
evidence model, the registry delta, and the publish-before-flip ordering — not selecting new tech.

## R1 — Target version (the monotonic-version constraint)

- **Decision**: Publish a coherent-set version strictly **> 0.1.53-preview.1**; the established next
  preview is **`0.1.54-preview.1`**. The plan does not hard-code the literal beyond the `>` constraint —
  the merge/release flow fixes the exact value (`speckit-merge` bump).
- **Rationale**: The org feed currently serves `0.1.53-preview.1` (tag `fs-gg-ui-template/v0.1.53-preview.1`,
  commit `55e5967` = Feature 218), confirmed lacking `b78e72a`. NuGet feeds are append-only; a stale
  re-tag would still lack `game` (spec edge case). FR-001 / SC-002 require exactly one new version `>`
  the current. The two in-repo pins are both at `0.1.53-preview.1` today and move together to `V`.
- **Alternatives considered**: Re-tagging `0.1.53-preview.1` (rejected — append-only feed, no `game`);
  jumping to `0.2.x` (allowed by "strictly greater" but breaks the established preview cadence and is
  unnecessary — Feature 220 is additive).

## R2 — Content gate (the released template MUST carry Feature 220)

- **Decision**: Gate the release on `git merge-base --is-ancestor b78e72a <release-tag>` being true
  AND the packed template actually exposing the `game` choice (content inspection, not just version
  string). Cut the release from a `main` commit that contains `b78e72a` (already on `main`).
- **Rationale**: A version string `> 0.1.53-preview.1` is necessary but not sufficient — a release cut
  from a `main` commit *before* `b78e72a` would re-ship a `game`-less template at a higher number
  (spec edge case). SC-002 demands content verification. Confirmed today: `b78e72a` is an ancestor of
  `main` (YES) but not of `fs-gg-ui-template/v0.1.53-preview.1` (NO).
- **Alternatives considered**: Trusting the version bump alone (rejected — does not detect a wrong
  base commit); inspecting only `template.json` source on disk (insufficient — must verify the *packed*
  artifact the feed serves carries `game`).

## R3 — Producer machinery (reuse, no new code)

- **Decision**: Reuse `release.yml`'s `publish-packages` job verbatim: it packs the whole coherent set
  (every `FS.GG.UI.*` package + the template at one version `V`) and pushes to `nuget.pkg.github.com/FS-GG`
  with `GITHUB_TOKEN` (`packages: write`). Triggered by a `v*` tag push (and `workflow_dispatch` with a
  `version:` input). `scripts/derive-template-version.sh` derives the released version from the
  `fs-gg-ui-template/v*` tag and feeds the Feature-216 reusable dispatch-sender that notifies Templates.
- **Rationale**: FR-010 forbids new product code / new workflow. The machinery is the same path Features
  204/218 used; it is canonical-repo-guarded (`if: github.repository == 'FS-GG/FS.GG.Rendering'`) and
  fail-loud (`set -euo pipefail`). No edit is required — only a correct tag-set push.
- **Alternatives considered**: Authoring a one-off publish workflow (rejected — FR-010, and duplicates
  audited machinery); manual `dotnet nuget push` (rejected — bypasses the pre-publish `package-tests` /
  `template-product-tests` gates and the coherent-set packing).

## R4 — Visibility (no action required)

- **Decision**: No package-visibility action. Org-readability of `FS.GG.UI.Template` was resolved in
  Feature 218 (`private → internal`/org-readable) and carries forward to the new version.
- **Rationale**: GitHub Packages visibility is per-package, not per-version; the resolved setting applies
  to all versions of `FS.GG.UI.Template`. SC-001 still asserts no exit 103 for the *new* version, but
  that is a live re-confirmation, not a new flip. Spec Assumptions record this. (Edge case "re-privatized"
  → would hit exit 103; the live probe catches it, but no proactive change is planned.)
- **Alternatives considered**: Re-running the visibility flip defensively (rejected — unnecessary churn;
  the live probe is the safety net).

## R5 — Evidence model (live cross-repo proof, no new unit tests)

- **Decision**: Evidence is live, consistent with Features 215/216/218:
  1. **Feed listing** — a `FS.GG.UI.Template` version `> 0.1.53-preview.1` is served.
  2. **Content** — `git merge-base --is-ancestor b78e72a <release-tag>` true AND the packed template
     carries the `game` choice.
  3. **Consumer install** — `dotnet new install FS.GG.UI.Template::<V>` from the org feed with an
     ordinary `packages: read` token → exit 0, no exit 103.
  4. **`game` scaffold** — scaffold selecting the `game` profile → accepted, minimal Pong-style MVU
     starter generated, no missing-profile / unknown-choice error.
  5. **Governance** — the generated `game` product builds and passes governance with **zero**
     `GovernanceTests` edits (preserving Feature 220's family-agnostic entrypoint acceptance).
  6. **Non-game parity** — `app` still scaffolds the controls showcase; `headless-scene`/`governed`/
     `sample-pack` output byte-identical to Feature 220's diff-verified baseline.
  7. **Registry** — entry + compatibility projection name `V` and read released for `game`.
- **Rationale**: Principle V (real evidence preferred). Deterministic local checks pass while the
  cross-repo path stays red (Features 175/216/218); the feed and a foreign token are the honest audience.
- **Alternatives considered**: New unit tests asserting the version string (rejected — they cannot
  observe the feed or a consumer token; would be synthetic for the contract this feature changes).

## R6 — Registry delta + publish-before-flip ordering

- **Decision**: After the feed is confirmed serving `V`, land a single `contract-change` PR on
  `FS-GG/.github`: in `registry/dependencies.yml` (`fs-gg-ui-template`) advance `version` /
  `package-version` / `package-tag` to `V`, flip the `game`-profile note **UNRELEASED → released**, flip
  the relevant `coherence` entry, and regenerate the `docs/registry/compatibility.md` projection. The
  flip MUST follow a confirmed feed listing (FR-007).
- **Rationale**: ADR-0001 makes the registry the source of contract truth; a `contract-change` item MUST
  update it as part of resolution (FR-006). Flipping before the feed serves `V` would make the registry
  claim "released" while the feed 404s (spec edge case). The registry PR (FS-GG/.github#77) already
  *recorded* `game` as UNRELEASED; this feature flips it.
- **Alternatives considered**: Flipping the registry first / in parallel (rejected — FR-007 ordering);
  hand-editing `compatibility.md` (rejected — it is a regenerated projection; regenerate it).

## R7 — Downstream notification & board closure

- **Decision**: Notify the downstream consumer SDD#44 (the `app → game` default-flip) of the published
  version via the existing dispatch-sender / a cross-repo comment (FR-009). Close #33 with the published
  version + registry PR link, move board item #33 to `Done`, and clear the `Blocked by: FS.GG.Rendering#33`
  mirror on item #31 (FR-008). The `app → game` default-selection flip itself is **out of scope** (owned
  by SDD#44).
- **Rationale**: The Coordination board is the source of order (cross-repo protocol); closing the producer
  item and clearing the mirror is what lets the consumer flip proceed. SC-005 makes all of this
  GitHub-verifiable. Per the `cross-repo-coordination` skill, this is filed/answered via issues + the
  Coordination board + an ADR reference where a decision is recorded.
- **Alternatives considered**: Doing the SDD default-flip here (rejected — Assumptions / spec scope put
  it on SDD#44); closing #33 before the registry PR lands (rejected — #33 is a `contract-change` item,
  resolution includes the registry update).

## Open questions

None. No NEEDS CLARIFICATION remain. The exact published literal (`0.1.54-preview.1` expected) is fixed
by the merge/release flow, not by this plan (R1).
