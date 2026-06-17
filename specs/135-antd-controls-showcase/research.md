# Phase 0 Research: Ant Design Controls Showcase (G3)

All decisions resolve unknowns in the plan's Technical Context. Format: **Decision / Rationale /
Alternatives considered**. There are **no remaining NEEDS CLARIFICATION**.

---

## R1 — Refresh the local NuGet feed before building (the hard precondition)

**Decision**: Treat a **feed refresh** as the first build step. Run `dotnet pack FS.GG.Rendering.slnx -c
Release` (output → `~/.local/share/nuget-local/`, the constitution's pack location) so the feed carries
(a) the **new `FS.GG.UI.Themes.AntDesign.0.1.0-preview.1.nupkg`** and (b) a **`FS.GG.UI.Controls` package
that contains the 96-control catalog incl. the net-new Ant primitives**. Because the repack reuses the same
version string `0.1.0-preview.1`, also **invalidate the consumer cache** for the affected ids before
restore: `dotnet nuget locals global-packages --clear` (or delete just
`~/.nuget/packages/fs.gg.ui.themes.antdesign` and `~/.nuget/packages/fs.gg.ui.controls`). Document this as
quickstart step **V0** and as a README precondition.

**Rationale**: Verified facts at planning time —
- The feed packages are dated **2026-06-15**; feature 132 (which added the Ant theme + net-new controls)
  is dated **2026-06-16**, so the feed predates it.
- `~/.local/share/nuget-local/` contains **no `FS.GG.UI.Themes.AntDesign*`** package at all.
- The packed `FS.GG.UI.Controls.0.1.0-preview.1.nupkg` dll contains **none** of `Segmented`, `Timeline`,
  `Collapse`, `Breadcrumb`, `Pagination` (grep over the dll returned 0), confirming it is the pre-132
  surface, while `src/Controls/catalog.yml` now lists **96** controls.
- `src/Themes.AntDesign/Themes.AntDesign.fsproj` is `IsPackable=true`, `PackageId=FS.GG.UI.Themes.AntDesign`,
  `Version=0.1.0-preview.1`, and the project **is in `FS.GG.Rendering.slnx`**, so a slnx repack produces it.

Without R1 the showcase cannot restore (`Themes.AntDesign` missing) and, even if it did, would see only 52
of the 96 controls — coverage (SC-001) would be unsatisfiable. R1 changes no source and no public surface
(it re-packs already-built output to the documented feed), so it stays Tier 2.

**Alternatives considered**:
- *Bump the package version (e.g. `-preview.2`)* to dodge the same-version cache trap — cleaner for cache
  correctness but it is a **product release decision** (touches `src/` version + would ripple to every
  other consumer/template baseline), out of scope for a consumer sample. Rejected; cache-clear is local and
  sufficient.
- *Project-reference `src/Themes.AntDesign` directly* — would make the sample build without a feed refresh
  but violates FR-015/SC-006 (no `src/` references; building against the feed *is* the consumer proof).
  Rejected.
- *Wait for a release pipeline to publish the package* — no such pipeline runs on push (release.yml is
  release-tag only); the local feed is the documented dev consumer path. Rejected.

---

## R2 — Page model: family pages (bijection) + template pages (compositions)

**Decision**: One page registry `PageRegistry.all : Page list`, where `Page` carries a
`Kind : PageKind` (`Catalog | Template`). **Catalog pages** group the 96 controls by family and own the
`ControlIds` that participate in the coverage bijection. **Template pages** (the 6 enterprise templates)
carry `ControlIds = []` for coverage purposes and instead declare the catalog **control *types*** they
compose, validated separately (R6). Reuse G1's `Page` shape (`Id`, `Title`, `ControlIds`, a `view`
builder), extended with `Kind`.

**Rationale**: Enterprise templates deliberately reuse controls that already appear on family pages
(a workbench uses `toolbar`, `data-grid`, `panel` — all counted on their family pages). Forcing them into
the "exactly one page" rule (FR-002) is impossible. Splitting the page set by kind keeps the bijection
honest over family pages (SC-001) while still shipping the templates (SC-002). This is the single justified
complexity item in the plan.

**Alternatives considered**: Template pages with real `ControlIds` and a "duplicates allowed for templates"
fudge in the coverage check — muddier and weakens the drift gate. A separate registry per kind — duplicates
the nav/shell wiring. Rejected both; one registry + a `Kind` tag is plainest.

---

## R3 — Theme: consume `FS.GG.UI.Themes.AntDesign` directly (no accent seam)

**Decision**: `AntShowcase.Core.AntTheme` is a thin wrapper exposing `resolve : ThemeMode -> Theme` that
returns `AntTheme.antLight` for `Light` and `AntTheme.antDark` for `Dark` (from `FS.GG.UI.Themes.AntDesign`).
The app-bar toggle flips a `Mode : ThemeMode` field on the model; `Host.create` sets
`Theme = AntTheme.resolve model.Mode`. **No accent selector** (unlike G1) — Ant's brand-blue is intrinsic to
the theme; the two variants are `antLight`/`antDark`.

**Rationale**: Feature 132's public surface is exactly `antLight`/`antDark`/`resolve` (confirmed in
`AntTheme.fsi`). The showcase's job is to demonstrate *that* theme, not to invent an accent seam over it.
Dropping the accent selector simplifies the shell and the model versus G1.

**Alternatives considered**: Expose `AntTheme.resolve overrides` to let the showcase tweak the palette —
unnecessary for a faithful Ant demo and risks implying the showcase changes tokens (it must not, FR-016).
Rejected; use the shipped variants verbatim.

---

## R4 — Coverage check over the 96-control catalog

**Decision**: Port G1's `CoverageMap` but compute the bijection over **Catalog-kind pages only**:
`catalogIds () = Catalog.supportedControls |> List.map (_.Id)` (the live 96), `assignedIds () =
PageRegistry.all |> List.filter (fun p -> p.Kind = Catalog) |> List.collect (_.ControlIds)`. `check`
reports `Unreferenced` (catalog ids on zero catalog pages **and** assigned ids no longer in the catalog) and
`Duplicated` (ids on >1 catalog page). Clean ⇔ both empty. The `coverage` CLI subcommand prints the summary
and exits non-zero on drift; a Tests suite asserts `isClean`.

**Rationale**: Reading the catalog from the public `Catalog` surface makes the check fail on drift in either
direction (catalog grows/shrinks, or a page references a removed control) — the honesty property G1
established, now over the widened surface. Template pages are excluded by the `Kind` filter (R2).

**Alternatives considered**: Hard-code the expected count (96) — brittle; the catalog widened once already.
Rejected; the live `Catalog` is the source of truth.

---

## R5 — Representative seeded demo content for every family, incl. net-new Ant controls

**Decision**: Extend G1's `DemoState` with seeded content for the net-new Ant primitives so none renders
empty (FR-004): e.g. `Timeline` items, `Steps` current/step list, `Collapse` panels (one expanded),
`Segmented` options + selection, `Rate` value, `Pagination` page/total, `Breadcrumb` trail, `Tag`/`Alert`
samples per intent, `Card`/`Result`/`Empty`/`Skeleton`/`Drawer`/`Avatar` representative props, plus the
existing list/grid/tree/chart seeds. All values are literal constants (deterministic).

**Rationale**: A showcase whose new controls render empty fails its own purpose and the populated-content
acceptance scenarios. Literal seeds keep headless evidence deterministic (FR-011).

**Alternatives considered**: Generate demo data procedurally — risks nondeterminism and adds machinery.
Rejected; explicit literals (the constitution's "ugly literals over clever factories" preference).

---

## R6 — Enterprise template pages as catalog-control compositions

**Decision**: Realize the six `docs/product/ant-design/templates/*.md` recipes in `Templates.fs`, each a
`view` composed **only** of catalog controls (no bespoke control types), populated from `DemoState`:
- **workbench** — `toolbar` + `data-grid` working area + side `panel`s (per `workbench.md`).
- **list** — filter `toolbar` + paginated `data-grid`/list + `pagination`.
- **detail** — `descriptions`/record view + related `panel`/`card`s + `tabs`.
- **form** — sectioned form (`text-field`/`select`/`switch`/…) with **validation** + submit → `result`.
- **result** — `result` control success/info state + follow-up `button`s.
- **exception** — `result`-based 403/404/500 state + recovery `button`.
Each template page declares the catalog control **types** it uses; `TemplateTests` asserts every node maps
to a known `Catalog` id (SC-002). The **form-validation contract** (R-form): invalid input keeps the model
in an `Invalid` sub-state with visible field errors and **no** success result; valid submit transitions to
a `Submitted`/`result` state (SC-009, FR-006) — a pure `update` transition.

**Rationale**: The template recipes are committed "groundwork" docs that explicitly name G3 as their
consumer and list the exact controls + tokens each composes — authoritative source material. Composing from
catalog controls (not new ones) is the whole point: it proves the patterns are buildable on the public
surface.

**Alternatives considered**: Ship fewer templates (e.g. only workbench + form) — loses breadth the spec
asked for (all six, FR-005/SC-002). Build dedicated "kit" controls for templates — that is Workstream D3,
out of scope and would violate "no new controls." Rejected both.

---

## R7 — Determinism: seeded `FrameInput` scripts, no clock/RNG

**Decision**: Reuse G1's `Scripts.fs` approach verbatim — per page a deterministic `FrameInput<Msg> list`
(keys/pointers/idles, no `Tick`-driven time) replayed by `Perf.runScript`. No `System.Random`, no
wall-clock. The form page's script drives an invalid-then-valid sequence to exercise R6's validation states.

**Rationale**: This showcase has **no game loop** (unlike G2), so it needs neither a PRNG nor `Tick` deltas
— strictly simpler. Seeded scripts + pure `update` give byte-identical evidence across same-seed runs
(SC-004).

**Alternatives considered**: Random demo interactions — breaks determinism. Rejected.

---

## R8 — New sample tree vs. extending `samples/ControlsGallery`

**Decision**: A **new `samples/AntShowcase/` tree**, not an Ant mode added to G1.

**Rationale**: (1) G1's coverage golden is 52→10 on Light/Dark; mutating G1 to 96 controls + Ant + template
pages would rewrite its assertions and golden evidence and entangle two features. (2) The Ant showcase
needs the template-page concept and the Ant-only theme set, which G1 deliberately excludes. (3) Two
independently demonstrable samples better tell the "one control set, many themes" story (G1 = Default,
G3 = Ant). The cost — some duplicated shell/evidence scaffolding — is small and was already accepted for G2.

**Alternatives considered**: Parameterize G1 over a theme provider — more code churn in a shipped sample for
no consumer benefit, and it couples the two coverage stories. Rejected. Record the choice as an optional ADR
(`docs/product/decisions/`), mirroring G2's `0008-g2-sample-apps.md`.

---

## R-summary — package set

Consumes (all from the local feed after R1): `FS.GG.UI.Themes.AntDesign` **(new)**, `FS.GG.UI.Controls`
**(refreshed to 96 controls)**, `FS.GG.UI.{Color,Scene,Controls.Elmish,SkiaViewer,DesignSystem,KeyboardInput,
Testing}`. **Does not** consume `FS.GG.UI.Themes.Default` (Ant-only showcase). No package is authored here.
