# FSI Authoring Evidence

This folder records the public Core surface exercise for `samples/SecondAntShowcase`.

- `second-ant-showcase-authoring.fsx` loads the built `SecondAntShowcase.Core` assembly and
  exercises coverage, interaction contracts, visual config, and review finding APIs through
  their `.fsi` signatures.
- Run it after building the sample:

```sh
dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release
dotnet fsi specs/171-second-antshowcase-sample/readiness/fsi/second-ant-showcase-authoring.fsx
```

This is API-shape evidence, not live visual acceptance evidence.
