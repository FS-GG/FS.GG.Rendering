# US5 AntShowcase Parity

Prerequisite:

- Local packages packed to `~/.local/share/nuget-local`.

Preferred command:

```sh
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/164-shared-visual-readiness/readiness/antshowcase-preferred
```

Result: `blocked`, screenshots `38/38`. The block is expected because reviewer rows are pending.

Minimum command:

```sh
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --pages data-collections,charts-statistical,charts-advanced,feedback-status,tpl-form,tpl-exception --out specs/164-shared-visual-readiness/readiness/antshowcase-minimum
```

Result: `blocked`, screenshots `12/12`. The block is expected because reviewer rows are pending.

Generated artifacts:

- `summary.json` and `summary.md` for preferred and minimum runs.
- `contact-sheets.json` for shared contact-sheet metadata.
- Per-theme contact sheet PNGs remain sample-owned.
- `reviewer-defects.md` is generated from `FS.GG.UI.Testing.VisualReviewerClassifications`.

Generic workflow centralization:

- Before: AntShowcase owned matrix expansion, reviewer template/parsing, readiness decision, summary rendering, and whole-file summary rewrite.
- After: `FS.GG.UI.Testing` owns those generic behaviors; AntShowcase still owns page registry, theme selection, screenshot capture, and contact-sheet PNG composition.
- Approximate centralization: 6 of 8 generic workflow responsibilities moved to shared helpers, about 75%.
