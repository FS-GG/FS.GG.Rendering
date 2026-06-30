# T017 — Non-game profiles unaffected (FR-005, SC-003)

Re-instantiated from the **feed** package with `--name Product` (matching the Feature-220 baseline
capture) and `diff -rq`'d generated `src` + `tests` against
`specs/220-game-starter-scene/readiness/fr007-baseline/<profile>/`:

```
headless-scene: ✅ EMPTY DIFF (src+tests byte-identical to F220 baseline)
governed:       ✅ EMPTY DIFF (src+tests byte-identical to F220 baseline)
sample-pack:    ✅ EMPTY DIFF (src+tests byte-identical to F220 baseline)
app:            ✅ controls-showcase product (View/Model/EvidenceCommands/LayoutEvidence/…)
```

**F221 attribution (the release also carries Feature 221, landed between 220 and 222).** F221's only
change to generated template output is `template/base/docs/evidence-formats.md` (3 ins / 1 del) —
**outside** `src`/`tests`. The Feature-220 byte-diff baseline captures `src`+`tests` only, so byte
identity holds: the three profiles are unchanged, and any whole-tree delta vs the F220 snapshot is the
single F221 doc line, **not** the `game` profile. FR-005 / SC-003 ✅
