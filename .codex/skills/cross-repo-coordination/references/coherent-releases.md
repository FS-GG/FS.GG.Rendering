# Coherent releases

Audit every affected producer and transitive consumer. Choose coherent versions, build and pack from
merged commits, publish in dependency order, and verify byte-identical artifacts on every required feed.
Then update registry leads, generated projections/manifests/locks, and downstream pins.

Public download/install verification is part of publication. A successful upload or green source build
alone is insufficient. Keep the coordinating claim until registry and required consumer obligations are
confirmed.
