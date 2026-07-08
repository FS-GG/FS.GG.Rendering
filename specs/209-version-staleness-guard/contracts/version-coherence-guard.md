# Contract — Version Coherence Guard

The guard's "interface" is its CLI/exit-code behavior, its failure-message schema, and the gate-step
wiring. This is the surface a maintainer and CI depend on (Constitution Principle I — the honest
audience for a machinery feature is the command line + CI lane, not an `.fsi`).

## 1. Script CLI contract — `scripts/validate-version-coherence.fsx`

**Invocation**
```
dotnet fsi scripts/validate-version-coherence.fsx
FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx
```

**Layers** (mirrors `validate-bom-consumer.fsx`)
| Layer | When | Cost | Proves |
|-------|------|------|--------|
| Structural verdict-core | always (env-free) | text + `git tag` only | the Lockstep Invariant minus RestoreProof |
| Restore-grounded proof | `FS_GG_RUN_VERSION_COHERENCE_SMOKE=1` | one Release pack + one clean restore | RestoreProof (FR-008) |

**Exit codes**
| Code | Meaning |
|------|---------|
| `0` | Coherent — every invariant conjunct holds (for the layers that ran). |
| `1` | Drift — ≥1 conjunct false. stderr lists each failure; the readiness report is still written. |
| `2` | Guard error — inputs unreadable (missing props/nuspec), tags not fetched, pack/restore tooling failed. Fails **closed** (never reported as coherent). |

**Inputs** (read-only): `template/base/Directory.Packages.props`, `src/Meta/FS.GG.UI.nuspec`,
`src/**/*.fsproj`, `template/base/build.fsx`, `git tag --list 'fs-gg-ui/v*'`. Throwaway pack feed
under the system temp dir for the live layer. Does **not** read `Directory.Build.props` `<Version>`
(decoupled, D5).

**Output**: a `Verdict` summarized to stdout/stderr + a regenerated report at
`specs/209-version-staleness-guard/readiness/version-coherence.md` with `provenance:
verdict-core | live`.

## 2. Failure-message schema (FR-007)

Every failure names the location with expected-vs-actual — **never** a bare "incoherent":
```
DRIFT [<rule-id>] <location>
  expected: <value-or-relation>
  actual:   <value>
  fix:      <the unambiguous corrective edit>
```

Rule ids and example messages:
| rule-id | Trigger | Example `expected` / `actual` |
|---------|---------|-------------------------------|
| `pin-lags-tag` | FsGgUiVersion < latest tag (204) | expected `>= 0.1.51-preview.1` (latest `fs-gg-ui/v…`); actual `0.1.50-preview.1` |
| `pin-no-tag` | FsGgUiVersion has no `fs-gg-ui/v<V>` tag (phantom), and it is not RELEASE-PENDING (§2.1) | expected a tag `fs-gg-ui/v0.1.99-preview.1`; actual none |
| `bom-member-skew` | `B.ids != P.members` | expected `{…16…}`; actual missing `FS.GG.UI.Foo` / extra `FS.GG.UI.Bar` |
| `bom-pin-not-token` | a BOM dep version != `[$version$]` | expected `[$version$]`; actual `[0.1.50-preview.1]` |
| `template-pin-hardcoded` | a template pin not `$(FsGgUiVersion)` | expected `$(FsGgUiVersion)`; actual `0.1.50-preview.1` |
| `template-consumed-skew` | `T.pins != T.expected` | expected the 11-member consumed manifest; actual missing/extra |
| `single-source-not-unique` | `occurrences != 1` | expected `1` `<FsGgUiVersion>`; actual `2` |
| `runtime-regex-broken` | `build.fsx` regex no longer matches | expected a match for `<FsGgUiVersion>…`; actual none |
| `restore-partial` | live: a member did not resolve to `V` | expected all members `@0.1.51-preview.1`; actual `FS.GG.UI.Scene @0.1.50-preview.1` |

Release lane (P5 / #48 — the `.template.package` `<Version>` axis, decoupled from the framework pin):
| rule-id | Trigger | Example `expected` / `actual` |
|---------|---------|-------------------------------|
| `pkg-lags-release-tag` | `<Version>` < latest `v*` tag | expected `>= 0.3.1-preview.1` (latest `v*`); actual `0.3.0-preview.1` |
| `pkg-no-release-tag` | `<Version>` has no `v<V>` tag, and it is not RELEASE-PENDING (§2.1) | expected a release trigger tag `v0.3.2-preview.1`; actual none |
| `pkg-lags-template-tag` | `<Version>` < latest `fs-gg-ui-template/v*` tag | expected `>= 0.3.1-preview.1`; actual `0.3.0-preview.1` |
| `pkg-no-template-tag` | `<Version>` has no `fs-gg-ui-template/v<V>` tag, and it is not RELEASE-PENDING (§2.1) | expected `fs-gg-ui-template/v0.3.2-preview.1`; actual none — and `v0.3.2-preview.1` is already cut, so this tag was due BEFORE it (push order) |
| `pin-leads-package` | `<Version>` < FsGgUiVersion | expected `<= 0.3.1-preview.1` (released package); actual framework pin `0.4.0-preview.1` |

### 2.1 RELEASE-PENDING — the legal transient, and its push-order bound

A version bump and the tag that publishes it cannot land atomically: the tag can only point at the
commit carrying the bump. So the three no-tag rules above are **waived on the change that performs
the bump** (`bumpedInCommitUnderTest`, which compares the element's *value* across `git diff HEAD~1
HEAD` — a reindent must not silence a fail-closed rule). Three states, not two:

| state | condition | verdict |
|---|---|---|
| `LAGS` | version < latest tag | always drift |
| `RELEASED` | version has its tag | steady state |
| `PENDING` | version > latest tag, no tag, **and this change bumped it** | legal here, due next |

Two things bound the waiver.

**(a) Successor tags, per the mandated push order.** Only the last tag triggers `release.yml`:

    fs-gg-ui/v<pin>  →  fs-gg-ui-template/v<pkg>  →  v<pkg>

A tag's waiver holds only while **no successor** — no tag to its right — has been cut. Once a
successor exists the release is *under way*, so this tag is overdue, not pending.

| tag | successors | waived iff |
|---|---|---|
| `fs-gg-ui/v<pin>` | `fs-gg-ui-template/v<pin>`, `v<pin>` | pin bumped here ∧ neither successor cut |
| `fs-gg-ui-template/v<pkg>` | `v<pkg>` | `<Version>` bumped here ∧ `v<pkg>` uncut |
| `v<pkg>` | *(none — lands last)* | `<Version>` bumped here |

A tag is a successor only **within its own release**, so each rule asks about the version *it* is keyed
on. Both successors carry the template package's version; a framework release bumps pin and package
together (`pin-leads-package` forbids `pin > pkg`), so wherever a `fs-gg-ui/v<pin>` snapshot is pending,
`pin = pkg`. Keying the pin's bound on `pkgVersion` instead would count the **previous** release's tags
as successors of a new snapshot — a false red on every pin-only bump.

Both mis-orderings are caught, and they are distinct failures:

- `v*` pushed first → `pkg-no-template-tag`. `publish-packages` (`needs: package-tests`) is skipped, so
  the set never ships; but `template-dispatch.yml` fires only on `fs-gg-ui-template/v*`. *Publish before
  announce* — FS-GG/.github#250.
- `fs-gg-ui-template/v*` pushed before `fs-gg-ui/v*` → `pin-no-tag`. The dispatch has already told
  FS.GG.Templates to pin a framework snapshot that was never cut and never published. *Announce before
  publish* — the same class, mirrored.

**(b) `FS_GG_VERSION_COHERENCE_RELEASE_LANE=1`** disables all three waivers. Set by `release.yml`'s
`package-tests` job — the job that gates `publish-packages`. The waivers exist because a tag cannot
point at a commit that does not exist yet, which is only true *before* the merge; at publish time every
tag is due. Successor-tag bounds can only see a mis-order that **left a tag behind**, and a publish need
not leave one.

> **Not covered.** The `workflow_dispatch (version:)` trigger publishes `inputs.version`, which this
> guard never reads — it validates the repo's `<Version>`. A dispatch from a coherent `main` is green in
> the release lane and ships an untagged version. Closing that requires a check on `inputs.version`
> itself, or removing the publishing dispatch path.

RELEASE-PENDING is **not silence**: the guard prints a greppable block naming the tags to cut, in push
order, to stdout and `$GITHUB_STEP_SUMMARY`, on **every** verdict and before the live proof. It makes no
claim about legality — the exit code carries that. (Suppressing it on red would leave `printDrift` as the
only tag instruction, and that enumerates failures rather than a procedure; `releaseLaneFailures` is
therefore emitted in push order too, so following it top-to-bottom never pushes `v*` first.)

Both classifiers (`scripts/validate-version-coherence.fsx` and `Feature209VersionCoherenceTests.fs`)
carry the bounds; a change to one that does not mirror the other desyncs the two independent verdicts.
The mirror exposes them as pure predicates (`pinWaived` / `templateTagWaived` / `releaseTagWaived`) and
table-tests all `2^n` states, because both classifiers read the *live* repo — which is always coherent,
so every waiver branch is dead in every real run. That is exactly how `0c7e091` shipped a regression
through a green suite. Deleting a bound now fails a test.

**Known limitation.** `bumpedInCommitUnderTest` reads `HEAD~1..HEAD`, whose first parent is the base
branch under a `pull_request` merge-ref checkout and the previous `main` commit under a squash/merge
push. Under *Rebase and merge* a release lands as several commits and the bump is not at `HEAD`, so the
package lane reds at the tip. Squash-merge and merge-commit both keep the whole release in one diff.

**Also not enforced.** Tags are matched by **name**, never by the commit they point at. `git tag v<V>`
run on a stale `main` tags `HEAD`, not the bump commit, and satisfies every rule.

## 3. Gate-step contract — `.github/workflows/gate.yml`

- **Checkout MUST fetch tags**: `actions/checkout@v4` with `fetch-depth: 0` (or `fetch-tags: true`) —
  without it `git tag` is empty and the guard fails closed with exit `2` (D2).
- **New merge-blocking step** "Version coherence guard": runs the structural verdict-core (always)
  and the scoped restore-grounded proof (`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1`). Non-zero exit fails
  the gate ⇒ PR cannot merge to `main` (FR-006, SC-001/002/004). Placed alongside the existing
  surface-baseline-drift step (the established "regenerate-and-fail-on-drift" gate pattern).
- **Step summary**: on failure, echo the `DRIFT […]` lines to `$GITHUB_STEP_SUMMARY` so the reviewer
  sees the named location without opening logs (SC-006).
- **Policy independence (FR-004)**: the verdict compares the BOM token/bracket and pins directly; it
  does **not** depend on `WarningsAsErrors=NU1605;NU1608`. (Contrast: `validate-bom-consumer.fsx`'s
  consumer-loudness layer *does* depend on that policy — that layer proves consumer behavior, not the
  in-repo gate.)

## 4. Release-lane relationship (`.github/workflows/release.yml`)

The deeper **full generate→restore→build of a product from the template** (all profiles) remains the
release lane's existing Package.Tests / product-from-template responsibility. The guard does not move
or duplicate it; the gate's scoped restore is the minimum real-restore that grounds FR-008 for every
PR (D4).

## 5. Cross-repo contract (FR-010, D8)

Upholds — does not modify — the `fs-gg-ui-version` and `fs-gg-ui-bom` registry rows. A note/ADR in
`FS-GG/.github` (via the `cross-repo-coordination` skill) records that drift is now structurally
caught by this repo's gate before merge. Recorded **after** in-repo verification passes (208 ordering).

## Acceptance mapping

| Spec acceptance | Verified by |
|-----------------|-------------|
| US1 #1 (no snapshot) | `pin-no-tag` |
| US1 #2 (204 lag, expected-vs-actual) | `pin-lags-tag` |
| US1 #3 (all lockstep passes) | exit `0`, report `result: pass` |
| US1 #4 (PR blocked) | gate step non-zero exit |
| US2 #2 (BOM half-bump, policy-independent) | `bom-pin-not-token` / direct compare, no warnings-as-errors |
| US2 #3 (unwired new member) | `bom-member-skew` / `template-consumed-skew` |
| US3 #1/#2 (partial/undefined restore fails) | `restore-partial` (live layer) / exit `2` on undefined property |
