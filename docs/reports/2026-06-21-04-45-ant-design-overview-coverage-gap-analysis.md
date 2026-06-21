# Ant Design components-overview coverage gap analysis

**Date**: 2026-06-21 04:45 +0200
**Author**: automated analysis
**Scope**: <https://ant.design/components/overview> (live) vs. the FS.GG Ant coverage matrix
[`docs/product/ant-design/coverage/ant-component-coverage.md`](../product/ant-design/coverage/ant-component-coverage.md)
**Posture reminder**: FS.GG adopts Ant Design **as a design language only** — no React/DOM/HTML/CSS.
"Missing aspect" below means a *design-language* gap (a component pattern or interaction idea not yet
dispositioned), never a missing React component or DOM mechanism.

## TL;DR

The repo's coverage matrix is **honest and complete for the snapshot it was taken against**
(`2026-06-16`, 70 components, all dispositioned). But Ant has shipped its **6.x** line since that
snapshot, and the live overview now lists **72 component entries plus one deprecation**. Three items
have drifted and are **not yet dispositioned** in our matrix:

1. **Masonry** — new in Ant **6.0.0** (Layout). Not in the matrix.
2. **BorderBeam** — new in Ant **6.4.0** (Other). Not in the matrix.
3. **List** — now marked **DEPRECATED** upstream (successor: *Listy*, v6). Our matrix still rows it as
   `existing`.

Everything else on the live overview maps 1:1 to existing matrix rows. No previously-dispositioned
component was renamed or removed (other than List's deprecation).

## Method

- Pulled the live components overview and the `llms.txt` index, grouped by category heading.
- Diffed the live component set against the 70 rows in `ant-component-coverage.md`.
- Confirmed `Masonry`, `BorderBeam`, and `border-beam` appear **nowhere** in `docs/`, `src/`, or
  `tests/` — they are genuinely absent, not merely unlisted in the matrix.
- Fetched each drifted component's own page to confirm version, purpose, and (for List) the
  deprecation note.

## Findings

### 1. Snapshot is one minor-train stale

The hub ([`reference/ant-llms-sources.md`](../product/ant-design/reference/ant-llms-sources.md)) owns
the retrieval date `2026-06-16` and is the single place that should be bumped on re-pull. The two
net-new components below were introduced in `6.0.0` and `6.4.0`, so our snapshot predates the Ant 6
line. This is the root cause of all three drift items — the matrix isn't *wrong*, it's *as-of an
earlier Ant*.

### 2. Masonry (net-new, Layout) — adoptable

> "A masonry layout component for displaying content with different heights" — responsive,
> configurable column counts and spacing; for galleries / irregular card grids.

This is a genuine **design-language pattern**, not a React mechanism. It is the column-packing layout
idea (cf. the existing `grid` / `wrap` / `stack` layout family). **Recommended disposition:**
`net-new` → a `masonry` layout control, or `composition` over the existing `grid`/`wrap` primitives if
a Yoga-backed column-packing arrangement can express it without a new control. Either way it belongs
in the Layout section of the matrix and the [`layout` pattern doc](../product/ant-design/patterns/layout.md).

### 3. BorderBeam (net-new, Other) — likely not-applicable, but note the accessibility idea

> "Renders a moving beam along a container border" — a purely **decorative animated effect** that
> "automatically hides when reduced-motion preferences are enabled."

BorderBeam carries no business semantics and is an animation/motion effect. **Recommended
disposition:** most likely `not-applicable` (decorative motion, no semantic-part model, no
interaction state) — but it surfaces a design-language concern worth capturing regardless: **a
reduced-motion policy**. Ant gates this effect on the OS reduced-motion preference. FS.GG has no
documented motion/reduced-motion stance today (see §5). Disposition this row *and* record the
reduced-motion idea even if the effect itself is declined.

### 4. List deprecation — matrix row needs a status note

Upstream: *"List component has been deprecated. Will be removed in the next major version,"* with a
forthcoming **Listy** successor (built-in virtual scrolling, richer layout) planned for v6. Our matrix
row 56 still reads:

```
| List | Data Display | existing | list-view | Component.Table.rowHoverBg | — |
```

`list-view` remains a perfectly valid FS.GG control — this is purely an **upstream-status** note, not a
removal. **Recommended action:** keep the row, change the rationale to flag the upstream deprecation
and the `Listy` successor to watch, so the next re-pull doesn't read this as undocumented drift.

### 5. Non-component "aspects" — what the overview offers beyond the component list

The `llms.txt` index also exposes design-language guidance the component matrix does not track. Most
are **mechanism** (CSS-in-JS, CSS Variable Plan, SSR, tree-shaking, the "Use with Vite/Next/Umi/…"
integration guides) and remain correctly **out of scope** under the design-language-only posture.

The genuinely design-language ones worth a deliberate disposition (today: largely implicit, not
documented as Ant mappings):

| Ant guidance area | FS.GG status today | Gap |
|---|---|---|
| Dark Mode / Preset Algorithm | `AntTheme.antLight`/`antDark` exist | Realized, but not cross-walked to Ant's algorithm in a pattern doc |
| Use Token / Modify Theme Token | `DesignTokensExt` + `StyleResolver` | Realized; the token *taxonomy* is mapped, the *theming workflow* is not |
| Motion / Transition / Reaction | — | **No documented motion or reduced-motion stance** (newly relevant via BorderBeam, §3) |
| Color Palette / Contrast | `ColorPolicy` (`wcag`/`ant`) | Well covered |
| Shadow / Elevation | `Elevation` tokens | Token exists; no Ant shadow-scale cross-map |

Of these, **Motion/reduced-motion** is the only true *blank* — the others are realized but
under-documented as explicit Ant mappings. Recommend tracking Motion as a small follow-up rather than
treating it as a coverage hole.

## Recommended actions (in priority order)

1. **Re-pull the Ant LLM snapshot** and bump the retrieval date in the hub
   ([`reference/ant-llms-sources.md`](../product/ant-design/reference/ant-llms-sources.md)) to capture the
   6.x line.
2. **Add two rows** to [`ant-component-coverage.md`](../product/ant-design/coverage/ant-component-coverage.md):
   `Masonry` (Layout, `net-new` or `composition`) and `BorderBeam` (Other, `not-applicable`, with the
   reduced-motion note in the rationale).
3. **Annotate the List row** with the upstream-deprecation / `Listy`-successor note.
4. **Update the honesty-check test** `tests/Controls.Tests/Feature132CoverageMatrixTests.fs` — it pins
   the total at **70**; adding the two rows moves it to **72**, and the test will (correctly) fail until
   both the matrix and the pinned count are updated together.
5. **Optionally** open a small motion / reduced-motion design-language note (§5), prompted by
   BorderBeam.

## What is *not* a gap

- All 18 Data Entry, all 20 Data Display (modulo List's status), all 11 Feedback, all 7 Navigation, and
  the 4 General components map cleanly to existing matrix rows.
- The composition dispositions (Dropdown, Form, Mentions, Transfer, Message, Notification) and the
  not-applicable trio (App, ConfigProvider, Util) remain correct — App/ConfigProvider/Util are still
  pure React/mechanism with no UI-control analog.
- No chart-family drift was assessed here; the chart coverage matrix
  ([`coverage/ant-chart-coverage.md`](../product/ant-design/coverage/ant-chart-coverage.md)) is governed by
  its own snapshot and honesty check and is out of scope for this report.
</content>
</invoke>
