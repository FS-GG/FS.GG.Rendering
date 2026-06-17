generated-consumer-validation

consumer-project: generated-package-consumer
restore-log: specs/035-api-discovery-names/readiness/package/consumer/restore.log
build-log: specs/035-api-discovery-names/readiness/package/consumer/build.log
local-package-feed: ~/.local/share/nuget-local
package-version: 0.1.9-preview.1
package-references: FS.GG.UI.Scene, FS.GG.UI.Controls, FS.GG.UI.SkiaViewer
project-references: none
copied-src-files: none
repository-source-inspection: false
assembly-reflection-authoring: false
result: pass

failure-class: restore
next-action: inspect restore-log and local-package-feed package availability.

failure-class: project-reference
next-action: remove project references and consume only package references.

failure-class: copied-src
next-action: remove copied source files from the generated consumer.

failure-class: reflection-authoring
next-action: replace reflection-authored samples with curated package documentation.

failure-class: compile
next-action: inspect build-log and update the generated package consumer code.
