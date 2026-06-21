# Contract: Surface Invariance (binding for all six stories)

This is the single overarching contract of Feature 182. Every story inherits it. It encodes the Tier 2
guarantee: **the public surface and observable behavior of every touched package do not change.**

## C-SI-0 — Resolved interpretation (maintainer decision, 2026-06-21)

C-SI-1 (each `.fsi` byte-identical) and the feature's goal **conflict**: in F#, public types
declared in a package's `.fsi` are bolted to the residual `.fs` (compiled last), and the
implementation references them pervasively — so extracting implementation into earlier-compiled files
would require a back-edge unless the public type/val *signatures* are also moved into a new paired
`.fsi`/`.fs` in the same package+namespace. A strict C-SI-1 reading therefore blocks almost all
extraction (mass FR-009).

**Resolved (maintainer):** the binding oracle is the **assembly surface** — the 12
`readiness/surface-baselines/*.txt` baselines (the SC-001 `git diff --exit-code` gate) **plus** the
public-surface *union*. Moving a public type/val signature into a new paired `.fsi`/`.fs` **within the
same package and namespace** is permitted: consumers, samples, the template, and generated products see
**zero** change (same namespace, same type, same shape), and the `.txt` baselines stay byte-identical.
Individual `.fsi` *files* may change content as long as the assembly surface union is byte-stable.
C-SI-1 below is read as "the public-surface union is frozen," not "each `.fsi` file is frozen byte-for-byte."

## C-SI-1 — Public `.fsi` is frozen

For each touched package, its companion `.fsi` file(s) MUST be **byte-identical** before and after the
feature:

- `src/SkiaViewer/SkiaViewer.fsi`
- `src/Controls/Control.fsi`, `src/Controls/RetainedRender.fsi`
- `src/Scene/Scene.fsi`
- `src/Testing/Testing.fsi`
- `src/Controls.Elmish/ControlsElmish.fsi`

No public symbol may change **name, namespace, module path, or signature** (FR-002). Verified by
`git diff --exit-code` on these files.

## C-SI-2 — Surface baseline is frozen (the automated oracle)

`dotnet fsi scripts/refresh-surface-baselines.fsx` regenerates `readiness/surface-baselines/*.txt` from
the built assemblies. After every story:

```
git diff --exit-code readiness/surface-baselines/      # MUST be empty
```

A required edit to `FS.GG.UI.{SkiaViewer,Controls,Scene,Testing,Controls.Elmish}.txt` means a split
leaked surface — it is a **defect**, not a baseline to update (FR-002, SC-001). The live gate
`tests/Package.Tests/SurfaceAreaTests.fs` + `build/Governance/PackageSurface.fs` read the same dir, so
a green diff is authoritative.

## C-SI-3 — Visibility lives in `.fsi` (Constitution II)

- No `private` / `internal` / `public` modifier on any top-level `.fs` binding.
- Extracted concern files declare visibility via `module internal X` or a new **internal** `.fsi` for
  that file (one that contributes nothing to the package's external surface union).
- The union of public surface across all split files MUST equal the pre-split `.fsi` exactly (C-SI-1).

## C-SI-4 — Compile order, no new cycle (FR-010)

- New files are inserted into the `.fsproj` `<Compile Include>` order **before** the residual god-file,
  so the residual file references extracted modules forward (F# file-order rule).
- No new back-edge, no new inter-project reference, no new project, no new package dependency. The
  dependency graph stays acyclic and unchanged (SC-007).
- A seam that would require a back-edge or reorder a public symbol's definition site is **out of
  scope** for that family and retained as-is per **C-SI-6**.

## C-SI-5 — Behavior is byte-stable (FR-003)

All rendered output, evidence/readiness artifacts (MD+JSON), viewer screenshots/observations, scene
hashes/fingerprints, damage regions, and CLI/harness output are byte-identical to baseline for every
touched subsystem. The regenerate-and-diff sweep in [../quickstart.md](../quickstart.md) is the gate.

## C-SI-6 — FR-009 retention is allowed and must be recorded

Any target or sub-seam whose split would change surface (C-SI-1/2), change output (C-SI-5), require a
back-edge (C-SI-4), or harm legibility MUST be **left in its current form** with the exclusion and its
rationale recorded in that story's contract / the plan's Implementation Outcome — never forced. Size
targets (SC-005) are goals, not hard rules.

## C-SI-7 — Red/green parity, no weakening (Constitution V, FR-008)

`dotnet build` + the full `*.Tests.fsproj` sweep are green at each story end, with the **same**
red/green set as the captured baseline (known pre-existing reds unchanged). No assertion is weakened
and no test deleted to green a build; tests blocked out-of-scope are skipped with written rationale.
