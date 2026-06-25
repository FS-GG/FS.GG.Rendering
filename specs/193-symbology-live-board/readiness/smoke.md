# Early evidence smoke run (T008) — Symbology Live Board (M6)

**Purpose** (plan.md:22–28): before building any presentation/test layer, prove the board really renders a
non-empty scene and reproduces *in this checkout* — deterministic unit tests can pass while the real run
path is broken, so this real run is the confirmation, not the plan's narrative.

**Checkout**: branch `193-symbology-live-board`, 2026-06-25. Build clean (0 warnings, 0 errors).

## Same-seed reproducibility (two real CLI runs)

```
$ dotnet run --project samples/SymbologyBoard -- evidence      # run 1 (default path)
symbology-board: seeded fingerprint = sha256:4786621d525ea94ae2a78df95893ff175c0abd6053b0fb05f3f0cd2004c96a95
symbology-board: reproducible (two runs byte-identical).
exit=0

$ dotnet run --project samples/SymbologyBoard -- evidence      # run 2 (explicit)
symbology-board: seeded fingerprint = sha256:4786621d525ea94ae2a78df95893ff175c0abd6053b0fb05f3f0cd2004c96a95
symbology-board: reproducible (two runs byte-identical).
exit=0
```

Both invocations print the **same** fingerprint `sha256:4786…6a95` → the board renders a non-empty,
reproducible scene from seed 1 + the 120-`Tick` script.

## Divergent seed (Board.evidence driven against the built assemblies)

```
seed=1 -> sha256:4786621d525ea94ae2a78df95893ff175c0abd6053b0fb05f3f0cd2004c96a95
seed=2 -> sha256:3b434d2ed96b4fa6faabaee58f348146cd2c9d8ad09e8d08e9f4a4b3271b1be4
divergent (s1 <> s2) = true
seed1 stable = true
```

The seed materially drives the board (different seed ⇒ different fingerprint), and seed 1 is stable across
calls. (`dotnet fsi` over `FS.GG.UI.Scene`/`Symbology`/`Canvas` + `SymbologyBoard.dll` — the pure scene
codec path, no native SkiaSharp; consistent with the FSI-can't-load-native-Skia gotcha, which is about the
render/readback path, not the canonical-bytes path.)

## Conclusion

The board renders non-empty and reproduces in this checkout; the seed drives divergence. Presentation (US1)
and test (US2) layers may proceed on observed evidence, not on the plan's hypotheses.
