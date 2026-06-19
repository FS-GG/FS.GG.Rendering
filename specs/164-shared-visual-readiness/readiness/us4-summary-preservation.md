# US4 Summary Preservation

Command:

```sh
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature164
```

Result: passed, 8 tests.

Covered:

- Missing managed markers are inserted deterministically.
- Repeated regeneration preserves manual text before and after the managed section.
- Multiple, reversed, and one-sided markers fail safely without returning writable content.

Runtime exercise:

```sh
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- visual-readiness --summarize specs/164-shared-visual-readiness/readiness/antshowcase-preferred --minimum-size specs/164-shared-visual-readiness/readiness/antshowcase-minimum --out specs/164-shared-visual-readiness/readiness
```

Result: passed; `validation-summary.md` contains the managed section markers and preserves manual notes outside them.
