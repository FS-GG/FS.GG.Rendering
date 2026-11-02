# Routine closure after original tag settlement

Rendering adopted all five canonical routine files with `scripts/routine-closure.py apply`
from FS-GG/.github `79f9fe82832c0b3aee1beac3232b84a9f75e1ee8`.
The matching `check` reports present 5/5, equal 5/5 and zero stale pins.

The [release-owner ruling](https://github.com/FS-GG/FS.GG.Rendering/issues/1474#issuecomment-6588135866)
accepts original settlement [run40997578723, attempt1](https://github.com/FS-GG/FS.GG.Rendering/actions/runs/40997578723)
at `f8f23a7a3677da2fb3d2090119443059e160a68c`.
Artifact12951464438 has ZIP SHA256
`b700ad2141c1d8a2c8d1ed0d9b9459b1ac90d0dd60332b28a633457b9cd45c23`;
its settlement.json has SHA256
`f495d633c0e79341856d76a3dd502917ba7623c7ee8de3313b324da9a8f897bd`.
The receipt binds `rendering-0324-original40737624380-a1` to producer
`4ede04be66cedffc8551cd14045bab714aa358a7`, with Verified convergence,
passed result, restored cleanup and no pending subordinate refs. The original
transport outcome remains Unknown. No tag or publication replay follows from adoption.

Both closed subjects now have accepted settlement runs. The settlement workflow job
is dormant before lock acquisition or credentials, and every CLI entry refuses both
subjects before reads or effects. Historical mechanism fixtures explicitly restore
the pre-settlement table only within fake-provider tests; current-state refusal tests
use the unmodified source table.

The producer workflow censuses and original intent files remain immutable. Only the
current executor census is refreshed. Two exact per-file waivers classify the old
routine workflow hash in the preserved producer maps as historical evidence. Other
old hashes and other paths still fail the closure checker.

Source and local controls do not establish the new base-loaded workflow in operation.
Exact-head required CI, merge readback and a live fast-forward routine canary remain
under Root integration. No release, tag, publication or runtime acceptance is implied.
