# Package Surface

- Public framework surface: no `.fsi` changes to `FS.GG.UI.SkiaViewer` or `FS.GG.UI.Controls.Elmish`.
- Sample Core surface: changed intentionally in `InteractionContracts.fsi` and `Evidence.fsi` to add responsiveness action/evidence fields.
- Reviewed sample surface baseline updated: `specs/171-second-antshowcase-sample/readiness/surface-baselines/SecondAntShowcase.Core.txt`.
- Surface refresh command: `dotnet fsi scripts/refresh-surface-baselines.fsx`.
- Surface refresh exit code: `0`.
- Readiness allowlist proof: `git check-ignore -v specs/172-fix-mouse-lag/readiness/full-validation.md` reports `.gitignore:87:!specs/172-fix-mouse-lag/readiness/**`.

Log: `specs/172-fix-mouse-lag/readiness/logs/surface-baselines.log`
