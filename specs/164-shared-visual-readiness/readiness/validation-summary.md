# Feature 164 Visual Readiness Summary

## Manual Validation Notes

- Shared visual-readiness helpers are implemented in `FS.GG.UI.Testing`.
- AntShowcase preferred evidence produced 38/38 screenshots.
- AntShowcase minimum evidence produced 12/12 screenshots.
- Both AntShowcase runs are intentionally `blocked` until reviewer classifications are completed; the shared report records this as `pending-review`.
- Root `./fake.sh` is absent in this checkout, so direct `dotnet` substitutes are recorded in the readiness files.

<!-- FS.GG VISUAL READINESS START -->
## Generated Visual Readiness Links

- preferred evidence: `specs/164-shared-visual-readiness/readiness/antshowcase-preferred`
- minimum-size evidence: `specs/164-shared-visual-readiness/readiness/antshowcase-minimum`
- package-feed validation: `package-feed.md`
- compatibility ledger: `compatibility-ledger.md`
- regression validation: `regression-validation.md`
- full validation: `full-validation/validation.md`
<!-- FS.GG VISUAL READINESS END -->

## Manual Caveats

- `dotnet test template/base/tests/Product.Tests/Product.Tests.fsproj` is currently blocked by pre-existing template/base compile errors unrelated to Feature 164.
- Local package packing completed with existing missing-readme warnings on several package projects.
