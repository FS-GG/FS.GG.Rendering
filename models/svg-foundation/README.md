# Retained interaction model

`retained-interaction.md` is the single editable profile-2 authority for the retained SVG reducer.
Its explicit bindings are in `retained-interaction.bindings.json`; installed SDD 1.7.0 extracts the
committed evidence under `readiness/svg-qual-01-2/`. The extracted `.qnt` is generated evidence and
must not be edited directly.

The model binds every public retained interaction message to a Quint action: Select, ReplaceScene,
ClearSelection, FocusNext, FocusPrevious, SetCamera, CapturePointer, and ReleasePointer. A fixed-seed
bounded Quint run emits ITF witnesses, and `extract-retained-traces.py` converts those model states
into `retained-interaction.traces.tsv`. The isolated .NET and Fable/Node package consumers replay that
same corpus through `SvgRetained.update`, compare every resulting projection, and report the first
divergence. Mutated Select-to-ClearSelection dispatch and stale-error acceptance must both diverge.

Run the public installed qualification with exact Quint and lmt objects:

```console
QUINT_BIN=/path/to/quint-linux-amd64 \
LMT_BIN=/path/to/lmt \
tests/svg-foundation/qualify-retained-profile.sh /tmp/svg-retained-qualification.json

dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter "SVG foundation retained scene"
bash tests/Scene.PortableConsumers/run.sh
```

The qualification installs `FS.GG.SDD.Cli` 1.7.0 from nuget.org, then disables network access. It
authors and inspects twice in scratch, compares both results with the committed extraction, runs the
exact extracted module's tests and two identical bounded runs, compares the generated trace corpus,
and proves stale source ranges and stale action declarations fail without writing authority.

The evidence is bounded to 12 steps, 16 traces, two selectable identities, small revision and pointer
domains, and integer camera samples. It proves correspondence for those witnesses and transition
classes. It does not establish temporal liveness, exhaustive state-space coverage, or numerical
equivalence for floating-point camera values; production tests separately cover finite-value
validation, and the browser suite stays at the rendering/effect boundary.
