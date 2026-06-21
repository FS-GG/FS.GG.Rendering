# Quickstart / Validation Guide: Shared Test/Util Helpers

This is a behavior-preserving refactor. "Validation" means proving the build and full test suite stay
green and that each duplicate is gone. Run these from the repository root.

## Prerequisites
- .NET `net10.0` SDK; the repo builds via `FS.GG.Rendering.slnx`.
- A clean working tree on `178-shared-test-util-helpers`.

## 0. Baseline capture (before any edit)
```bash
dotnet build FS.GG.Rendering.slnx
dotnet test  FS.GG.Rendering.slnx
```
Record the result. Expect green **except** the two documented package-feed reds
(`tests/Package.Tests`, `samples/ControlsGallery/ControlsGallery.Tests`). Those are the environmental
baseline (SC-001) — not regressions.

## 1. Repo-root finder (User Story 1)
After migrating every test/harness finder to `FS.GG.TestSupport.RepositoryRoot`:
```bash
dotnet test FS.GG.Rendering.slnx          # path-dependent tests resolve the same root
# zero remaining local finders (SC-002):
grep -rn --include='*.fs' -e 'let rec findRepositoryRoot' -e 'let findRepositoryRoot' \
     -e 'FS.GG.Rendering.slnx' tests/ | grep -v 'tests/TestSupport/'
```
Expected: tests green; the grep returns **nothing** (only `tests/TestSupport` defines the finder).
Fail-loud check (Acceptance #3): from a directory with no marker above it, `RepositoryRoot.find`
raises with an actionable message.

## 2. FNV primitive (User Story 2)
After routing the four `src/Controls` folds through `Internal/Hashing`:
```bash
# byte-identity via the relational suites (no absolute-constant assertions):
dotnet test FS.GG.Rendering.slnx --filter 'FullyQualifiedName~Feature159'
dotnet test FS.GG.Rendering.slnx --filter 'FullyQualifiedName~Fingerprint'
# one offset-basis literal site (SC-003):
grep -rn --include='*.fs' '0xcbf29ce484222325UL' src/ | grep -v 'Internal/Hashing.fs'
```
Expected: identity/reuse/promotion + fingerprint tests green; the grep returns **nothing**.

## 3. clamp (User Story 3)
After routing the three `src` sites through `Numeric.clamp`:
```bash
dotnet test FS.GG.Rendering.slnx --filter 'FullyQualifiedName~Layout|Caret|Viewer'
# one clamp definition (SC-004):
grep -rn --include='*.fs' 'let clamp\|let inline clamp' src/ | grep -v 'Shared/Numeric.fs'
```
Expected: clamped-behavior tests green; the grep returns **nothing** (`clampNonNegative` is a
different name and does not match).

## 4. Surface & package invariants (all stories)
```bash
git diff -- '*.fsi'                        # expect: no published-surface signature change (SC-005)
dotnet test FS.GG.Rendering.slnx --filter 'FullyQualifiedName~SurfaceArea|ApiReference'
```
Expected: no `.fsi` diff to published modules; surface-area / API-reference baselines green.

## 5. Independence check (SC-006)
Each consolidation is a separate change unit; reverting any one leaves the other two building and
green. Verified by keeping the three migrations as independent commits/units.

## Reference
- Helper contracts: [`contracts/`](./contracts/) — repo-root-finder, fnv-hash-primitive, clamp.
- Entities & invariants: [`data-model.md`](./data-model.md).
- Divergence reconciliation & placement rationale: [`research.md`](./research.md).
