import assert from "node:assert/strict";
import { summarizeTrace } from "./trace-evidence.mjs";

const startId = "start";
const endId = "end";
const base = [
  { name: "clock_sync", ts: 100, args: { sync_id: startId } },
  { name: "thread_name", tid: 7, args: { name: "CrRendererMain" } },
  { name: "RunTask", tid: 7, ts: 120, dur: 4000 },
  { name: "DrawFrame", tid: 9, ts: 140 },
  { name: "clock_sync", ts: 200, args: { sync_id: endId } },
];

for (const frame of [
  { name: "AnimationFrame", ph: "b", ts: 130, args: {} },
  { name: "AnimationFrame", ph: "b", ts: 130, args: { animation_frame_timing_info: { duration_ms: "corrupt" } } },
]) {
  assert.throws(
    () => summarizeTrace({ traceEvents: [...base, frame] }, startId, endId),
    /zero usable AnimationFrame duration samples/,
  );
}

assert.throws(
  () => summarizeTrace({
    traceEvents: [
      ...base.filter((event) => event.name !== "DrawFrame"),
      { name: "AnimationFrame", ph: "b", ts: 130, args: { animation_frame_timing_info: { duration_ms: 4 } } },
    ],
  }, startId, endId),
  /zero compositor\/presentation samples/,
);

const dropped = summarizeTrace({
  traceEvents: [
    ...base,
    { name: "AnimationFrame", ph: "b", ts: 130, args: { animation_frame_timing_info: { duration_ms: 26 } } },
  ],
}, startId, endId);
assert.equal(dropped.frameSamples, 1);
assert.equal(dropped.droppedFrames, 1);

console.log("frame-evidence inversion passed: missing/corrupt frames and zero compositor fail closed; >25ms is dropped");
