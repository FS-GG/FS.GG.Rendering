# Provisional F# release preflight reducer

`Plan.inspect` accepts the raw release-plan JSON, a requested target version,
and package identities already enumerated from packable projects at one exact
source commit. It refuses duplicate JSON properties, malformed or case-alias
IDs, missing or extra packages, wrong package kinds, version mismatch, and a
baseline that does not precede the target. The source facts themselves must be
19 distinct identities: 17 libraries, the `FS.GG.UI` BOM, and the template.
The plan must also contain the ordered release tags for the requested target:
`fs-gg-ui/v<version>`, `fs-gg-ui-template/v<version>`, and `v<version>`.

This is a source-only FSC-06 candidate stacked on Python preflight repair
Rendering PR #1332. The Python gate remains authoritative. The adapter must
read project XML from the exact source SHA, bind the requested version and
workflow identity, observe authenticated feed collision state, and write the
receipt. Archive payload and mode readback, partial-publication recovery,
package release, and receiver adoption are outside this reducer.

Run the independent source controls with:

```sh
dotnet restore tests/FS.GG.Rendering.ReleasePreflight.Tests/FS.GG.Rendering.ReleasePreflight.Tests.fsproj --locked-mode
dotnet run --project tests/FS.GG.Rendering.ReleasePreflight.Tests/FS.GG.Rendering.ReleasePreflight.Tests.fsproj -c Release --no-restore
```

## Selected palette-heading successor

`SelectedSuccessor.inspect` is the maintained interpretation boundary for the closed
`eng/release/palette-heading-0.32.4.json` selection. The actual release source guard
invokes `scripts/release-selected-successor.fsx` with plan bytes and package facts
read from the exact Git source. It reuses `Plan.inspect` and permits source qualification
only with baseline0.32.3 and explicit false readiness. It refuses publication: no real
0.32.4 original archive custody and receiver tuple has been acquired yet. No historical
admission counts, producer IDs, or credential authority are inferred from source success.
The provisional generic reducer does not replace historical publication admission.
