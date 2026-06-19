# Regression Validation

Status: `accepted`

Preservation checks:

- AntShowcase package-only restore behavior is preserved by checking all selected sample projects for
  `FS.GG.UI.*` package pins and rejecting direct framework `src/` project references.
- Existing package validation evidence remains separate from Feature 163 package proof artifacts.
- Existing Rendering.Harness compositor/readiness commands remain in `Cli.fs`; Feature 163 adds
  `package-feed` and `validation-lanes` without replacing previous command handlers.
- Feature 160/161 focused lane and readiness behavior remains covered by existing tests, with
  Feature 163 lane summary tests asserting that focused lane success and aggregate validation are
  reported separately.
- Concurrent output isolation is covered by `Feature163ValidationLaneTests`: default lanes use
  distinct output roots and log/result paths.
- The focused lane run wrote lane-specific TRX files under `lanes/*/TestResults/` and per-lane
  logs/results under `lanes/<lane>/`.
- The aggregate full-solution lane was intentionally left unselected in focused readiness evidence;
  the readiness summary records it separately instead of treating it as green.
