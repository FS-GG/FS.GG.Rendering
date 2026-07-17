# CI cadence map (Stage R6)

> **Migration Stage R6 deliverable — the auditable cadence → trigger mapping.**
>
> This document is a *derivation*, not a new decision. It places each already-decided check at
> exactly one CI cadence and is the single place the no-overlap invariant (FR-009) is audited.
>
> **Sources of truth (this file derives from them; it does not re-decide them):**
> - [`docs/validation/validation-set.md`](../validation/validation-set.md) — the R3 frequency
>   partition (local / CI / release-only) for validation-set members.
> - [`docs/validation/harness.md`](../validation/harness.md) — the R5 harness tiers (T0–T-uinput),
>   which are infrastructure, **not** validation-set members.
> - [`specs/005-ci-cadence-wiring/contracts/cadence-matrix.md`](../../specs/005-ci-cadence-wiring/contracts/cadence-matrix.md)
>   — the contract this map realizes (cadence → trigger → checks, one row per member).
> - [`specs/005-ci-cadence-wiring/contracts/gate-contract.md`](../../specs/005-ci-cadence-wiring/contracts/gate-contract.md)
>   — what the required gate runs, what reds it, what can never red it.
> - [`specs/005-ci-cadence-wiring/contracts/run-summary.schema.md`](../../specs/005-ci-cadence-wiring/contracts/run-summary.schema.md)
>   — the per-run proof-scope disclosure each run emits.

<!-- The sections below are filled by Stage R6 tasks:
     - SDK pinning decision (T005)
     - Member → cadence map + audit invariants (T018)
     - Cadence audit result (T019)
     - Surface-baseline drift / first-run behavior (T009)
     - Branch-protection maintainer step (T022)
     - Quickstart V1–V7 outcomes incl. measured gate wall-clock (T023)
     - Evidence-summary glue decision (T024) -->

## 1. Toolchain / SDK pinning

**Decision:** every workflow pins the SDK with `actions/setup-dotnet@v4` (`dotnet-version: 10.0.x`)
rather than relying on whatever `dotnet` the hosted image preinstalls.

**Why:** the repo has **no `global.json`** (checked at R6), so there is no in-repo SDK floor to key
off, and `net10.0` is recent enough that a given `ubuntu-latest` image may or may not carry it.
Pinning makes `dotnet build`/`dotnet test`/`dotnet run` deterministic across runner-image refreshes
and across the gate/release/capability workflows, which all use the identical setup step. If a
`global.json` is later added, switch `setup-dotnet` to read it (`global-json-file: global.json`) so
there is a single SDK source of truth.

All three workflows therefore begin with `actions/checkout@v4` → `actions/setup-dotnet@v4` before any
build/test/harness step.

## 2. Member → cadence map

Three cadences, one workflow file each. Only `gate` is *intended* to be required; branch protection is
not enabled today (§5), so today it informs a merge rather than blocking one.

| Cadence | Trigger | Workflow | Required | Runner | Fork PRs |
|---|---|---|---|---|---|
| **gate** | `push` + `pull_request` → `main` | `.github/workflows/gate.yml` | **yes** | hosted headless | run (no secrets) |
| **release** | `release: published` / `v*` tag (+ `workflow_dispatch`) | `.github/workflows/release.yml` | no | hosted headless | restricted to `FS-GG/FS.GG.Rendering` |
| **capability** | `schedule` (weekly) (+ `workflow_dispatch`) | `.github/workflows/capability.yml` | no | capable (TODO: provision) | restricted |

Every validation-set member and every harness tier maps to **exactly one** cadence. R3 frequency
labels are quoted from [`validation-set.md`](../validation/validation-set.md); harness tiers carry the
R5 source label `infra (R5)` and are not validation-set members.

> **Feature 235 — the gate's test list is derived, and the coverage is now machine-enforced.** The
> gate's deterministic tier iterates every `tests/*.Tests` project in `FS.GG.Rendering.slnx` (skipping
> the GL-capability `GL_TEST_PROJECTS`), so a new test project is in a cadence by construction rather
> than by remembering to edit a hardcoded list. `tests/Build.Tests/CadenceCoverageTests.fs` asserts
> `deterministic ∪ GL == the slnx test set` and that this table (and `validation-set.md`) name every
> slnx test project and no retired one — closing the "test project runs in no cadence" class (#47) and
> the map's earlier drift in both directions permanently.

| Member | R3 / source label | Cadence | Capability | Headless-runner behavior | Wired at |
|---|---|---|---|---|---|
| `Build.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Canvas.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Controls.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Diagnostics.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Elmish.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `KeyboardInput.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Layout.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Lib.Tests` (runtime subset) | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Rendering.Harness.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Scene.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Symbology.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Symbology.Render.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `SymbologyBoard.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `Testing.Tests` | local | gate | none | runs | gate.yml local tier (slnx-derived) |
| `SkiaViewer.Tests` | local | gate | gl | degrade-and-disclose (skipped, disclosed) | gate.yml GL step |
| `Smoke.Tests` | local | gate | gl | degrade-and-disclose (skipped, disclosed) | gate.yml GL step |
| `surface-baselines` | ci (push/PR) | gate | none | runs (see §4 coverage) | gate.yml drift step |
| docs build (`fsdocs`) | ci (push/PR) | gate | none | runs (build only, strict) | gate.yml docs step |
| harness **T0** (`offscreen` det.) | infra (R5) | gate | none | runs (required) | gate.yml harness-evidence |
| harness **T1** (`offscreen` readback) | infra (R5) | gate | gl | degrade-and-disclose (advisory) | gate.yml harness-evidence |
| harness **T2** (`live-x11`) | infra (R5) | capability | x11 | degrade-and-disclose until capable runner | capability.yml |
| harness **T3** (`perf` paced-native) | infra (R5) | capability | gl/x11 | degrade-and-disclose until capable runner | capability.yml |
| harness **T-uinput** (`input --backend uinput`) | infra (R5) | capability | uinput | inert + disclosed (backend pending) | capability.yml |
| `Package.Tests` (default tier) | validation-set | **gate** + release | none | hermetic; slnx member since #540 | gate.yml (slnx-derived loop), release.yml |
| `Package.Tests` (release tier) | release-only | release | none | deferred behind `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE` | release.yml |
| `Product.Tests` (template) | validation-set | **gate** + release | none | instantiated from the template; all five profiles on every PR since #680 (§4d) | gate.yml `generated-product-gate`, release.yml |

## 3. Audit invariants and result

The audit (FR-009) checks these invariants by inspection of this map against the sources:

1. **Exactly one cadence per member** — no member appears in two cadence rows.
2. **No release-only member in `gate`** — the *release tier* of `Package.Tests` (the consumer smoke, deferred
   behind `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE`) and template `Product.Tests` never run on push/PR.
   **#540 changed what this invariant is protecting.** It used to be enforced by keeping the whole
   `Package.Tests` *project* out of the slnx — which also kept its ~325 hermetic, working-tree rules off
   every PR, so each fired only after the merge that broke it. The project is now a slnx member and its
   default tier runs on the gate; the release-scoped checks are held back by their own env flag, which is
   where the timing distinction belongs. The invariant is unchanged; the mechanism enforcing it moved from
   the *solution* to the *code*, where the author writing the rule can see it.
3. **Every row traces to a settled source** — validation-set members → `validation-set.md` (R3);
   harness tiers → `harness.md` (R5). Nothing is invented here.
4. **Only `gate` is required** — release/capability never block merge. Branch protection is enabled
   (§5): `Deterministic gate` and `API compatibility gate` are required contexts on `main`, and
   `enforce_admins` is on (§5.1, ADR-0103). `gate`'s third job, `Template payload restore gate`, is
   feed-dependent and deliberately unselected (§4b).
5. **Capability rows degrade-and-disclose** — never a silent drop, never a false pass.

### 3.1 Audit result (T019)

Cross-checked this map against `docs/validation/validation-set.md` and the actual triggers/steps in
`gate.yml`, `release.yml`, `capability.yml` on 2026-06-14; re-audited 2026-07-02 (Feature 235, #47)
after the gate loop became slnx-derived and the coverage machine-enforced. **PASS** (SC-003, SC-007):

1. **Exactly one cadence per member** — ✅ every row above appears once; no member is in two cadences.
   `CadenceCoverageTests` now asserts `deterministic ∪ GL == slnx test set` with no overlap.
2. **No release-only member in `gate`** — ✅ `gate.yml` runs all 17 slnx test projects (the 15
   deterministic members above + `SkiaViewer, Smoke` GL) plus `surface-baselines`, `fsdocs`, and
   harness `offscreen` (T0/T1) only.
   **Re-audited 2026-07-13 (#680):** template `Product.Tests` used to appear **only** in `release.yml`,
   and that is no longer true — nor should it have been. It is instantiated from the template and exists
   in no checkout, but "hard to reach" is not "release-only": nothing compiled it until publish day, so
   an uncompilable scaffolded test rode green `main` and detonated mid-release (#679). `gate.yml`'s
   `generated-product-gate` now scaffolds all five profiles and compiles + runs their tests on every PR
   (§4d). This does not violate the invariant: the *release-scoped* checks (the consumer smoke, the
   `RELEASE_LANE` coherence mirror) still run nowhere but the release lane.
   **Re-audited 2026-07-12 (#540):** `Package.Tests` is now a slnx member, so the gate's slnx-derived
   loop *does* reach it — deliberately. Its default tier is hermetic (working-tree reads only, ~4s) and
   its release-scoped checks stay out of the gate by being deferred behind
   `FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE`, which only `release.yml` sets. So no release check runs on a PR,
   and — new — no PR-breakable check waits for a release either. The old arrangement enforced half of
   that invariant and silently violated the other half.
3. **No trigger overlap** — ✅ `gate` = `push`/`pull_request` to `main`; `release` = `release:
   published` + `v*` **tag** push + manual; `capability` = weekly `schedule` + manual. The `release`
   `push` filter is tag-only, so it never fires on a branch push or PR. No event reaches two cadences
   for the same member.
4. **Only `gate` is required** — ✅ `release.yml`/`capability.yml` carry no required status and are
   intended to be excluded from branch protection (§5).
   > **Not re-audited since.** Branch protection was enabled after this audit ran, and
   > `API compatibility gate` joined the required set on 2026-07-09 (§5.1, ADR-0103). The ✅ above
   > covers only the `release`/`capability` half of invariant 4, which is unchanged. The invariant's
   > branch-protection half has never been machine- or audit-checked — nothing in the repo can read
   > branch protection (ADR-0103, "Open follow-ups").
5. **Capability rows degrade-and-disclose** — ✅ `capability.yml` invokes each tier through the
   `harness-evidence` action with no `required-tiers` and `continue-on-error: true`; absence/skip is
   disclosed, never a false pass and never blocking.

The 16 gate-run test members above (14 deterministic + `SkiaViewer`/`Smoke` GL) are exactly the
`tests/*.Tests` set of `FS.GG.Rendering.slnx` and the `validation-set.md` "Local inner loop" list —
no addition, no omission. This equality is asserted by `CadenceCoverageTests` (Feature 235), so it
cannot silently drift again (the earlier staleness — retired `Color`/`Input` still listed while six
projects were omitted — was the P4 finding #47).

## 4. Surface-baseline drift — chosen gate behavior

The gate regenerates the public-`.fsi` surface for **all 9 committed baselines** from the built
assemblies (`scripts/refresh-surface-baselines.fsx`, which writes directly to
`tests/surface-baselines/`) and then fails on any uncommitted change — the canonical
regenerate-then-`git diff` check:

- **Drift fails the gate.** Any modified baseline reds the gate; the fix is to rerun the script
  locally and commit the updated baseline.
- **First run with no baseline ⇒ FAIL (never a silent pass).** A newly-generated baseline that is
  **untracked** (a package gained a public surface with no committed baseline) fails the gate via the
  `git ls-files --others` check, rather than treating "nothing to compare" as success (FR-003).

**Full coverage (the earlier 4-of-9 gap is closed).** Originally the imported script regenerated only
4 surfaces and wrote them to `readiness/surface-baselines/` — a leftover from the R3→R4 migration that
relocated baselines to `tests/surface-baselines/` (see `PROVENANCE.md`) without updating the script.
The generator now (a) covers all 9 packages, (b) writes to the committed location, and (c) excludes
compiler-generated/anonymous types (their names embed a non-deterministic hash and would make the
baseline unstable). Before re-baselining, a public-surface review confirmed the 5 previously-stale
surfaces are **deliberately `.fsi`-governed and not over-exposed** (every public type is either
declared in a curated `.fsi` or explicitly `internal`) — see
[`docs/validation/surface-baseline-review.md`](../validation/surface-baseline-review.md). The
re-baseline was therefore purely additive (365 net-new public types recorded, 0 removed).

Verified locally on 2026-06-14: after the re-baseline, a fresh regenerate produces no `git diff`
(clean on a current checkout).

## 4a. Version-coherence guard — chosen gate behavior (Feature 209)

A sibling merge-blocking step, **Version coherence guard**, makes the FS.GG.UI version-staleness bug
class (Feature 204) a loud, local, automatic failure instead of a downstream consumer's broken build.
It runs `scripts/validate-version-coherence.fsx` in two layers, both merge-blocking:

- **Structural verdict-core (env-free).** Re-derives, from the repo + pushed `fs-gg-ui/v*` tags, that
  the single `<FsGgUiVersion>` literal is present exactly once and matches an existing snapshot tag and
  does **not lag** the latest (preview-aware SemVer compare, not string); the BOM uses the single
  `[$version$]` exact-bracket token with `B.ids == P.members`; the template's consumed pins all derive
  through `$(FsGgUiVersion)` and equal the documented 12-member manifest; and `build.fsx`'s runtime
  regex still resolves the literal. It compares pins **directly** — independent of any
  `WarningsAsErrors=NU1605;NU1608` consumer policy (FR-004).
- **Scoped restore-grounded proof (`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1`).** One Release pack + one
  clean restore of `FS.GG.UI@V` asserting the **complete** 16-member set resolves to exactly `V`
  (FR-008, anti-text-grep). The deeper full generate→restore→build of a product from the template ran
  **only** on the release lane until #680; it now also runs on every PR (`gate.yml`
  `generated-product-gate`, §4d), because a check that first speaks at publish time speaks too late
  (#679). `release.yml`'s `template-product-tests` is kept as the last line of defence and as the
  `push: tags` escape hatch's only gate.

Exit codes: `0` coherent · `1` drift (names the location expected-vs-actual) · `2` guard error (inputs
unreadable / tags not fetched) — **fails closed**, never green-by-absence. On drift the `DRIFT […]`
lines are echoed to `$GITHUB_STEP_SUMMARY` (SC-006). The gate's `actions/checkout@v4` uses
`fetch-depth: 0` so `git tag` sees the `fs-gg-ui/v*` snapshot tags (otherwise the guard fails closed).

Note: the repo-root `<Version>` (`Directory.Build.props`) is **decoupled by default** (D5) and is not
compared by the guard.

### The guard validates the repo; the publish ships the trigger

`release.yml`'s `package-tests` job sets `FS_GG_VERSION_COHERENCE_RELEASE_LANE=1`, which disables every
RELEASE-PENDING waiver: at publish time no tag can be "cut next", so a missing tag is drift. That job
`needs:`-gates `publish-packages`, so nothing ships until the guard is green.

**Except on the pre-cut validation run (#681, §4e), where it is deliberately OFF.** `release-tags.yml`
now calls `release.yml` once with `validate-only: true` *before* it pushes any tag, so that a commit
whose validators fail never gets a tag that cannot be taken back. On that run the tags genuinely do not
exist yet — cutting them is the *next* step — so the RELEASE-PENDING waivers are exactly right, and
forcing them off would red the lane by construction on every release (the always-red-lane failure #506
removed, one workflow up). On the real publishing call the flag is `1` and a missing tag is drift,
unchanged.

But the guard's **subject** is the version it reads from the repo (`<Version>` in
`.template.package/FS.GG.UI.Template.fsproj`), while `publish-packages`' **object** is the version it
resolves from the trigger. On `release` and `push: tags` those are the same string — the tag names the
commit whose `<Version>` was validated. On `workflow_dispatch` they are not: `inputs.version` is free
text, so a dispatch from a coherent `main` validated one version and published another, untagged, and
`template-dispatch.yml` (which fires only on `fs-gg-ui-template/v*`) never told FS.GG.Templates. That is
the publish-before-announce class of `FS-GG/.github#250`, entered through the door beside the guard.

`publish-packages` therefore asserts `inputs.version == <Version>` before it packs or pushes anything.
A manual dispatch can only *re-publish* the version `package-tests` just proved coherent and tagged
(idempotent via `--skip-duplicate`); a pack-only dry run (no input) is exempt because it ships nothing.

### Why the two version axes stay independent

The guard's size comes mostly from carrying **two** decoupled version axes, and the natural question is
whether one could be derived from the other:

- the **framework pin** `<FsGgUiVersion>` — what a generated product restores `FS.GG.UI.*` at, snapshotted
  by `fs-gg-ui/v*`;
- the **template package** `<Version>` of `FS.GG.UI.Template` — what `dotnet new install` resolves,
  snapshotted by `fs-gg-ui-template/v*` and triggered by `v*`.

They must remain independent because **template content changes without the framework changing**. A
template-only release (new sample, fixed scaffold, corrected doc) ships a new `FS.GG.UI.Template` over an
*unchanged* framework pin. Deriving the pin from the package would republish all sixteen `FS.GG.UI.*`
members at the template's version on every such release — which is exactly what produced the orphaned
`FS.GG.UI.* 0.1.60/0.1.61` packages that no product pins. Deriving the package from the pin would forbid
template-only releases altogether. So the axes are ordered, not equal: `pin <= package`
(`pin-leads-package`), because a framework snapshot is only reachable through a template that consumes it.

The registry records this as `fs-gg-ui-template: version = framework pin` vs `package-version = release
tag`; ADR-0012/0013 (dual-publish) and ADR-0024 (consumer edges) depend on the split. Collapsing the axes
is a cross-repo decision — it changes a published contract — so it belongs in an `FS-GG/.github` ADR, not
here; this section records why the independence is load-bearing today.

## 4b. Template payload pins — chosen gate behavior (#241)

`template/base/Directory.Packages.props` declares three version axes — `$(FsGgUiVersion)`,
`$(FsGgGameVersion)`, `$(FsGgAudioVersion)` — that become a **scaffolded product's** package pins.
Until #241 nothing in CI ever **restored** them. `Package.Tests` reads the file as *text*:
`AudioProfileWiringTests` asserts axis *structure* and explicitly disclaims the *value*, and template
payload is *content* rather than a nuspec dependency, so `NU5104` cannot see it either. Only
`$(FsGgUiVersion)` was protected, by §4a. A pin that was stale, prerelease, yanked, or simply
nonexistent passed every check in this repo — which is exactly how #235 happened: a **stable**
`fs-gg-ui` template (`0.4.0`) scaffolded products restoring **prerelease** `FS.GG.Game.*` /
`FS.GG.Audio.*`, with `$(FsGgGameVersion)` also two minors stale, and nothing was red for months.

`scripts/validate-template-payload-pins.fsx` closes it, in two layers on **two different jobs**:

- **Structural verdict-core (env-free, offline)** — step of the required `Deterministic gate`. The
  three axis literals are present exactly once each and well-formed; **every** `FS.GG.*`
  `PackageVersion` derives through one of the three axes (a bare literal is invisible to an axis bump
  and to the staleness rule below); every `<!--#if -->` profile gate has a shape the guard can
  evaluate; and **every scaffold profile resolves a non-empty pin set** — a profile that restores
  nothing would otherwise report a vacuous pass.
- **Restore-grounded proof (`FS_GG_RUN_TEMPLATE_PAYLOAD_RESTORE=1`)** — the separate
  `Template payload restore gate` job. It restores each of the five profiles for real, on the
  template's real TFM (`net10.0`), against nuget.org, and asserts the **resolved graph** rather than
  the literals: no prerelease `FS.GG.*` **including transitive** (`FS.GG.Game.Render` reaches down to
  `FS.GG.UI.Scene`, so a regex over the three literals would miss it); `NU1603`/`NU1608`/`NU1101`/
  `NU1102`/`NU1605` promoted to errors so a nonexistent pin cannot resolve upward silently; every
  pinned `(id, version)` present on the feed; and the Game/Audio pins not **lagging** feed-newest.

Exit codes: `0` coherent **or `RELEASE-PENDING`** (see below) · `1` drift (named, expected-vs-actual) ·
`2` guard error (feed unreachable, restore tooling failed, an unevaluable gate, zero pins matched). It
**fails closed**, per `FS-GG/.github#266`: *"nothing to check" and "checked, and it's fine" must not
share an exit code.*

**`RELEASE-PENDING` — why this gate is no longer expected-red on a release PR** (#506). The pin bump is
what *causes* the publish: `release-tags.yml` cuts `fs-gg-ui/v<pin>` on merge and calls `release.yml`.
So at PR time `$(FsGgUiVersion)` necessarily names a version nuget.org does not carry yet, and this gate
reported `pin-not-published` plus `NU1102 pin-does-not-resolve` on all five profiles — **by
construction**, on every framework release ([#426](https://github.com/FS-GG/FS.GG.Rendering/pull/426)
for 0.8.0, [#498](https://github.com/FS-GG/FS.GG.Rendering/pull/498) for 0.9.0), merged past both times.
That is the always-red advisory gate of `FS.GG.SDD#362`, and it was worse than noise: a **genuine**
`pin-not-published` — a typo, a pin onto a version never published, a half-failed release — produced a
**byte-identical** red, so the gate camouflaged the very defect it exists to catch.

The guard now waives it, bounded exactly as `validate-version-coherence.fsx`'s `PinPending` is:

- **`$(FsGgUiVersion)` only** — the axis this repo publishes. `FS.GG.Game.*` / `FS.GG.Audio.*` ship from
  **other** repos, where a bump here publishes nothing, so an unpublished pin on those axes is a real
  defect *even on the commit that bumped it*, and stays red. (A naive "bumped ⇒ waive" would reopen
  #235 — a stale component pin, green.)
- **only when *this* commit bumped it** (`git diff HEAD~1 HEAD` over the props file — hence the job's
  `fetch-depth: 0`). A pin nobody bumped that the feed does not carry is stale or typo'd: drift, as before.
- **only when the pin is genuinely absent from the feed**, so the ordinary case still runs the full proof.
- **never in the release lane** (`FS_GG_VERSION_COHERENCE_RELEASE_LANE`), where every package is due.

In the window the resolved-graph proofs genuinely **cannot** run — no profile can restore against
packages that do not exist — so they are **skipped and reported as skipped**, never as passed. The
`RELEASE-PENDING` block names what was not proved (the five profiles' graphs) and what still was (the
structural core, and Game/Audio existence + staleness). If the publish never lands, the next commit to
`main` does not bump the pin, the waiver is off, and the gate reds on `pin-not-published`.

**Why the restore half is a separate, non-required job.** It reads nuget.org, and requiring a
feed-dependent check takes a dependency on that feed's availability — an outage would wedge every
merge in the repo. `api-compatibility-gate` reads a feed too, and **is** required (§5.1, ADR-0103):
it reads one only to *find a baseline*, so it can classify a silent feed as `FeedUnavailable` and
exit 0. This job cannot — the feed is its **subject**, and "the payload did not restore" cannot be
told apart from an outage. It therefore has **no elevation path** and stays non-required.
**Not-required is not `continue-on-error`** (#216): the job has none, and a red is the gate
reporting what it found.

**Why `$(FsGgUiVersion)` is exempt from the staleness rule, and only that rule.** The `FS.GG.UI.*`
set is published *from this repo*, and §4a already pins it to `fs-gg-ui/v*` snapshot tags — a source
of truth that exists at merge time. Feed-newest does not: between a release landing on `main` and the
packages reaching nuget.org, feed-newest **lags the repo**, and a "pin ≥ newest" rule would red every
PR in that window against this repo's own release. Game and Audio ship from *other* repos, where the
feed *is* the source of truth and no such window exists. UI pins are still proved to exist and to be
non-prerelease by the restore layer.

**The preview channel is asserted by `$(FsGgUiVersion)` alone.** A prerelease framework pin means the
template deliberately scaffolds preview products, so a prerelease component is coherent with it. It
must not be read as "any axis is prerelease": under that reading a prerelease `$(FsGgAudioVersion)`
would declare the very preview channel that excuses it, and #235 would have stayed green.

## 4c. The packaged-consumer path — chosen gate behavior (#300)

Four sample suites — `AntShowcase`, `ControlsGallery`, `SampleApps`, `SecondAntShowcase`, **twelve
`.fsproj`** — consume the framework *only* as packed `FS.GG.UI.*` NuGet packages. None is in
`FS.GG.Rendering.slnx` (deliberately: an in-solution project would take `ProjectReference`s and stop
proving the packaged path at all), and until #300 none was named in any workflow. The path an actual
downstream product walks was compiled on **zero** pull requests, while each sample's `nuget.config`
asserted the opposite: *"building against it IS the proof the Ant-theme consumer path works end to
end (research R1 / FR-015 / SC-006)"*. The proof was asserted and never executed.

`src/` is fully gated, and §5.1's `ApiCompat` guards the public surface. What neither can see is the
**composition** — whether the sixteen packages, restored together at one version from a feed, build a
program. `ApiCompat` compares surfaces *pairwise*; it cannot see that `AntShowcase.Core` opens eight
of them at once. Only compiling the consumer sees that.

The lane splits on exactly the §4b axis — **does this check need a feed?**

| half | what it asserts | needs a feed? | where it runs | required? |
|---|---|---|---|---|
| pin mirror | every `samples/**` `FS.GG.UI.*` pin `==` `src/*/*.fsproj` `<Version>` | no — it reads two sets of files as text | a step of `gate.yml`'s `Deterministic gate` | **yes** |
| source proof | pack `src/` → restore **and build** the twelve `.fsproj` against that feed | yes — nuget.org, for the samples' third-party deps | `packaged-consumer.yml` | no |

Both run `tools/Rendering.Harness` `package-feed` (`--mode check` / `--mode proof --pack`), which
already existed and which nothing had ever invoked.

**Neither names a sample.** The harness discovers them: a `samples/*/` directory is a package consumer
exactly when its own `nuget.config` maps `FS.GG.UI.*` to the local feed. A hardcoded list is how a
consumer goes ungated in the first place, so a fifth sample is covered by construction — the same
reasoning that makes the deterministic tier derive its test projects from the slnx. Discovery finding
nothing is an error (exit 2), never a vacuous pass.

**Why the proof cannot be required.** Its *subject* is a feed, so a nuget.org outage is
indistinguishable from a bad pin — the same reason `template-payload-restore-gate` stays
non-required, and the reason `api-compatibility-gate` could be elevated (it classifies a silent feed
as `FeedUnavailable` → exit 0; this cannot). NOT-REQUIRED IS NOT `continue-on-error` (#216): the
workflow has none.

**Why the pin half *can* be required.** It packs nothing and reads nothing over the network.

**The mirror rule.** `samples/*/nuget.config` maps `FS.GG.UI.*` *exclusively* to the machine-local
feed, which only a local `dotnet pack` of `src/` fills, at `<Version>`. So a sample pin naming any
other version cannot resolve (`NU1102`). Renovate's datasource is `nuget.pkg.github.com` — a feed
these projects are configured never to read — so **every** version it can propose is one they cannot
resolve. That is PR #233: it proposed the published `0.4.0` against a tree pinned to
`0.4.0-preview.1`, and merged with **4/4 green** because nothing in CI read the files it changed. The
gate now names the rule on failure, and `.github/renovate.json` disables `FS.GG.UI.*` under
`samples/**` so the bot stops re-proposing it every cycle. **Fix the pin, not `<Version>`** — the pin
moves only with the `release:` commit that moves `<Version>`.

**Lockfiles.** The samples have no `packages.lock.json` and gain none: in-solution projects restore
`--locked-mode`, but a `src` `<Version>` bump already stales 24 committed lockfiles, and locking
twelve more would add that cost to every release to hash-pin a graph the proof rebuilds from source
on each run. The proof restores **unlocked**, into an **isolated** package cache, so a stale global
cache entry cannot mask a feed that is missing a package.

**Cost.** Packing sixteen projects is not free, so `packaged-consumer.yml` is `paths:`-filtered to
`src/**`, `samples/**`, `tools/Rendering.Harness/**`, and itself. That filter is *why* it is its own
workflow rather than a job in `gate.yml`: a required workflow that is path-skipped never reports its
context, which blocks the merge button forever.

Tracking issue: FS.GG.Rendering#300. Evidence that the gate fails a hand-reverted #233:
`specs/163-package-feed-validation-lanes/readiness/packaged-consumer-gate.md`.

## 4d. The scaffolded product — chosen gate behavior (#680)

`template/base/tests/Product.Tests/**` becomes the **generated product's own test suite**. Until #680
the only thing that ever compiled it was `release.yml`'s release-only `template-product-tests`. The
neighbouring lanes each stop one step short: `lifecycle-live-gate` scaffolds every profile × lifecycle
and **audits the emitted tree** — it reads files, it never builds them — and `Package.Tests` reads the
template as **text**. So an uncompilable scaffolded test was green on every gate and detonated
mid-publish, after `release-tags` had already pushed the `v0.9.1` tag triple, leaving `main` pinned to
a version that does not exist (#679).

**A gate that only ever runs at publish time discovers its findings at the worst possible moment.**
`gate.yml`'s `generated-product-gate` now scaffolds, compiles and tests the product on every PR.

**All five profiles, and that is the gate — not gold-plating.** The two scaffolded test files are cut
into *mutually exclusive* `//#if (profile == …)` regions, so **each profile compiles different
source**:

| file | regions |
|---|---|
| `BehaviorTests.fs` | `{governed\|headless-scene}` · `{game}` · `{app\|sample-pack}` |
| `GovernanceTests.fs` | `{governed\|headless-scene}` · `{the rest}` |

#680 originally proposed *one representative profile* per PR to hold the cost down. That was measured
and rejected: `app` — the obvious representative — touches neither governed region, and a stray
`//#endif` in `GovernanceTests.fs` (arriving with #436, the change family that caused #679) had left
`governed` and `headless-scene` **uncompilable on green `main`**. Reintroducing that one line reds the
gate on exactly those two profiles while `app`, `sample-pack` and `game` stay green — so a
one-profile gate would have shipped the break. **A profile that is not compiled is not gated.**

**Cost is not the constraint it looks like.** The pack dominates and is *shared*: pack once (~40s),
then each profile is an instantiate + build + test (~15–20s). All five land in ~2 min, in **one job
with a loop** — deliberately *not* a `strategy.matrix`, which would give each profile a fresh runner
and re-pack the whole solution five times to buy nothing.

**Hermetic on the `FS.GG.UI.*` axis.** `.github/actions/product-local-feed` — shared verbatim with
`release.yml`, so the gate cannot drift from the lane it is supposed to predict — packs the framework
set **from this tree** at the template's pin and maps `FS.GG.UI.*` to that feed alone. Two consequences
matter: the gate is **green on a release PR**, where the pin names an unpublished version by
construction (#506's trap — a lane that is expected-red at release time is one people merge past), and
it tests the template against **this diff** rather than against the last release (#452's rule, one
layer down).

**Unconditional — no `paths:` filter.** #679 arrived through a *test* file; no plausible hand-written
filter over `src/**` would have caught it.

**Why it cannot be required.** Everything that is *not* `FS.GG.UI.*` — FSharp.Core, Expecto, and
Game/Audio on the game and sample-pack profiles — still restores from nuget.org, exactly as a real
consumer does. Its subject includes that feed, so an outage is indistinguishable from a real break:
the same bar that keeps `template-payload-restore-gate` (§4b) and `packaged-consumer` (§4c)
non-required, and that `api-compatibility-gate` clears only because it can classify a silent feed as
`FeedUnavailable` → exit 0. This lane has no such classification and therefore **no elevation path**.
NOT-REQUIRED IS NOT `continue-on-error` (#216): the job has none and must not grow one.

Tracking issues: FS.GG.Rendering#680 (this lane), FS.GG.Rendering#679 (the wedged release it exists to
prevent).

## 4e. The cut order — chosen release behavior (#681)

`release-tags.yml` used to do two things, in this order, on a push to `main`: **cut** the tag triple
(on the strength of the coherence guard's exit code), then **call** `release.yml`, whose release-only
validators gate the publish.

That order is backwards, and it produced #679. The guard's exit code is a **structural** verdict — it
says nothing about whether the artifacts the commit describes can be built, tested or published. On
`1fddbd0b` the cut succeeded, the validators failed, the publish was correctly skipped, and `main` was
left with the full `v0.9.1` tag triple, **no package on any feed**, and a pin naming a version that does
not exist.

**The trap is the second half.** `release-tags` derives what to cut from the guard's `RELEASE-PENDING`
block. With `fs-gg-ui/v0.9.1` pushed, the guard reads `pin == latest tag` and reports **COHERENT, exit
0, nothing pending** — the same thing it reports after a release that *worked*. So no future push to
`main` would ever cut, therefore never call `release.yml`, therefore **never publish 0.9.1**. The repo's
own guard certified a release that did not exist, and no automation could reach it.

**The order is now:**

```
plan  →  validate  →  cut  →  publish  →  (rollback-failed-cut)
```

| job | does | pushes a tag? |
|---|---|---|
| `plan` | guard verdict, parse the triple, #517 purity proof (tags cut **locally**) | **no** |
| `validate` | `release.yml` with `validate-only: true` — the real validators, no publish | no |
| `cut` | pushes the triple, in the guard's order | **yes** |
| `release` | `release.yml` — validators again (release-lane), then `publish-packages` | no |
| `rollback-failed-cut` | deletes the triple if the publish failed or was cancelled | deletes |

**Why this is the fix, stated as the property that actually matters.** Both orders fail into a freeze —
unavoidable once a bump is on `main` — but they fail into *different* freezes:

- **cut-then-validate**: tags cut · publish failed · guard says `COHERENT` · `RELEASE-PENDING` gone
  ⇒ the release can **never** be retried by any automation. Human surgery (delete three tags, or cut a
  new version).
- **validate-then-cut**: no tags · publish never ran · guard still says `RELEASE-PENDING`
  ⇒ fix the cause, push to `main`, and the workflow cuts and publishes. **Self-healing.**

**`rollback-failed-cut` is the compensating half, not the fix.** `validate` removes the *known* cause;
it cannot remove a publish that fails *after* the cut for reasons no pre-cut check can see (a nuget.org
5xx, an expired Trusted-Publishing trust). Then the tags come back off — `v*` deleted **first**, so
every intermediate state stays one the guard reads as `RELEASE-PENDING` — and the release is retryable
again. Retrying is safe even after a partial publish: `publish-packages` pushes `--skip-duplicate`, and
a retry necessarily republishes the *same* version (`<Version>` is a property of the commit).

**Known residual.** If the runner dies between `cut` and `rollback-failed-cut`, the phantom state
returns and the guard will again call it `COHERENT`. Closing that needs the guard itself to treat a
`v*` tag with no package behind it as **drift** rather than coherence — a change to
`scripts/validate-version-coherence.fsx`, tracked separately.

**Cost.** The heavy validators run twice on a release (once pre-cut, once in the publishing call). That
is deliberate: `release.yml` is also the entry point for the operator's manual `push: tags` escape
hatch, which nothing else validates, and its `package-tests` re-runs with the release-lane waivers *off*
— a genuinely different assertion (that the tags just cut are actually there). A release happens every
few weeks; #679 cost days.

Tracking issues: FS.GG.Rendering#681 (this order), FS.GG.Rendering#679 (the wedged release it produced).

## 4f. Frozen skill mirrors — chosen gate behavior (#738)

This repo ships byte-identical copies of four product skills whose canonical bodies live in **FS.GG.Game**
(ADR-0022 §6). `scripts/check-frozen-mirrors.fsx` guards them. It used to run, whole, inside the required
`Deterministic gate` — and it asked **two questions that are not the same kind of question**:

| | question | whose commit decides it | lane |
|---|---|---|---|
| 1 | did **this change** edit a body this repo does not own? | **this one** — the tree, against the digest it **declares** (§4g; `git` against the merge base until #833) | `--required` |
| 2 | is our mirror what FS.GG.Game's `main` says it is *right now*? | **somebody else's** | `--freshness` |

Question 2 has an input that is not in this tree, so its answer moves when another repo merges. In a
required, `enforce_admins` gate that is a **merge freeze on a pristine `main`**: FS.GG.Game lands a skill
edit, every open PR here goes red, and no change in this repo can clear it — someone has to hand-copy
bytes. That is #714, and #696 records the rate: *"Nine 'main is RED, nothing can merge' incidents in three
days; this is one of the recurring sources."* The job is called **Deterministic gate**; with question 2 in
it, it was not one.

**[ADR-0105](../product/decisions/0105-a-required-gate-reads-only-the-commit.md)** is the rule this
settles under, and its one-sentence test is the whole of the decision:

> **Could this gate turn an already-green commit red without anyone changing this repository?**
> If yes, it may not be required.

**The split.**

- **`--required`**, in the `Deterministic gate`. Reads the working tree — **no registry, no canonical, no
  network, no `GH_TOKEN`** (its `env:` block is gone, and a test asserts the absence). Since #833 its
  **verdict** needs no `git` either: it reds on `MIRROR EDITED` — a body that does not hash the digest this
  tree **declares** it froze, which is #541's entire case (see §4g); on a mirror **deleted**; and on a
  `NoCounterpart` body **vendored in**. It still *invokes* `git`, for the **evidence** its error carries
  (which of the body and the declaration moved) — a checkout git cannot read costs a worse message, never a
  different verdict.
- **`--freshness`**, in the **non-required** `Frozen mirror freshness` job. Reads the org registry and the
  owners' live bodies. Reds on `CANONICAL MOVED` and `FROZEN MIRROR STALE`, prints the runnable re-freeze
  command, and asserts the in-tree pin (`FrozenMirrorVerdict.foreignSkills`) against the registry so a new
  foreign skill cannot arrive unguarded. Its remedy is a **re-freeze PR** — which is ADR-0105 option (1)
  working as intended: the external thing moving produces a reviewable, schedulable PR here instead of a
  red `main`.

**Two things this is not.**

It is **not a demotion to a warning.** Every drift verdict still reds a lane; `FrozenMirrorVerdict.failsIn`
is the one place that decides which. The freshness job carries **no `continue-on-error`** (#216) and must
never grow one: not-required means branch protection does not *block* on it, not that a stale mirror may
merge in silence. A stale mirror ships a `--profile game` scaffold a skill FS.GG.Game has already moved on
from.

And it is **not enough to demote `CANONICAL MOVED` alone**, which is what #738 originally proposed.
`CANONICAL MOVED` and `FROZEN MIRROR STALE` are the **same stale mirror at two moments in time**: `decide`
returns the first only while the registry's nightly bot has not yet reconciled, and the identical,
untouched, still-stale mirror falls through to the second the moment it does. Demoting one and not the
other buys a wedge that waits for a cron job. Both moved; both still red the freshness lane.

Tracking issues: FS.GG.Rendering#738 (this split), FS.GG.Rendering#714 (the live wedge),
FS.GG.Rendering#541 (why the guard exists), FS.GG.Rendering#720 (the verdict split it depends on).

## 4g. Frozen skill mirrors — how a re-freeze lands (#833)

§4f's split left one edit with **no legal way to merge**: the **re-freeze** itself.

The required lane asked `git` *"did this change edit a mirror?"* — and a re-freeze **does**. Copying new
canonical bytes down and vendoring in your own content are the **same diff** against the merge base, and the
canonical that would tell them apart is the input ADR-0105 forbids that lane from reading. So the required,
`enforce_admins` gate red on the one edit this repo is **obliged** to make. #789 was the first re-freeze
after §4f landed and hit it immediately; PR #832 carried correct content, was green on the freshness lane,
and needed a **one-time protection lift** to land. Every earlier re-freeze (#714, #773, #780, #781) predates
§4f, when the required lane checked *freshness* — which a re-freeze **satisfies**. #696 counts ~40 of these,
so it recurs until fixed.

**The decision: an offline lane cannot infer intent, so it is TOLD.** Each `Mirrored` skill in
`FrozenMirrorVerdict.foreignSkills` **declares** the canonical digest it is frozen to, and the required
lane's whole question becomes one the tree can answer:

> **Is the body in this tree the body this tree says it froze?** (`sanctionOf`)

| what you did | body | declaration | required lane |
|---|---|---|---|
| nothing | unchanged | unchanged | **green** |
| **re-freeze** — both, one commit | new canonical | updated to match | **green** (#833) |
| edited a mirror (#541's author) | changed | untouched | **RED** |
| edited the declaration alone | unchanged | changed | **RED** |

**Why this is not a rubber stamp**, which is the only question worth asking about a sanction:

- **#541's break cannot reach it.** All three motivating breaks were authors who never knew the file was a
  mirror. None would write a declaration; every one still reds.
- **A forged declaration reds the freshness lane.** The declaration is `sha256(body)`, so
  `declaration ≠ canonical` **is** `body ≠ canonical` — the drift `decide` already convicts on. Forging buys
  a green required lane and a red freshness lane: a silent break traded for a loud one.
- **It is deliberate and reviewable**, in the diff, next to the body it sanctions.

**What it gives up, said plainly:** an author who edits a body *and* writes a matching declaration passes
the required lane. That is the trade, not an oversight — the alternative was ~40 admin merges, and a gate
whose documented remedy is *"merge past a red required check"* teaches exactly one lesson.

**ADR-0105 is untouched, and did not need amending.** The sanction hashes this commit's own bytes against a
declaration carried in this commit — `sanctionOf` takes two digests off the working tree and consults no
registry, no canonical, no network, no token, and no `git`. *Could this gate turn an already-green commit
red without anyone changing this repository?* Still **no**, and now structurally rather than by argument:
there is no input in scope for FS.GG.Game to move.

ADR-0105's own narrative still calls `MIRROR EDITED` *"the one verdict `git` proves from the commit alone"*,
which described #738's implementation accurately and is now the *mechanism* rather than the *decision*. The
decision — a required gate reads only the commit — is what this strengthens. Amending or superseding an
accepted ADR is a call for a human, so it is flagged rather than done here.

That also retires §4f's `git`-probe fail-closed. It was right then — that lane convicted on `git` alone, so
a blind probe had to red or a shallow clone was a silent #541 fail-open — but its **premise** is gone. The
declaration is the oracle and it is in the working tree, which a shallow clone has in full; an undeclared
edit reds whether or not `git` can see the merge base. `baselineOf` stays as the freshness lane's oracle and
as **evidence** in the required lane's message, where it says *which* of the body and the declaration moved.
Reding on `Unknown` now would be a false red about a question the lane no longer asks.

Tracking issues: FS.GG.Rendering#833 (this sanction), FS.GG.Rendering#789 / PR #832 (the re-freeze it
blocked), FS.GG.Rendering#738 (§4f, the split that introduced the gap), FS.GG.Rendering#541 (why the guard
exists), FS.GG.Rendering#696 (the ~40-re-freeze cadence that makes it recur).

## 5. Branch protection (one-time maintainer step)

The spec defines which checks are required; **enabling** branch protection is the maintainer's
one-time action (it cannot be set from the repo tree). On `main`:

- **Require status checks to pass before merging** → select exactly **two** of the `gate` workflow's
  jobs: **`Deterministic gate`** and **`API compatibility gate (breaking-change → SemVer major)`**
  (§5.1). Do **not** add `release` or `capability` jobs as required (FR-007) — that constraint is
  about those two *workflows*, and says nothing about `gate.yml`'s own jobs. Every OTHER job in
  `gate.yml` is evidence, not a gate; the ones with a standing reason to stay that way are listed
  below.
- Leave **`Generated product gate (scaffold every profile, compile and run its tests)`** unselected
  (§4d). It restores the scaffolded product's third-party dependencies (FSharp.Core, Expecto, and
  Game/Audio on the game and sample-pack profiles) from nuget.org, so it is feed-dependent for the
  same reason — and with the same absence of an elevation path — as the two below.
- Leave **`Template payload restore gate (scaffolded pins resolve, stable)`** unselected. It is
  feed-dependent, and ADR-0101's bound applies to it as it does to ApiCompat (§4b). Unlike ApiCompat,
  it has **no elevation path**: its subject *is* an external feed, so a nuget.org outage is
  indistinguishable from a bad pin and must stay unable to wedge the repo.
- Leave **`Frozen mirror freshness (our mirrors still match FS.GG.Game's canonicals)`** unselected, and
  **it may never be elevated** (§4f, ADR-0105). Its subject is *another repo's `main`*, so it fails
  ADR-0105's test outright: FS.GG.Game merging a skill edit turns an already-green commit here red, with
  no change in this repo and no PR here able to prevent it. Requiring it is not a hypothetical mistake —
  it is what this job's other half was doing until #738, and it wedged every merge in the repo on a
  pristine `main` (#714). The half of that guard whose verdict **is** a function of this commit
  (`--required`: did *this change* edit a mirror?) stays where it was, in the required `Deterministic
  gate`, with all of #541's teeth.
- Leave **`Packaged-consumer gate (samples build against the packed feed)`** (`packaged-consumer.yml`)
  unselected, for the same reason, plus a second one: it is `paths:`-filtered, and a required check
  that is path-skipped never reports its context — it would block the merge button on every PR that
  does not touch `src/**` or `samples/**` (§4c). Its offline half, the sample-pin mirror check, is
  already a step of the required `Deterministic gate`.
- Do **not** enable "Require branches to be up to date before merging". This section originally
  recommended it, but under the ADR-0021 parallel intra-repo model it serializes every merge: each
  landing invalidates every other open PR's green. `gate` also runs on `push: main`, so a bad
  interleaving reds `main` rather than being prevented pre-merge. That is the chosen trade
  (ADR-0100).
- Leave `release.yml` and `capability.yml` unselected — they are advisory by design and must never
  block a merge (gate-contract "What can NEVER fail the gate").

Result: a PR merges iff `Deterministic gate` and `API compatibility gate` are green;
`Template payload restore gate`, release and capability runs are visible evidence but never gate.

### 5.1 `API compatibility gate` — required as of 2026-07-09

This section once said to select **only** `Deterministic gate`. That wording predates `gate.yml`'s
second job and was read as a standing prohibition on requiring it (ADR-0100). It was never a
judgement about ApiCompat — only a description of a workflow that had one job. **Amended here**
(FR-007 is unchanged: it constrains `release` and `capability`, which are advisory *by design*, and
says nothing about `gate.yml`'s own jobs).

**`API compatibility gate (breaking-change → SemVer major)` is authorized to join the required set**,
under exactly one precondition:

> **The job must be green on `main` at the moment it is added.**

That is not ceremony. A required check that is red on the base branch blocks *every* PR in the repo,
including the release PR that would discharge the red — a deadlock only an admin bypass escapes.

**Status (2026-07-09): DONE — the check is required.** The precondition was discharged and the context
added. Both contexts are now required on `main`:

```
Deterministic gate
API compatibility gate (breaking-change → SemVer major)
```

The second string is the job's `name:`, not its key, and the arrow is U+2192.

How it was discharged: the three breaks — `FS.GG.UI.Controls`, `FS.GG.UI.DesignSystem`,
`FS.GG.UI.Themes.AntDesign` — forced the SemVer major `0.4.0-preview.1`, cut by FS.GG.Rendering#225.
**The green followed the publish, not the merge.** `scripts/apicompat-check.sh` never reads this
repo's version: it packs each project at `check_version` (the baseline plus an `.apicheck` prerelease
identifier) and compares against `latest_version()` **off the feed**. The gate went on reporting the
three breaks until `0.4.0-preview.1` reached the feed and became the baseline — at which point the
compared surface is, by construction, the surface the baseline was packed from. It then reported
`OK=17 BREAK=0 (total 17, compared 17)` on `main`, and the context was added at that commit. See
[ADR-0101](../product/decisions/0101-apicompat-stays-advisory.md).

### What now blocks a merge

Requiring the check makes its exit codes load-bearing, and #216/#227 refined them after ADR-0101 was
written. The bound ADR-0101 relies on still holds, but it is `FeedUnavailable` — not `Indeterminate` —
that carries it:

| outcome | meaning | exit | blocks a merge? |
|---|---|---|---|
| `BREAK` | the gate ran and found a `CP####` removal | 1 | **yes** — cut a SemVer major |
| `Indeterminate` | `dotnet pack` failed, so the gate **never ran** for that packable | 3 | **yes** — a check that could not run is not a pass |
| `FeedUnavailable` | the feed did not answer (transport error, 5xx, no token) | 0 | no — a feed outage informs a merge, it does not block one |
| `NoBaselineYet` | the packable is not on the feed yet | 0 | no |

So the structural cost requiring it accepts is bounded: the check reads the package feed, but a feed
outage degrades to `FeedUnavailable` and exits 0, and a fork PR with no token resolves every packable
that way — forks still merge. What a red *does* mean is either a real break or a gate that could not
execute, and neither should merge.

Tracking issues: FS.GG.Rendering#219 (the authorization), FS.GG.Rendering#225 (the major that
discharged the breaks and elevated the check), FS.GG.Rendering#216 (the exit-code semantics above).

## 6. Quickstart validation outcomes (V1–V7)

Mechanically-validated locally on 2026-06-14 where possible; items needing a real GitHub run or a
true headless/fork context are marked. Source scenarios: `specs/005-ci-cadence-wiring/quickstart.md`.

| # | Scenario | Status | Evidence / note |
|---|---|---|---|
| **V1** | Clean PR ⇒ gate green; deterministic portion < 10 min | ✅ green (locally) | Deterministic gate path measured at **~192 s** locally (build + 9 local-tier tests + surface gen + harness offscreen) — comfortably under SC-002's 10 min (hosted runners are slower and add the fsdocs tool install; still expected to pass). All 9 deterministic local-tier projects pass (`Lib.Tests`: 30/30 after the samples quarantine below). Confirm end-to-end timing on the hosted runner. |
| **V2** | Deterministic break ⇒ gate red + merge blocked | ✅ mechanism confirmed | Any non-zero `dotnet test` in the local-tier loop reds the step and the job (the loop runs under `set -euo pipefail`). Observed live before the samples quarantine: the `Lib.Tests` sample failure surfaced as a real red — exactly the merge-block path. Confirm the full red→merge-block on a real PR once branch protection (§5) is enabled. |
| **V3** | Capability-blocked checks degrade & disclose | ✅ logic validated | The `harness-evidence` renderer was exercised against a synthetic headless `offscreen` (`T0` passed, `T1` `status:"skipped"`): T1 rendered under **notProvedHere** with its rationale, never under proved, and the overall stayed **pass**. On this dev box GL is present so a *live* headless skip can't be observed here — confirm on the hosted runner. |
| **V4** | Run summary states proved vs not-proved | ✅ by construction | Every harness step appends a proof-scope block (proved / notProvedHere / failed / overall + `runnerCapability`) to `$GITHUB_STEP_SUMMARY`; a reader answers "was live/visual behavior verified here?" from the summary alone. |
| **V5** | Misconfiguration fails fast, absence does not | ✅ logic validated | The action treats process **exit 2** (bad usage) as a hard `failed` (fail-fast), distinct from `status:"skipped"` (clean absence, never fails). Verified against a simulated exit-2 run. |
| **V6** | Each check at exactly its cadence | ✅ audited | See §3.1 — PASS. No overlap; no release-only member in the gate; only `gate` required. Real release/`workflow_dispatch` placement to be observed on first tagged release / manual capability run. |
| **V7** | Fork PR gets a real signal without secrets | ✅ by construction | `gate.yml` declares `permissions: contents: read` and uses no secrets, so fork PRs run the full gate. `release.yml`/`capability.yml` are guarded by `if: github.repository == 'FS-GG/FS.GG.Rendering'`, so fork events skip them without false failures. Confirm on a real fork PR. |

### Samples quarantine (so V1 is green now)

`tests/Lib.Tests` and `tests/Smoke.Tests` reference `samples/{BasicViewer,InteractiveViewer,ScreenshotGallery,…}`
projects that **do not exist** in this repo (samples were not imported at Stage R4; only
`template/fragments/samples` scaffolding is present). To let the gate be green on current HEAD, the
sample-dependent assertions were **quarantined to skip-with-reason when `samples/` is absent**, rather
than fail:

- `Lib.Tests` "BasicViewer contract smoke" now guards on the project's existence (`if File.Exists …`),
  exactly like its already-guarded `InteractiveViewer`/`ScreenshotGallery` siblings. Result: 30/30 pass.
- `Smoke.Tests` skips its three sample-contract tests via `skiptest` when `samples/` is absent (3
  Ignored, 0 failed). `Smoke.Tests` is GL-gated anyway and is skipped entirely on the headless gate.

Both **self-restore** to full assertions the moment `samples/` is imported — no further CI change
needed. Importing the samples (or otherwise restoring full sample coverage) remains upstream,
Stage-R4-style work outside R6.

> R6 also applied one small enabling fix: `Lib.Tests` and `Smoke.Tests` located the repo root by
> searching for `*.sln`/`build.fsx`, which no longer matches the migrated `FS.GG.Rendering.slnx` on
> net10.0 and threw at module init (blocking test discovery entirely). They now also detect `*.slnx`,
> mirroring the fix Feature 045 already made in `Elmish.Tests`. Without this, the wired members could
> not even be discovered to run.

## 7. Evidence-summary glue decision

**No new glue script was added** (Decision 6 default). The `harness-evidence` composite action's
inline renderer reads each tier's `run.json` (`status`, `skipReason`, `proofLevel`,
`authoritativeFor`, `notAuthoritativeFor`, `env`) and emits the full proof-scope summary
(proved / notProvedHere / failed / overall) directly to `$GITHUB_STEP_SUMMARY`, satisfying FR-006 and
SC-005 without a separate aggregator. `scripts/ci/summarize-evidence.*` was therefore **not** created,
and no corresponding `Rendering.Harness.Tests` test was needed (T024 conditional ⇒ skipped).
