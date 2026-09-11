# SVG-FOUND-01.2 portable scene evidence

`FS.GG.UI.Scene` now owns an additive retained SVG contract over its existing `Scene` and
`SceneNode` vocabulary. The contract keeps root, layer, and semantic object identities; defines
camera meaning as `screen = pan + zoom * scene`; validates finite values; classifies every first-cut
node as rendered, unsupported, or invalid; and provides revision-aware selection, focus, camera,
and pointer-capture transitions. Game policy and browser event normalization remain outside it.

The NuGet package's curated `fable/` view contains only `Types.fsi/fs`, `RetainedSvg.fsi/fs`, and
its metadata project. The full .NET assembly remains unchanged in scope. The isolated consumer gate
packs `FS.GG.UI.Scene` to a temporary local feed, copies two consumers away from this checkout, and
proves a .NET build/run and Fable 5.17.0 compile without project references, linked sibling sources,
Skia, SkiaViewer, Controls.Elmish, or Fable.Browser.Dom.

The new runtime dependency is development-only `Fable.Package.SDK` 1.4.0 (`PrivateAssets=all`). Its
packed license is MIT, copyright Mangel Maxime (2024), and the inspected nupkg SHA-256 is
`54837b5d2e7ba6b1ba36467467653b4d6d209c4a0355757c85d2d3514f3e20fb`. Test-only Fable compiler and
Fable.Core artifacts are restored from NuGet and are not copied into `FS.GG.UI.Scene`. No S.I.R.
source, asset, or third-party donor material is included.

Run the evidence:

```console
dotnet test tests/Scene.Tests/Scene.Tests.fsproj
bash tests/Scene.PortableConsumers/run.sh
quint test models/svg-foundation/retainedInteractionTest.qnt --main retainedInteractionTest
quint run models/svg-foundation/retainedInteraction.qnt --main retainedInteraction --invariant revisionNeverDecreases --witnesses currentSelectionWitness staleSelectionWitness --max-steps 20
```

The Quint model is bounded to revisions 0–3 and two selectable object identities. The F# suite
enumerates the corresponding reducer matrix and includes a stale-revision mutation witness. The
installed SDD profile-2 object cache remains unavailable as recorded by SVG-FOUND-01.1; these direct
Quint 0.32.0 witnesses do not change that qualification result.
