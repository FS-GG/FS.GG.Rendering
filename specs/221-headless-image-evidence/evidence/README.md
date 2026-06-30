# Evidence — Feature 221 (Headless Image Evidence Path)

Committable pixel/timing proofs. Unlike `../readiness/`, this directory is **not** gitignored.

| Artifact | Task | Proves |
|---|---|---|
| `representative-game-scene.png` | T008/SC-001 | A real, decodable **800×600 RGBA PNG** of the pinned scene, rendered with **no GPU/GL/X/display**. |
| `generate-headless-png.fsx` | T008/T014 | Reproducible generator: renders the fixture through the public `SceneEvidence.renderPng` surface, checks byte-identity, measures timing. |
| `timing.md` | T014/SC-004 | Measured single-render time (median **11.9 ms**) vs the 5 s CI bound. |
| `us2-live-frame.md` | T017/SC-003 | Live-window (`OffscreenReadback`) capture proof — `environment-limited` here (no GL), with disclosed substitute. |

## Reproduce

```bash
dotnet restore tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --force-evaluate /p:RestoreLockedMode=false
dotnet build   tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-restore -c Debug
dotnet fsi specs/221-headless-image-evidence/evidence/generate-headless-png.fsx
git checkout -- src/*/packages.lock.json tests/*/packages.lock.json   # revert the force-evaluate lockfile churn
```
