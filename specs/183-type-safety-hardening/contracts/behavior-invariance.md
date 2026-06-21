# Contract: Behavior Invariance + Intentional Exact Surface (FR-005 / FR-006)

The binding oracle for all three stories. Phase 6 is **Tier 1**, so unlike features 177–182 the gate is
**not** "surface byte-identical." It is two separate, simultaneously-binding invariants.

## A. Behavior is byte-stable (FR-005) — non-negotiable

For every touched subsystem, the following MUST be byte-identical to a baseline captured immediately
before the change (`specs/183-…/readiness/baseline/`):

1. **`SceneNode` codec wire bytes + round-trip values** — for a fixed corpus of representative scenes
   covering all 25 cases. Serialize → compare bytes; deserialize → structural equality. (US2 primary gate.)
2. **Rendered output** — scene trees, scene hashes, fingerprints for the control/scene corpus. (US1/US2.)
3. **Evidence / readiness artifacts** — Markdown + JSON emitted by the touched paths. (US1/US3.)
4. **Damage regions + diagnostics** — `damageRegion`, `validateDamage`, `classifyWindowObservation`,
   `promotionDecision`, `damageRegionSet` outputs for fixed inputs. (US3.)
5. **Full test red/green set** — the Release `*.Tests.fsproj` sweep reproduces the **same** result as
   baseline: the 2 known reds (`Package.Tests` 8-fail, `ControlsGallery` 2-fail) and 14 greens, same
   counts (SC-006). A new red is a regression.

No assertion may be weakened, deleted, or `--accept`-overridden to green a build. Any observed behavior
difference means the refactor changed semantics → fix it or retain that part per FR-010; never baseline
it forward.

## B. Surface change is intentional, minimal, and exact (FR-006)

- The **only** packages whose public surface may change are `FS.GG.UI.Scene` and `FS.GG.UI.SkiaViewer`.
- After `dotnet fsi scripts/refresh-surface-baselines.fsx`, `git diff readiness/surface-baselines/` MUST
  show changes **only** in `FS.GG.UI.Scene.txt` and/or `FS.GG.UI.SkiaViewer.txt` (new flag-record /
  `DamageNodeCounts` type names), and the `.fsi` git diff MUST show **only** the planned signature/DU
  field-name edits. `FS.GG.UI.Controls.txt` and the other 9 baselines MUST be unchanged.
- US1 and the `internal`/`private` flag functions MUST leave every public baseline unchanged (an
  accidental public promotion is a defect even in a Tier-1 feature).
- The surface-baseline `.txt` is **type-name level**; the DU field-naming may produce **no** `.txt`
  change at all — in that case the `.fsi` git diff is the reviewed record. Either way the diff is read
  and confirmed to contain nothing unplanned.

## C. Per-story acceptance matrix

| Story | Behavior gate (A) | Surface gate (B) | Bump |
|---|---|---|---|
| US1 Kind registry | scene-hash/fingerprint/inspection/a11y/virtualization byte-identical | **no** baseline/`.fsi` change | none |
| US2 codec + DU | **codec wire bytes identical** + every-case round-trip identity + scene hashes | `Scene.fsi` field names (+ any `.txt`); nothing else | Scene |
| US3 flag records | damage/diagnostic/promotion outputs identical for fixed inputs | `Scene.txt`+`SkiaViewer.txt` new record types only; Controls unchanged | Scene, SkiaViewer |

## D. Capture & diff commands

```bash
# BEFORE any edit (Foundational):
dotnet build FS.GG.Rendering.slnx -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx          # snapshot 12 baselines (git clean after)
DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/183-type-safety-hardening/readiness/baseline/test-baseline.md
# + capture codec bytes / scene hashes / damage regions for the corpus into baseline/

# AFTER each story:
dotnet build FS.GG.Rendering.slnx -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff -- readiness/surface-baselines      # must match B (only Scene/SkiaViewer, only planned)
git diff -- 'src/**/*.fsi'                    # the fine-grained intentional record
DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/183-type-safety-hardening/readiness/post-change/test-baseline.md
# + re-capture corpus, diff bytes/hashes/regions == baseline (A)
```
