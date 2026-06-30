# Representative game scene fixture (T006)

The single, concretely-pinned fixture all determinism/non-blank/timing tasks reference by name, so
FR-003/SC-001/SC-002/SC-004 rest on a fixed, reproducible input.

## Identity

| Property | Value |
|---|---|
| Constructor | `HeadlessImageEvidenceTests.representativeGameScene ()` (`tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs`) |
| Output size | `representativeGameSceneSize = { Width = 800; Height = 600 }` |
| Content | background rectangle (`rgb 18,22,30`) + token circle (`rgb 220,80,60`, r=120 @ 400,300) + HUD panel rectangle (`rgba 255,255,255,48`) + bundled-font text `sizedText "HP 100 / SCORE 42" @24px` |
| Exercises | geometry, colour, **and** the bundled-font text path (`Fonts.fs`) — the three FR-002 content classes. |

A second pinned scene `secondaryScene ()` (`WAVE 7` @32px, distinct colours) exists **only** for the
concurrency test (T010), so interleaved renders that interfered would diverge from their own baseline.

## Pinned outputs (this environment)

- Bytes: **8491** · Dimensions: **800×600 RGBA** · `file`: `PNG image data, 800 x 600, 8-bit/color RGBA`.
- `sha256` (run 1 = run 2): `b24daddc2d8825a49fdfba5781cdb9e0748f2a33265fea8d06c54fb5bd608181`.

These tie SC-001 (non-blank, correct dims) and SC-002 (byte-identity) to a known input. The exact `sha256`
is environment/Skia-version sensitive; the *byte-identity across runs* invariant is what the tests assert.
