# Provisional physical skill-source observation

Run from the repository root:

```sh
dotnet fsi tests/skill-staging-physical/PhysicalTests.fsx
python3 tests/skill-staging-physical/PythonContrast.py
```

`SkillStagingPhysical.fsx` reads one source tree and passes its observed bytes to
the pure policy in `skill-staging-policy.fsx`. It refuses symbolic links and
reparse points at every source-directory component and enumerated entry,
including in-tree links, dangling links, and unlisted sidecars. The disposable
fixtures contrast its refusal with the lexical F# `sourceDirectory` seam and
the live Python stager's outside-link containment check. The F# test also
reads every checked-in product row through the adapter without staging it.

This is an observation-time adapter, not a staging or packing path. A path can
change between link inspection and byte read, and the result is not bound to a
later copy. It cannot establish race-free source custody or installed parity;
that needs handle-bound no-follow reads or an immutable validated snapshot.
