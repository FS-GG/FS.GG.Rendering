# Provisional physical skill-source observation

Run from the repository root:

```sh
dotnet fsi tests/skill-staging-physical/PhysicalTests.fsx
dotnet fsi tests/skill-staging-physical/PinnedTests.fsx
dotnet fsi tests/skill-staging-physical/CapturedPlanTests.fsx
dotnet fsi tests/skill-staging-physical/ManifestCaptureTests.fsx
dotnet fsi tests/skill-staging-physical/DirectoryModeTests.fsx
dotnet fsi tests/skill-staging-physical/OutputModeTests.fsx
dotnet fsi tests/skill-staging-physical/CrossRootInstantTests.fsx
python3 tests/skill-staging-physical/PythonContrast.py
```

On Linux, `SkillStagingPhysical.fsx` opens the checkout root and each source
component relative to held directory descriptors with `O_NOFOLLOW`. It
enumerates directories by descriptor, opens children without following links,
reads regular-file bytes through held descriptors, and then passes those bytes
to the pure policy in `skill-staging-policy.fsx`. It refuses symlinks,
nonregular entries, and case aliases, including empty directory aliases. Two
directory scans and two file reads with descriptor metadata checks refuse
observed mutations. Other platforms fail closed with
`source-platform-unsupported`.

The disposable fixtures contrast the lexical F# seam with the live Python
stager's outside-link containment check. The original physical test reads all
20 checked-in product rows without staging. The pinned test demonstrates the
old path-check/read race, then swaps paths before and after descriptor open to
show no-follow refusal or original opened bytes. FIFO and case-alias controls
are independent negatives.

This is a read-only capture, not a staging or packing path. A path can change
after capture, and no later copier is bound to these byte arrays. The two
scans/checks do not prove an atomic snapshot against adversarial ABA or all
concurrent in-place writes. #1332 Python preflight acceptance, #1334 source
acceptance, and installed-package parity remain prerequisites for any live
receiver decision. This draft does not authorize a release or cutover.

The stacked [captured plan](CapturedPlan.md) owns byte copies and source modes
for a possible later copy adapter. It still writes no output.

The further stacked [manifest capture](ManifestCapture.md) pins the manifest
read before passing its bytes to that plan.

The [directory plan](DirectoryMode.md) retains empty directories and source
modes as read-only facts for a later copy adapter.

The [output mode contract](OutputMode.md) projects a successful fresh target
under an explicit umask and checks the current Python policy without staging.

The [cross-root fixture](CrossRootInstant.md) refuses persistent manifest
drift between pinned manifest and product-root captures. It does not prove an
atomic snapshot or exclude adversarial ABA.
