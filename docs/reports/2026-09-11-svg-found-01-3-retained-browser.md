# SVG-FOUND-01.3 retained browser adapter evidence

Date: 2026-09-11

Status: implemented and locally verified; package publication and template adoption remain disabled

## Delivered contract

`FS.GG.UI.Scene.SvgBrowser` is a separate packable adapter over
`FS.GG.UI.Scene`. Its package carries the exact curated Fable project and two
source files under `fable/`; the base Scene package remains free of browser
dependencies. The adapter validates the portable retained-scene contract before
mounting and routes selection, focus, pointer capture, camera changes, and scene
replacement through `SvgRetained.update`.

The host retains one SVG root and the element for every unaffected layer across
an accepted revision. It rejects stale replacement through the Scene reducer,
provides camera-aware inverse picking, maps both pointer and keyboard activation
to the same selection message, and projects root/object roles, labels, focus,
and selection state into the DOM. Disposal removes the six listeners owned by
the host and removes its root. The adapter schedules no animation-frame work.

## Isolated browser fixture

`tests/Scene.SvgBrowser.Tests/run.sh` packs Scene and the adapter to a temporary
feed, copies the fixture outside the checkout, restores only package references,
compiles with Fable 5.17.0, builds a production Vite bundle, and runs the result
in Playwright Chromium. It rejects sibling project/source links, native browser
dependencies, accidental Skia/viewer/control dependencies, an uncurated Fable
payload, and any browser console error.

The 2026-09-11 observation in
`readiness/svg-foundation/svg-browser-observations.json` passed all focused
behavior checks:

- Pointer and keyboard selected the same object through an accessible DOM route.
- Anchored zoom and pan preserved inverse picking under representative camera transforms.
- Revision 2 retained the SVG root plus both layer elements and preserved selection.
- A revision 1 replacement returned `non-increasing-revision` and did not replace revision 2 DOM.
- Twelve mount/dispose cycles returned the owned-listener balance to zero each time; the mounted host owned six listeners and scheduled zero frames.

The measured fixture scene had two layers, three objects, and ten SVG nodes. On
headless Chromium 151.0.7922.34 at 800×600 and device scale 1 on Linux
7.2.2-arch1-1/Node v26.8.2, 20 samples observed a 0.2 ms median mount (0.4 ms
maximum) and 13.8 ms median input-to-next-animation-frame time (14.4 ms
maximum). The production output was two files, 79,899 raw bytes and 21,483
gzip bytes. These are early observations for this small fixture, not thresholds
or complete M9 qualification. The run did not capture compositor presentation,
heap/detached-node data, Firefox/WebKit, touch/mobile GPU behavior, or live
assistive-technology announcements.

## Provenance

The runtime adapter adds `Fable.Browser.Dom` 2.20.0. The inspected NuGet archive
has SHA-256
`979c5bfeb339a9e49cc8bbdc60b8ba5702d91280cb532b24aaf03b016ad03248`,
identifies Fable contributors, points to repository commit
`3cfb6a7f559d7f4251a693313eb3f694f4fda1d5`, and carries the MIT license with
copyright © 2019 Fable. Playwright Core 1.62.1 (Apache-2.0) and Vite 8.1.5 (MIT)
are fixture-only npm dependencies. No S.I.R. code or assets were copied for this
milestone; the owner's authorization recorded in the foundation audit therefore
does not replace the separate third-party review above.

## Reproduction

```sh
dotnet restore FS.GG.Rendering.slnx --force-evaluate
dotnet build FS.GG.Rendering.slnx --no-restore
bash tests/Scene.SvgBrowser.Tests/run.sh
```

The repository also has an advisory, path-filtered `svg-browser` workflow that
installs Chromium, reruns the isolated fixture, and retains the JSON observation
artifact. No package was published and no browser adapter became a template or
product default in this milestone.
