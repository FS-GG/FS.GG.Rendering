# Research — Feature 231: product skill-manifest + single standalone materialize (ADR-0014 P2)

All decisions below were made against verified repo/upstream state on 2026-07-02:
`FS.GG.Contracts` 1.4.0 live on nuget.org + org feed (dll-only package, no source content
files); `Fsgg.SkillMirror` (`providerSourceRoot=".agents"`, `sha256`, `skillPath`,
`skillIdOfPath`, `mirrorTargetRoots`, `retargetSkillPath`, `mirror`, `verify`) and
`Fsgg.Schemas` (`SkillScope`, `SkillManifestEntry`, `SkillManifest`, `skillManifestVersion=1`,
`agentSkillRoots=[".claude";".codex";".agents"]`) published; SDD's orchestrated fan-out
(`HandlersScaffold.fs`) mirrors **every file** under `.agents/skills/` via
`retargetSkillPath`, and records per-skill sha256 in provenance.

## R1 — Materialize mechanism: MSBuild target in the product (not a template post-action)

**Decision**: One spec-kit-gated **product build step**: a target in
`template/base/Directory.Build.props` (`FsGgMaterializeSkillRoots`, `BeforeTargets="Build"`,
incremental via a stamp file) that runs `dotnet fsi .specify/scripts/fs-gg/materialize-skill-roots.fsx`
when that script exists in the product. The script (and the product's manifest-driven verify)
is emitted **only** under `lifecycle == "spec-kit"`, so the target self-gates by file
existence: under `sdd`/`none` the script is absent and the target never fires (FR-004).

**Rationale**:
- A run-script post-action is ruled out by Feature 205's hard-won property: default
  generation is **side-effect-free** — the old auto-run post-action produced the spinning
  `--allow-scripts` prompt defect the lifecycle validator still documents
  (`scripts/validate-lifecycle-template.fsx` lines 262-266). Reintroducing an auto-run
  post-action regresses that; an opt-in one (like `--initGit`) would leave the default
  scaffold violating ADR-0011 §1 permanently.
- MSBuild-level placement covers **every** build entry: stock `dotnet build` at the product
  root (Feature 212 keeps it first-class) *and* the FAKE path (`./build.sh` → `build.fsx` →
  `dotnet build`), so the Templates composition gate (P3.T3.2), which today uses plain
  `dotnet build`, materializes for free in the standalone lane.
- `dotnet fsi` ships with the .NET SDK; the script is pure BCL (`System.Text.Json`,
  `System.IO`, `SHA256`) — no restore, no network, cross-platform.
- Idempotent + incremental: writes are skip-if-byte-identical; the target declares
  `Inputs` (manifest + `.agents/skills/**`) / `Outputs` (stamp), so multi-project products
  don't re-run fsi per project after the first materialize.

**Consequences / accepted tradeoff**: a freshly scaffolded spec-kit product has the union in
`.agents/skills/` (plus the base `.claude/` tree's `fs-gg-project`) but populates
`.claude/skills/` + `.codex/skills/` fully at the **first build**. Every documented product
entry (`README`, quickstart, agent-context docs) already begins with restore/build. ADR-0014
Decision 2 explicitly allows "one template post-action / **build target**". Recorded in the
scaffold docs; the P3 gate asserts post-build state.

**Alternatives considered**:
- *Template post-action (run-script)*: rejected — Feature 205 regression (above).
- *FAKE-only hook in `build.fsx`*: rejected — misses stock `dotnet build` (the composition
  gate's path), violating "one mechanism covers both entries".
- *Inline MSBuild C# task (RoslynCodeTaskFactory)*: rejected — ports the algorithm to a second
  language, gutting the content-parity guarantee the roadmap §6 demands.
- *Keeping generation-time emission via twins*: the F1 defect this feature deletes.

## R2 — Vendored algorithm form and content-parity gate

**Decision**: Vendor the algorithm as **one pure F# file**,
`template/lifecycle/skill-mirror-vendored.fs` (module `FsGg.Vendored.SkillMirror` — a
transliteration of `Fsgg.SkillMirror` with only the namespace/module header changed), plus a
thin IO driver `template/lifecycle/materialize-skill-roots.fsx` that `#load`s it. The template
emits both to `.specify/scripts/fs-gg/` (spec-kit-gated, `copyOnly`). The parity gate lives in
`tests/Package.Tests` (the release gate project): it `<Compile Include>`s the vendored `.fs`
**and** takes a `PackageReference` to `FS.GG.Contracts` (centrally pinned `1.4.0`), then
asserts **behavioral equality** of `sha256`/`skillPath`/`skillIdOfPath`/`mirrorTargetRoots`/
`retargetSkillPath`/`mirror`/`verify` over representative + adversarial inputs (empty union,
multi-root, missing copies, divergent bodies, hash mismatches, `\\` paths, non-skill paths),
including the vendored root-set constant equalling `Fsgg.Schemas.agentSkillRoots`.

**Rationale**: the 1.4.0 nupkg ships only `lib/net10.0/FS.GG.Contracts.dll` (verified) — there
is no shipped source to byte-compare, so behavioral parity over the full exported surface is
the strongest checkable form of "the vendored copy equals the library". Keeping the vendored
copy in F# with a near-verbatim body makes drift-by-edit visible in review and keeps the two
lanes one algorithm in fact.

**Alternatives considered**: byte-comparing against the SDD repo (network dependency in a
gate — rejected); embedding the algorithm in the fsx driver only (untestable from Package.Tests
without fsi-in-test — rejected); shipping no parity gate (violates roadmap §6 — rejected).

## R3 — Manifest placement, shape, and freshness

**Decision**: One canonical, checked-in manifest `template/skill-manifest/skill-manifest.json`
conforming to `skill-manifest` schema v1: `schemaVersion: 1` and the **full product-scope
catalog** — 12 entries (`fs-gg-scene`, `fs-gg-symbology`, `fs-gg-skiaviewer`, `fs-gg-elmish`,
`fs-gg-keyboard-input`, `fs-gg-ui-widgets`, `fs-gg-styling`, `fs-gg-layout`, `fs-gg-testing`,
`fs-gg-samples`, `fs-gg-feedback-capture`, `fs-gg-project`), each
`{id, scope: "product", sha256: <digest of canonical SKILL.md bytes>, resolvablePath:
".agents/skills/<id>/SKILL.md"}` (no inline `body` — one canonical body on disk, per
ADR-0014 §1). It is emitted to **`.agents/skills/skill-manifest.json` in every lifecycle**
(new ungated `template.json` source, `copyOnly`).

- **Union resolution**: the manifest is the catalog; the **concrete scaffold's union** is
  "manifest entries whose `.agents/skills/<id>/SKILL.md` was emitted" (profile/feedback
  conditionality stays in `template.json`, where it already lives) **plus** present
  non-manifest skills (the `speckit-*` process set and any co-tenant), which verify with an
  empty reference digest (`ExpectedSkill.Sha256 = ""` — presence + cross-root identity only,
  exactly the library's declared semantics).
- **Placement rationale**: inside `.agents/skills/` the file is provider-owned in every lane
  (Feature 229 confinement; no `isSddTree` exposure), and both mirror authorities — SDD's
  fan-out (which copies *every* file under `.agents/skills/`, verified in
  `HandlersScaffold.fs`) and our standalone materialize — propagate it to all roots, giving
  P3's composition gate the expected-union contract in **both lanes** for free.
- **Freshness**: a Package.Tests gate recomputes each catalog digest from the canonical
  source (`template/product-skills/<id>/SKILL.md`, `template/fragments/samples/skill/`,
  `template/feedback/skill/`, `template/base/.agents/skills/fs-gg-project/`) and fails on any
  mismatch or on a catalog↔`template.json`-sources divergence, so the manifest cannot go
  stale. A helper script `scripts/generate-skill-manifest.fsx` regenerates it.

**Alternatives considered**: per-scaffold manifest reflecting the concrete selection (needs
10+ conditional manifest variants or post-processing — rejected); manifest at `.agents/`
root (outside the provider-owned subtree Feature 229 confines writes to — rejected);
repo/registry-only publication (P3 gates then lack an in-product contract — rejected).

## R4 — Digest stability requires verbatim, name-neutral skill bodies (subsumes R2.3/F5)

**Decision**: All product-skill emissions become **`copyOnly`** (verbatim bytes), and the
canonical bodies are made **name-neutral**: the two path-bearing capitalized-`Product` token
sites (`template/product-skills/fs-gg-testing/SKILL.md` — `src/Product/Product.fsproj`;
`template/product-skills/fs-gg-layout/SKILL.md` — `Product.LayoutEvidence`,
`Product/Program.fs`) are rephrased to name-agnostic forms (e.g. `src/<Name>/<Name>.fsproj`,
"the starter's `LayoutEvidence` module in your product's `Program.fs`"). The
`effectiveNameLower` symbol itself is left untouched.

**Rationale**:
- Content-addressing forces this: a manifest `sha256` can only hold if the materialized body
  is byte-stable across scaffolds, so skill bodies cannot undergo name substitution at all —
  which *simultaneously* fixes F5 for skill prose (the R2.3 requirement: 126
  lowercase-`product` occurrences in the canonical product-skill bodies are being rewritten
  today) and the capitalized-token dangling references the R2.4 guard would otherwise flag.
- Scoping the fix to skill emission keeps the blast radius zero for every other file:
  `effectiveNameLower` exists solely for legacy `sourceName` byte-parity (Feature 217
  data-model, verified), and non-skill prose corruption (e.g. `build.fsx` "generated
  product" messages, `load-product.fsx`'s self-reference) is a pre-existing, out-of-scope
  defect — recorded below as a bounded follow-up, not silently expanded into this feature.

**Follow-up (bounded, not in scope)**: the general F5 class outside skills (e.g.
`load-product.fsx` line 3 advertising a filename that the scaffold does not actually rename)
deserves its own feature; candidate: replace prose-word substitution with a delimited token.

## R5 — What ships where (the product/dev boundary, R2.1)

**Decision** (spec-kit lane, `.agents/skills/` at generation):
- `speckit-*` process skills (16): the blanket repo-root source `.agents/skills/` →
  `.agents/skills/` is **narrowed with `"include": ["speckit-*/**"]`** (stays `copyOnly`).
- `fs-gg-project`: from `template/base/.agents/` as today — **no longer overwritten** by the
  repo-root wrapper (the overwrite is what ships the dangling
  `../../../template/base/...`-routing body today).
- Profile-gated canonical product skills: the existing 9 `template/product-skills/<id>` →
  `.agents/skills/<id>` rows, now `copyOnly`.
- `fs-gg-samples` / `fs-gg-feedback-capture`: existing conditional rows, `.agents` target
  only, `copyOnly`.
- **Deleted from product output**: the whole repo dev surface — 17 `fs-gg-*` wrapper dirs
  (9 `fs-gg-product-*` aliases + 8 framework/dev wrappers incl. `fs-gg-ant-design`,
  `fs-gg-design-system`, `fs-gg-diagnostics`, `fs-gg-generated-controls-guidance` and the
  root wrappers that shadow canonical bodies), achieved by the `include` narrowing — plus all
  24 per-skill `.claude`/`.codex` twins and the 2 blanket `.claude`/`.codex` copies (R2.2).
  Net `template.json` source count around the skill surface: 38 rows → 14.
- `sdd`/`none`: unchanged placement — product skills to `.agents/skills/` only, plus the new
  ungated manifest row (a provider-owned data file SDD's fan-out mirrors like any co-tenant
  file).

**Repo surface intact (FR-010)**: nothing under the repo's own `.agents/skills/` changes;
only which subset the template vendors.

## R6 — Verify semantics and staged enforcement

**Decision**: the fsx driver, after mirroring, runs the vendored `verify` over
`agentSkillRoots` with expected = (present ∩ manifest → digest-checked) ∪ (present ∖ manifest
→ empty digest) and prints per-skill drift diagnostics. In-product it is **advisory**
(build does not fail on drift; roadmap P4 flips enforcement); with `--enforce` it exits
non-zero on drift — the flag the repo's release gates use. The repo gates
(`validate-lifecycle-template.fsx` live loop + Package.Tests) run **enforcing**: live
spec-kit scaffolds must show `three-root-mirror=ok`, digest-match, **zero dangling routes**,
and `sdd`/`none` scaffolds must show `claude-product-skills=0 codex-product-skills=0`.
Extra skill-directory files (today: `fs-gg-symbology/reference.fsx`) are mirrored by the
same all-files fan-out (matching SDD's) and covered by the gates' full-directory byte
comparison; manifest digests cover `SKILL.md` bodies (the schema's granularity).

## R7 — No-dangling-route guard (R2.4)

**Decision**: a Package.Tests gate + live-loop check: for every skill body the template can
emit (canonical sources + the `speckit-*` set), extract path-like references (relative
`../`-escapes always flagged; token extraction for `docs/**`, `src/**`, `.specify/**`,
`scripts/**`, `readiness/**`-shaped references; placeholders containing `<`/`*` skipped) and
resolve each against the scaffold tree(s) in which that skill ships (profile × lifecycle
aware). Any unresolvable reference fails the gate. The env-free core resolves against the
template's declared emission set; the env-gated live loop resolves against real scaffolds.
Known-good baseline: `fs-gg-testing` references `docs/effects-boundary.md` — present in
`template/base/docs/` (verified).

## R8 — Gate/test rework map

- `Feature219EmitFrameworkSkillsTests.fs` (G-EMIT): re-derive to the R5 emission table (9
  product rows `.agents`-only + `copyOnly`; `include`-narrowed speckit row; manifest row;
  materialize-script row; zero `.claude`/`.codex` skill targets).
- `Feature204LifecycleTemplateTests.fs`: gated-source audit drops the `.claude`/`.codex`
  twin expectations (Feature 230's GV floors/`three-root-mirror` move to post-materialize
  semantics); `sdd`/`none` invariants unchanged.
- `scripts/validate-lifecycle-template.fsx`: verdict core re-derives the new source
  classifier; live loop runs the materialize script (`dotnet fsi … --enforce`) on spec-kit
  scaffolds before asserting root equality; report lines updated
  (`spec-kit/<p>: three-root-mirror=ok (materialized)`, `manifest-digests=ok`,
  `dangling-routes=0`).
- `Feature217ProductNameTemplateTests.fs` / `Feature224SkillCatalogCurrencyTests.fs` /
  `Feature225ProductSkillVocabularyTests.fs`: audit for assertions over the old emission
  shape (substituted skill bodies, twin sources, wrapper vendoring) and re-derive where they
  encode superseded Feature 230 structure.
- New `Feature231SkillManifestTests.fs`: manifest schema/digest freshness (R3), vendored
  parity (R2), no-dangling guard core (R7), Directory.Build.props target presence/shape (R1).

## R9 — Version/release

Template-only change (no `src/**`): bump `.template.package/FS.GG.UI.Template.fsproj`
`0.1.60-preview.1 → 0.1.61-preview.1`; `FsGgUiVersion` stays `0.1.58-preview.1` (matches the
Feature 230 precedent). Release via the repo's existing tag flow after merge; registry flip +
Templates re-pin are P3/P4 (out of scope, per spec).

## New dependency justification (Constitution: minimized dependencies)

`FS.GG.Contracts` `1.4.0` — **test-only** (`tests/Package.Tests`), the published contract
authority the parity gate compares against; pinned exactly (central package management);
owner: this feature's gates; available on nuget.org (no feed auth needed in CI).
