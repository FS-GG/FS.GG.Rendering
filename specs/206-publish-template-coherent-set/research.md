# Phase 0 Research: Publish Template & Tag the Coherent Set

All `NEEDS CLARIFICATION` from Technical Context are resolved below. Format per decision:
Decision / Rationale / Alternatives considered.

## R1 — Published template version number

**Decision**: `0.1.50-preview.1` for `FS.GG.UI.Template`.

**Rationale**: Must be strictly greater than the published `0.1.17-preview.1` (FR-002) and not yet on
the feed — confirmed: the local feed holds only `FS.GG.UI.Template.0.1.17-preview.1.nupkg`. Aligning
the template package number with the framework set it pins (`FS.GG.UI.* 0.1.50-preview.1`, the
coherent snapshot established by 204) makes "which template ↔ which framework" legible from the
version alone: a reader who sees `0.1.50-preview.1` on both the template and the framework packages
reads one coherent release. The template package is versioned independently, so adopting `0.1.50`
is a free choice, not a constraint violation.

**Alternatives considered**: `0.1.18-preview.1` (next in the template's own line) — rejected because
it leaves the template↔framework correspondence implicit and invites the "which 0.1.x goes with which
framework" question the coherent set exists to answer. A jump that skips numbers in the template line
is harmless for preview packages.

## R2 — Coherent-set tag name & namespace

**Decision**: Annotated tag `fs-gg-ui-template/v0.1.50-preview.1`.

**Rationale**: Spec Assumptions explicitly defer the namespace choice to planning. A **template-scoped**
namespace (`fs-gg-ui-template/...`) keeps the two coherence anchors distinct: `fs-skia-ui/v0.1.50-preview.1`
means "the framework `FS.GG.UI.*` set" (204), and `fs-gg-ui-template/v0.1.50-preview.1` means "the
template package + the framework set it scaffolds against" (this feature). The tag records the
published template version (FR-003), so its `v<semver>` segment is `v0.1.50-preview.1`, matching R1.
The tag is **annotated** (not lightweight), mirroring 204's
`-m "coherent FS.GG.UI.* snapshot for fs-gg-ui template pin"` convention.

**Alternatives considered**: Reuse the `fs-skia-ui/...` namespace — rejected: it conflates the
template snapshot with the framework snapshot and would need a disambiguating suffix on an already-
used version. A lightweight tag — rejected: the coherent set wants a durable message recording what
it binds. (Edge case FR-002 "tag already exists": `fs-gg-ui-template/v0.1.50-preview.1` does not yet
exist — `git tag --list` shows only `fs-skia-ui/v0.1.50-preview.1`. If it did, the publish must
surface the collision and pick a distinct name, never move the existing tag.)

## R3 — What "publish" means; the feed target

**Decision**: "Publish" = `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o
~/.local/share/nuget-local` after bumping `<Version>`, producing
`FS.GG.UI.Template.0.1.50-preview.1.nupkg` on the existing local/preview feed. No public NuGet.org
push.

**Rationale**: Spec Assumptions name "the project's existing local/preview package feed" as the
target, and the constitution fixes pack output at `~/.local/share/nuget-local/`. The template package
`Content Include="..\**\*"` packs the whole repo content (including `.template.config/template.json`
with `lifecycle` + `initGit`, and `template/base/` pinned at `FsSkiaUiVersion=0.1.50-preview.1`), so a
repack at the new version captures the 204+205 surfaces with no source edits beyond `<Version>`.

**Alternatives considered**: Push to a remote/public feed — out of scope; no remote feed is
configured and the spec scopes the target to the local feed. Hand-editing the `.nupkg` — rejected,
non-reproducible.

## R4 — Byte-identical default-output guarantee (FR-005)

**Decision**: Verify the `spec-kit` (default) lifecycle output **against the installed published
package**, profile-by-profile, using `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi
scripts/validate-lifecycle-template.fsx` plus a direct `dotnet new fs-gg-ui` byte-diff against the
prior published baseline. Block the publish (do not tag, do not reconcile) on any byte difference.

**Rationale**: 204/205 already guarantee `spec-kit` is byte-identical to the pre-lifecycle output;
this feature's job is to prove the *packaged* artifact preserves that, since working-tree green does
not prove the package is correct (the Standing Assumption). The existing validator already performs
per-profile byte-diff and unknown-value rejection.

**Alternatives considered**: Trust the working-tree test runs — rejected; they don't exercise the
install→instantiate path that consumers use.

## R5 — Side-effect-free verification against the package (FR-006)

**Decision**: Scaffold from the installed package with **no git flag** in a headless context and
assert: no `.git` directory created, no process spawned, prompt return. Cross-check
`Feature205TemplateSideEffectTests` invariants (GV-1..GV-6: `initGit` opt-in present, `skipGitInit`
absent, post-actions gated, no defensive flag) hold in the packaged `template.json`.

**Rationale**: FR-006/SC-003 require the 205 guarantee verified against the published package, not
only the tree. The test suite already encodes the manifest invariants; the live instantiate proves
runtime side-effect-freedom.

**Alternatives considered**: Manifest-only check — insufficient; SC-003 measures actual zero
processes/repos at instantiation time.

## R6 — Cross-repo reconciliation path & the dependent request

**Decision**: After publish + tag succeed and all profiles are green, (a) update the
`fs-gg-ui-template` row in `FS-GG/.github` `registry/dependencies.yml` to record the coherent release
at `0.1.50-preview.1` with the tag and a `tracking` link, and mirror it into
`docs/registry/compatibility.md`; (b) post a `## Response` on **`FS-GG/FS.GG.SDD#1`** citing the
published version and the `fs-gg-ui-template/v0.1.50-preview.1` tag (the package carrying the
side-effect-free + lifecycle surface is now installable, so SDD's scaffold-path work is unblocked);
(c) move the P1 Rendering board item to Done and clear the "blocked by lifecycle symbol" relation.
All three happen **only** after US1+US2 evidence is complete; any partial failure leaves the record
in-progress (FR-010).

**Rationale**: Mirrors 204's US3 sequencing (`gh` confirmed authenticated as `EHotwagner`,
`FS-GG/.github` resolvable). The dependent request `FS-GG/FS.GG.SDD#1` ("Scaffold path must own
git-init/chmod after fs-gg-ui Feature 205") is the open ask whose precondition is a shippable
package; the publish + tag is exactly what it was waiting on. Coordination protocol forbids editing
another repo's files directly — registry/projection changes go through the `.github` repo, the
request through `gh issue comment`.

**Alternatives considered**: Reconcile before tagging — rejected; the record would assert coherence
the artifacts don't yet back. Closing `FS-GG/.github#1` (the 204 framework request) — already CLOSED
by 204; not this feature's subject. Treating SDD#1 as "adopt the lifecycle symbol" only — the issue
body is broader (side-effect-free scaffold ownership), so the response cites both the lifecycle symbol
and the side-effect-free surface the published package carries.
