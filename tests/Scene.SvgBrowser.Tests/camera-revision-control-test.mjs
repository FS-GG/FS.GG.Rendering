import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { cameraRevisionControl } from "./camera-revision-control.mjs";

// Inert observation surface: tests the exact witness, not a substitute SVG renderer.
function fixture(fault) {
  let revision = 1;
  let stage = "before";
  let camera = { zoom: 2, panX: 20, panY: 10 };
  const root = {
    getAttribute: (name) => {
      assert.equal(name, "data-scene-revision");
      if (fault === "missing-marker") return null;
      if (fault === "stale-dom" && stage === "replace") return "1";
      return String(revision);
    },
  };
  const api = {
    state: () => ({ revision, camera }),
    observe: () => ({ revision: fault === "stale-observer" && stage === "replace" ? 1 : revision }),
    scenePoint: () => ({ x: 20, y: 20 }),
    hit: () => fault === "wrong-hit" ? "beta" : "alpha",
    zoomAt: (...args) => {
      assert.deepEqual(args, [60, 50, 4]);
      stage = "zoom";
      if (fault === "zoom-revision") revision++;
      if (fault !== "no-op-zoom") camera = { zoom: 4, panX: -20, panY: -30 };
      return { error: null };
    },
    panBy: (...args) => {
      assert.deepEqual(args, [10, -5]);
      stage = "pan";
      if (fault === "pan-revision") revision++;
      if (fault !== "no-op-pan") camera = { ...camera, panX: camera.panX + 10, panY: camera.panY - 5 };
      return { error: null };
    },
    replaceCurrent: () => {
      stage = "replace";
      if (fault !== "no-op-replacement") revision = 2;
      return { error: fault === "rejected-replacement" ? "other" : null };
    },
  };
  return {
    svgFoundation: api,
    document: { querySelector: (selector) => {
      assert.equal(selector, "[data-scene-root-id]");
      return fault === "replaced-root" && stage === "zoom" ? { ...root } : root;
    } },
  };
}

const receipt = cameraRevisionControl(fixture());
for (const key of ["beforeRevision", "zoomRevision", "panRevision"]) {
  assert.deepEqual(receipt[key], { state: 1, observed: 1, dom: "1" });
}
assert.deepEqual(receipt.replacementRevision, { state: 2, observed: 2, dom: "2" });
assert.equal(receipt.positiveControl, "actual-replace-scene-advanced-revision");
const negatives = [
  ["zoom-revision", /zoom manufactured a scene revision/],
  ["pan-revision", /pan manufactured a scene revision/],
  ["missing-marker", /correspondence failed \(before\)/],
  ["stale-dom", /correspondence failed \(replace\)/],
  ["stale-observer", /correspondence failed \(replace\)/],
  ["replaced-root", /correspondence failed \(zoom\)/],
  ["no-op-zoom", /did not change the camera/],
  ["no-op-pan", /did not change the camera/],
  ["no-op-replacement", /positive control failed/],
  ["rejected-replacement", /positive control failed/],
  ["wrong-hit", /anchor\/inverse contract failed/],
];
for (const [fault, expected] of negatives) {
  assert.throws(() => cameraRevisionControl(fixture(fault)), expected, fault);
}
// Guard the actual runner join: a separately passing helper is not browser evidence.
const runner = readFileSync(new URL("./browser-test.mjs", import.meta.url), "utf8");
assert.match(runner, /import \{ cameraRevisionControl \} from "\.\/camera-revision-control\.mjs";/);
assert.match(runner, /cameraRevisionEvidence = await page\.evaluate\(cameraRevisionControl\);/);
assert.match(runner, /cameraRevisionIsolation: cameraRevisionEvidence,/);
console.log(`camera revision witness controls: positive + ${negatives.length} refusals; browser execution unobserved`);
