# Quickstart: Validating Lookless Slot Composition (Feature 095)

This is a **backfill** — the code, the suite, and the readiness evidence already exist. This guide
shows how to **run and read** the 095 validation, not how to build the feature. Validation is the
conformance check that the suite is green, the parity oracle matches, and the public-surface delta is
zero.

## Prerequisites

- .NET SDK with `net10.0` (per `Directory.Build.props`).
- Repo root: `/home/developer/projects/FS.GG.Rendering`.
- No GL context / display required — every 095 proof is deterministic and headless (record /
  `Children` comparison, structural scene equality, and the in-process keyed reconciler).

## 1. Build the library

```bash
dotnet build src/Controls/Controls.fsproj
```

Expected: clean build. `Controls.fsproj` declares `<InternalsVisibleTo Include="Controls.Tests" />`,
so the suite can reach the internal slot seam (`slotFill`/`slotFillsOf`/`slotFor`/`lowerSlots`).

## 2. Run the Feature 095 suite

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~095"
```

Expected: all seven test lists pass. What each proves (see `contracts/slot-composition.md` §4 for the
full clause mapping):

| Test list | Proves | SC |
|---|---|---|
| `slotPlacement` | `Button.Leading`/`Trailing` land in two distinct ordered regions; `Panel` children order `[header; body; footer]`; `slotFor` resolves present, distinguishes absent from empty | SC-001 |
| `loweringProperties` | purity/determinism + totality over ≥1000 generated `(kind, fills)` cases (`Gen095`); no-slot ⇒ identity | SC-005 |
| `typedClosure` | no public free-form `Attr.slot` / slot-name escape hatch — typed props only | SC-006 |
| `unfilledParity` | unfilled `Button` byte-identity + structural scene equality to the frozen baseline (light + dark); unfilled `Panel` == legacy; `CheckBox` gains no slots | SC-002, SC-007 |
| `compose` | a binding inside a slot dispatches (E1); a `Danger`-classed slotted control resolves via the 093 resolver (E3); a focusable slotted control appears in `Focus.order` (E4) | SC-003 |
| `retainedIdentity` | a keyed slotted control keeps its `RetainedId` across a position-shifting re-render via the live `RetainedRender` path, and the stepped scene equals a full rebuild | SC-004 |
| `evidence` | writes/confirms the `readiness/parity/*.scene.txt` frozen-oracle scenes | SC-002 |

## 3. Confirm zero public-surface delta

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Surface"
```

Expected: pass **unchanged**. The lone public entry, `SlotFillsValue`, is already committed at
`tests/surface-baselines/FS.GG.UI.Controls.txt`
(`FS.GG.UI.Controls.AttrValue\`1+SlotFillsValue`). Backfilling the spec adds no new baseline delta;
the slot seam is `internal` and the typed props are the authoring path.

## 4. Read the readiness evidence

```text
specs/095-lookless-slot-composition/readiness/parity/
├── button.light.normal.scene.txt   # frozen pre-slot procedural oracle (light theme)
└── button.dark.normal.scene.txt    # frozen pre-slot procedural oracle (dark theme)
```

`unfilledParity` asserts an authored-but-unfilled `Button` renders structurally scene-equal to these
frozen scenes under both themes. The evidence is **structural scene equality**, not pixels or
desktop visibility — that limitation is in scope-of-proof and disclosed here. `Panel`'s unfilled case
is proven by direct `lowerSlots`-identity (no frozen scene yet — bounded follow-up DF-2).

## What this validation does NOT cover

- Pixel-level rendering or on-screen visibility (out of scope; structural equality only).
- A frozen parity scene for `Panel` (DF-2 — `Panel`'s no-slot case is covered by `lowerSlots`-identity).
- Data-bound slot templates, selectors, specificity, or cascade — permanent non-goals (FR-008);
  styling is feature 093, runtime state derivation is feature 096.
