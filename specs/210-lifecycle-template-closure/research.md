# Phase 0 Research: Close the Lifecycle-Agnostic Template Epic

All NEEDS CLARIFICATION from Technical Context are resolved below.

## R1 — Validate the published package, not the working tree

**Decision**: The acceptance harness installs the published `FS.GG.UI.Template` `.nupkg` from the local feed
into an **isolated template store**, runs the `dotnet new` matrix from that installed package, then
uninstalls and restores the prior state. The working-tree template under `.template.config/` is never the
artifact under test.

**Rationale**: The epic's acceptance is "the **published** template emits Spec Kit only when asked." The
child evidence (204/206) was generated against the working tree via the source template; a later republish
that changed behavior would be masked by stale child reports (spec edge case "Drift between child evidence
and the published artifact"). Testing the installed package closes that gap and makes the close/don't-close
decision rest on what consumers actually pull.

**Mechanics**:
- Install pinned: `dotnet new install FS.GG.UI.Template::0.1.51-preview.1 --add-source ~/.local/share/nuget-local/`
  (isolate via a dedicated `--add-source` and a clean profile so the working-tree source template does not
  shadow it; verify `dotnet new list` reports the package version, not the working-tree source).
- Run matrix: for each `lifecycle ∈ {spec-kit, sdd, none}` × `profile ∈ {app, headless-scene, governed,
  sample-pack}`, instantiate into a temp dir and record the gated-file-set presence/absence.
- Uninstall/restore so the dev environment's working-tree template is reinstated.

**Alternatives considered**:
- *Reuse `validate-lifecycle-template.fsx` as-is* — rejected: it parses `.template.config/template.json` and
  scaffolds the working tree, which is exactly the artifact the epic acceptance must NOT trust.
- *Trust the child readiness reports* — rejected: violates the spec's core "single record against the
  published artifact" requirement (FR-001/FR-006) and the drift edge case.

## R2 — Which published version to pin (user-confirmed)

**Decision**: Pin and validate **`FS.GG.UI.Template.0.1.51-preview.1`** — the latest packed package in the
local feed, i.e. what a consumer pulls today. *(Confirmed by the user during planning.)*

**Rationale**: 0.1.51-preview.1 is the most recent consumer-facing artifact (packed by Feature 208's merge
bump). Pinning it makes the acceptance record reflect the live feed state rather than a superseded release.

**Caveat captured in the record (FR-006)**: There is **no dedicated template git tag** at 0.1.51 — only the
framework tag `fs-gg-ui/v0.1.51-preview.1` exists; the latest *template* tag is
`fs-gg-ui-template/v0.1.50-preview.1`. The acceptance record therefore:
- cites the **package version `0.1.51-preview.1`** as the authoritative pin,
- cites the **framework tag `fs-gg-ui/v0.1.51-preview.1`** as the nearest tag anchor,
- **flags the missing template tag** as a coordination note and proposes the follow-up
  `git tag fs-gg-ui-template/v0.1.51-preview.1` so future closure is tag-reproducible.

Reproduction does not depend on the missing tag: the harness reproduces from the **feed package version**
(`::0.1.51-preview.1 --add-source ~/.local/share/nuget-local/`), which is deterministic.

**Alternatives considered**:
- *0.1.50-preview.1 (tagged)* — more tag-defensible, but not the latest consumer artifact; rejected per user.
- *0.1.50 + prove 0.1.51 inert* — extra work the user did not select; rejected.

## R3 — Definition of "byte-identical" default output

**Decision**: The default value is `spec-kit`. "Byte-identical" means: for each of the four profiles, the
output of `lifecycle=spec-kit` (and the no-flag default, which must equal explicit `spec-kit`) is identical
to the **pre-lifecycle template baseline** captured by Features 204/206 — comparing **both the set of emitted
files (presence) and each file's content (bytes)**. The record restates the baseline (pre-lifecycle template
output per profile) so the artifact is self-contained, and states explicitly that both presence and content
are compared.

**Rationale**: Spec edge case "Defining byte-identical" requires the baseline, profile set, and
presence-vs-content scope all stated explicitly so the result is unambiguous and reproducible. 204's
`diff-vs-today=none` and 206's PV-3 blocking gate already use this presence+content semantics; 210 reuses it
against the published package.

**Alternatives considered**:
- *Presence-only diff* — rejected: would miss content drift (e.g. a substituted version string), defeating
  the purpose.

## R4 — Gated lifecycle file set (reused, not redefined)

**Decision**: Reuse Feature 204's gated set verbatim: every `source` entry whose target is under `.specify/`,
`.agents/`, or `.claude/`, plus the generated agent-context tree (`AGENTS.md`/`CLAUDE.md`) and the generated
constitution — all carry `condition: lifecycle == "spec-kit"` in `.template.config/template.json`. The three
ungated PRODUCT sources (base → `./`, samples → `samples/`, ant overlay) are present for all values.

**Rationale**: Spec Assumption: "this feature reuses that definition rather than redefining the gated set."
Redefining risks divergence from the implemented gate.

**Verification rule for `none` vs `sdd`**: both suppress the identical gated set; additionally `none` MUST
attach **no external-orchestrator expectation** (no SDD/scaffold hook, no governance placeholder). The record
asserts `none == sdd` on the gated set AND that `none` carries no orchestrator marker (FR-004).

**`profile` and `lifecycle` are orthogonal axes** (clarification): `profile` (`app`/`headless-scene`/
`governed`/`sample-pack`) selects the *product shape*; `lifecycle` (`spec-kit`/`sdd`/`none`) selects whether
the *governance surface* is emitted. They do not interact — every cell of the 3×4 matrix is a valid,
intended combination. In particular the `governed` **profile** (a product-shape choice) is independent of the
spec-kit governance **surface**: `governed` + `lifecycle=none` is a legitimate cell (a `governed`-shaped
product with no lifecycle surface attached), not a contradiction. The harness validates all 12 cells on this
basis.

## R5 — Acceptance-record provenance / fresh-checkout fallback

**Decision**: Mirror `validate-lifecycle-template.fsx`: the harness has (a) an always-on env-free
verdict/report core that can emit the record with synthesized live-only lines marked
`provenance: verdict-core` (Constitution V disclosure) so a fresh checkout with a gitignored `readiness/` is
not red-by-default, and (b) an env-gated live loop (`FS_GG_RUN_PUBLISHED_ACCEPTANCE=1`) that performs the real
install + matrix and writes `provenance: live`. The **close/don't-close conclusion is only valid from a
`provenance: live` run**; the record states this explicitly.

**Rationale**: Constitution V (real evidence preferred, synthetic disclosed loudly) and the repo's
established report-gate pattern (Features 128/204). Prevents a synthesized green from being mistaken for a
verified close.

## R6 — Consumer guidance placement and migration note

**Decision**: Extend the existing consumer guide `.template.package/README.md` (it already documents the
`--lifecycle` option). Add: (1) a decision tree mapping the three consumer scenarios (governed Spec Kit
product → `spec-kit`; SDD-composed app-only → `sdd`; bare standalone → `none`) to values; (2) per-value
include/exclude; (3) an explicit standalone-`none` statement that **no governance and no orchestrator are
attached or expected**; (4) a migration note from the pre-lifecycle template, including "select the default
(`spec-kit`) to reproduce prior output."

**Rationale**: Spec says the capability is shipped but undiscoverable; the README is the canonical consumer
surface already in the published package, so guidance ships with the artifact. Avoids creating a competing
doc location.

**Alternatives considered**:
- *New standalone doc under `docs/`* — rejected: splits consumer guidance from the package README consumers
  already read; higher drift risk.

## R7 — Cross-repo remainder: reuse, don't duplicate

**Decision**: For the SDD scaffold-path obligations, **reuse the existing open request `FS-GG/FS.GG.SDD#1`**
("Scaffold path must own git-init/chmod after fs-gg-ui Feature 205") — verify it is still open and reference
it from the closure record; do **not** file a new one. For the **constitution-ownership P0 decision for
`lifecycle=sdd`** (open per 204 spec), capture it as a tracked cross-repo/decision item — first check for an
existing issue; create one only if none exists. Update the `FS-GG` Projects v2 "Coordination" board so the P1
epic shows **Rendering-side complete** with the two remainder items attributed to their owning repos.

**Rationale**: Spec FR-010/FR-011 and edge case "Duplicate cross-repo asks" — an already-open request MUST be
reused. The `cross-repo-coordination` skill defines issues + the Coordination board as the channel; the
registry row for `fs-gg-ui-template` is already coherent from Feature 206, so 210 references it rather than
re-resolving it.

**Tooling**: `gh issue view FS-GG/FS.GG.SDD#1`, `gh issue list` (search for an existing constitution-ownership
item), `gh project` for the board. The closure record links each item by URL.

## R8 — "Buildable" verification: bounded build spot-check, not the full matrix

**Decision**: FR-003/FR-004 require the `sdd` and `none` products be **buildable**, not merely present. Verify
this with a **bounded build spot-check**: the harness runs `dotnet build` on the `app`-profile output for
`lifecycle=sdd` and `lifecycle=none` and asserts exit 0. The `spec-kit` default is **not** separately built —
its buildability follows from FR-005 byte-identity to the known-good pre-lifecycle baseline (a build would be
redundant). The other three profiles are covered by gated-set/byte-identity assertions; building one
representative profile per suppressed value is sufficient evidence that suppression does not break the product.

**Rationale**: "Present" (filesystem check) is weaker than the spec's "buildable" MUST; a suppressed-lifecycle
product could be missing a project reference or `.fsproj` the gated content supplied. Building every cell (12
builds) is disproportionate for a closure-proof; the 2-build spot-check honors the requirement at bounded cost.

**Environment fallback**: if the build toolchain/restore is unavailable, the buildability line is recorded
`environment-limited` (Constitution V/VI — disclosed, never a silent pass) and the close conclusion names the
unbuilt cell, so a reviewer sees the gap rather than an unverified green.

**Alternatives considered**:
- *Presence-only ("product present")* — rejected: does not verify the FR-003/FR-004 "buildable" MUST.
- *Build all 12 cells* — rejected: disproportionate; the per-value `app`-profile build is representative.
