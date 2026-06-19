# Lane List Evidence

Command:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --list
```

Output:

```text
build	required	00:10:00	Build verification for the solution.
library-tests	required	00:10:00	Fast library and package validation not tied to one sample.
package-proof	required	00:10:00	Package pin and local-feed source proof for package-consuming samples.
controls	required	00:15:00	Controls package and rendering-control behavior validation.
rendering-harness	required	00:10:00	Rendering harness contracts, package-feed helpers, and lane runner tests.
antshowcase-sample	required	00:10:00	Package-consuming AntShowcase sample validation.
aggregate-solution	optional	00:20:00	Full solution validation recorded separately from focused lanes.
```
