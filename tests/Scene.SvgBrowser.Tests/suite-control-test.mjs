import assert from "node:assert/strict";
import { createHash, randomBytes } from "node:crypto";
import { portableSha256 } from "./fixture-hash.js";
import { validateRebuild, validateLifecycle, validateExcessive, summarize } from "./suite-contract.js";

for (const length of [0, 1, 55, 56, 63, 64, 65, 1024, 100000]) {
  const bytes = randomBytes(length);
  assert.equal(portableSha256(bytes), `sha256:${createHash("sha256").update(bytes).digest("hex")}`);
}
validateRebuild({ rebuiltObjects: 0, rebuiltDefinitions: 0 });
assert.throws(() => validateRebuild({ rebuiltObjects: 1 }), /unnecessary-rebuild gate/);
validateLifecycle({ roots: 0, active: 0, frames: 0, listeners: 0 });
assert.throws(() => validateLifecycle({ listeners: 1 }), /listener\/resource-leak gate/);
validateExcessive({ rootRetained: true, error: "node-limit" });
assert.throws(() => validateExcessive({ rootRetained: false, error: null }), /excessive-document gate/);
assert.equal(summarize(Array.from({ length: 30 }, (_, i) => i)).p99, null);
console.log("page control predicates and portable artifact hashes: passed");
