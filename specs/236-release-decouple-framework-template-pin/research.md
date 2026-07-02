# Research: release framework@pin, template@tag; guard the release lane

## R1 — Why the issue's literal fix is wrong; the two-axis model

Issue #48 proposes "fail the release job when `$VER ≠ FsGgUiVersion` (or rewrite the scaffold's pin
to `$VER` before restore)". Both conflate two versions the repo **deliberately** keeps apart:

| Axis | Version-of-truth | Snapshot tags | Consumed by |
|---|---|---|---|
| framework `FS.GG.UI.*` | `<FsGgUiVersion>` in `template/base/Directory.Packages.props` = `0.1.58-preview.1` | `fs-gg-ui/v<V>` | generated **product** (restores at pin) |
| template `FS.GG.UI.Template` | `<Version>` in `.template.package/FS.GG.UI.Template.fsproj` = `0.1.61-preview.1` | `v<V>` + `fs-gg-ui-template/v<V>` | **composer** (`dotnet new` installs) |

Registry `FS-GG/.github → registry/dependencies.yml`, `fs-gg-ui-template`, states this outright:
`version: "0.1.58-preview.1"` (framework pin, "UNCHANGED (no src/ change)") vs
`package-version: "0.1.61-preview.1"` (template package, Features 230/231 content-only), and warns
"No `fs-gg-ui/v0.1.60` tag exists — pushing one would trip the Feature 209 pin-lags-tag guard." So a
template-only release **legitimately** has `$VER`(template) ≠ `FsGgUiVersion`(framework). Failing on
that inequality would block the exact releases the model is designed to allow. Rewriting the pin to
`$VER` before restore would instead test framework bits (`0.1.61`) the shipped template never pins
(`0.1.58`) — testing the wrong thing. → we fix the workflow to version each axis on its own truth.

## R2 — Tag/pin evidence (drift confirmed)

```
$ git tag --list 'v*' | sort -V | tail             → … v0.1.58 v0.1.60 v0.1.61
$ git tag --list 'fs-gg-ui/v*' | sort -V | tail     → … fs-gg-ui/v0.1.58   (STOPS at 58)
$ git tag --list 'fs-gg-ui-template/v*' | tail       → … 0.1.58 0.1.60 0.1.61
pin  <FsGgUiVersion>            = 0.1.58-preview.1
tmpl <Version>                  = 0.1.61-preview.1
```

`v0.1.60`→`c8ba4aa` (Feature 230), `v0.1.61`→`e39d1ce` (Feature 231) — both real template-content
commits with **no `src/` change**. Because `publish-packages` packed the whole slnx at `$VER`, those
releases published `FS.GG.UI.* 0.1.60`/`0.1.61` — orphans (no product pins them; no `fs-gg-ui/v*`
snapshot). The `template-product-tests` gate for those releases packed `FS.GG.UI.* 0.1.60`/`0.1.61`
to the local feed but the instantiated product restored `0.1.58` from nuget.org → the local feed was
never touched (dead weight).

## R3 — The corrected release.yml shape

- **`template-product-tests`**: the instantiated product restores `FS.GG.UI.*` @ pin. On a release
  the coherent set isn't on any public feed yet, so the local feed must carry **pin** bits, packed
  from *this* source tree. Read the pin from `Directory.Packages.props`; pack the slnx at the pin;
  add the feed. `dotnet new install .` installs the **working-tree** template (its content is tested
  as-is, independent of `$VER`). Net: the gate tests the exact framework bits @ pin from the local
  feed + the working-tree template content — the real consumer graph.
- **`publish-packages`**: two packs — `FS.GG.Rendering.slnx` at **pin**, `.template.package/…fsproj`
  at **`$VER`**. `.template.package` is **not** a slnx member (`grep` on the slnx: no match), so the
  two packs never overlap. Inter-package deps: framework members reference each other via
  ProjectReference (resolve to the pack `-p:Version` = pin); the template package pins `FS.GG.UI.*` at
  `$(FsGgUiVersion)` = pin. So `template@$VER` → depends on `framework@pin`, coherent. Push both with
  `--skip-duplicate`; when pin == `$VER` (a framework release) framework@pin == framework@$VER, so the
  behavior collapses to today's coherent-set publish.

## R4 — Guard extension (release lane), env-free + fail-closed

The Feature-209 guard already validates the framework lane (pin ∈ `fs-gg-ui/v*`, pin not lagging the
latest, BOM/template/member lockstep). Add a parallel **release-lane** block, same failure-shape
(`Rule/Location/Expected/Actual/Fix`), reading only the repo + `git tag --list`:

- `pkg-version` = single `<Version>` in `.template.package/FS.GG.UI.Template.fsproj` (fail-closed if
  absent / not unique).
- Tag sets `v*` and `fs-gg-ui-template/v*` (fail-closed if unfetched — never green-by-absence).
- Rules: `pkg-version` ∈ `v*` **and** ∈ `fs-gg-ui-template/v*`; `pkg-version` not < latest of either
  (preview-aware `SemVer.cmp`, reused); `pin ≤ pkg-version` (`pin-leads-package` if the framework pin
  is newer than the released template package).

Current tree satisfies all three (`0.1.61` ∈ both tag sets and is the latest; `0.1.58 ≤ 0.1.61`) →
**green now**, so this PR's own merge gate stays green while the drift *classes* become catchable.
The mirror `Feature209VersionCoherenceTests.fs` re-derives the same structural verdict for the
release/local lane and must gain the same three assertions (it shares no code with the script by
design — A1 authority note in the file).

## R5 — Alternatives considered

- *Fail `$VER ≠ pin`* — rejected (R1): blocks legitimate template-only releases.
- *Guard "pin must not lag latest `v*`"* — rejected: red now (`0.1.58 < 0.1.61`) and **wrong** — the
  `v*` lane is the template package, which the pin is *designed* to lag. The correct pin/`v*` relation
  is `pin ≤ pkg-version`, which we assert instead.
- *Delete the orphan `v0.1.60`/`61` framework packages* — impossible (immutable feeds) and needless
  (unreferenced). Prevent recurrence via FR-002.
