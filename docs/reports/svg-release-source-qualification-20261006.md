# SVG 0.32.1 release-source qualification

The Stage A/B release-route candidate at
`b4326e3a6dca9c372beb4d9f7a6be807538a4212` passed a bounded local F# qualification
on 2026-10-06. This qualifies the changed controls and wrapper refusal paths.
Required hosted source checks, the 19-package candidate, installed Templates
qualification and publication remain pending.

The actual five-command window completed in 10.469 seconds. It first proved that
F# assertions were active with an expected assertion termination, ran the NuGet
scope/provenance and package-census controls, then compiled both real wrappers
with no arguments and observed their pre-transport refusals. Ordered exit codes
were `[-6, 0, 0, 3, 3]`; all expected output, capture hashes and clean process
retirement joined. The hosted NuGet control invocation also explicitly defines
`DEBUG`, so an assertions-disabled invocation cannot stand in for those controls.

The selected SDK was 10.0.401 and runtime 10.0.12. The supervisor verified the
retained FSI/runtime/reference closure before and after the graph, set and read
back core limits `[0,0]`, and disabled .NET mini-dumps. One CPU, a 2 GiB sampled
owned-memory bound, 32 owned processes and 10 aggregate CLR processes applied
within one 180-second clock, with 150 seconds for work. Observed peaks were
292,802,560 bytes, two owned processes and one CLR process. Private scratch peaked
at 38,544 bytes. Resource failure was false and custody was clean. These are
sampled observations, not an instantaneous memory-sandbox guarantee.

The retained terminal receipt SHA-256 is
`92621bb834ae5855608b761f3e00c1cf13318a7ef0fdc90d090675e54135134c`.
The private wrapper source seal is
`cfa35598ba572224e767ea42f9cbd014b3537623d8fa3251eb6396bd88e1c5d7`.
Raw captures and tool/source joins remain in the programme's private custody.
The preceding window is retained as failed: its private sentinel produced F#
FS0030 before executing an assertion. A unit-constrained fallback statement fixed
that fixture; the expected assertion exit and strict acceptance rules were not
relaxed. The failed window contributes no qualification credit.

Earlier source checks passed: 24 release-source guard controls, 16 preflight
controls, seven historical mirror controls and 34 Bash syntax checks. They remain
separate from this actual F# result and from hosted qualification. The release
change preserves the historical 0.32.0 publisher's bound inputs; its control
invocation now enables assertions. The 0.32.1 plan still refuses publication even
if its readiness field is changed.

After protected Stage A/B source delivery, Stage C uses the existing source-only
workflow to produce and retain the genuine 19-package 0.32.1 archive against the
0.32.0 API baseline. Its observed producer/run/archive/custody identities must then
bind the existing Templates receiver route. That route currently selects the
historical producer, so its source pins and the exact Rendering caller require a
reviewed successor before genuine installed qualification. Neither the local F#
result nor historical receiver evidence supplies those identities.

Stage D follows candidate and receiver acceptance: bind the new immutable attempt,
qualify source/executor separation and original-byte recovery, and obtain fresh
both-feed/tag observations and effective publisher authority. No package, tag,
feed mutation, network-cut operation, browser measurement or GPU qualification
occurred in this local window. SVG milestones .3/.4, consumer adoption and the
late host GPU batch remain open.
