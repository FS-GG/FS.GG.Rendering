# Retained interaction model

`retainedInteraction.qnt` models the shared-state revision and selection rules in
`src/Scene/RetainedSvg.fs`. One reducer call is one atomic transition. Browser events, rendering,
pointer coordinates, and game-command policy are outside this bounded model.

The bound uses revisions 0–3, two selectable identities (1 and 2), identity 0 for no selection,
and an unavailable identity outside that set. It witnesses current-revision selection,
stale-revision rejection, selection preservation when an identity survives a newer revision, and
selection clearing when it does not. `SvgFoundationRetainedTests.fs` enumerates the corresponding
F# state/command matrix and contains a mutation witness that fails when stale selection is applied.

Run the model with Quint 0.32.0:

```console
quint typecheck models/svg-foundation/retainedInteraction.qnt
quint typecheck models/svg-foundation/retainedInteractionTest.qnt
quint test models/svg-foundation/retainedInteractionTest.qnt --main retainedInteractionTest
quint run models/svg-foundation/retainedInteraction.qnt --main retainedInteraction --invariant revisionNeverDecreases --witnesses currentSelectionWitness staleSelectionWitness --max-steps 20
```

The model uses the default `init` and `step` actions. These direct runs are bounded design evidence;
they do not qualify the unavailable `fsgg-quint-profile/2` cache recorded by SVG-FOUND-01.1.

Correspondence:

- `reduceSelect` ↔ `SvgRetained.update (RetainedInteractionMessage.Select ...)`
- `reduceReplace` ↔ `SvgRetained.update (RetainedInteractionMessage.ReplaceScene ...)`
- `retainedInteractionTest.qnt` ↔ `tests/Scene.Tests/SvgFoundationRetainedTests.fs`
