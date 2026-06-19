# FSI Contract Transcript

Command:

```sh
dotnet build tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore
dotnet fsi --quiet
```

FSI input exercised the public `Rendering.Harness.ValidationLanes` surface from
`tests/Rendering.Harness/bin/Release/net10.0/Rendering.Harness.dll`.

Output:

```text
status=no-progress-timeout
readiness=blocked
request-list-only=false
lane-count=7
roles=build:required,library-tests:required,package-proof:required,controls:required,rendering-harness:required,antshowcase-sample:required,aggregate-solution:optional
selected=rendering-harness
```
