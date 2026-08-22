function marker(events, id) {
  return events.find((event) => event.args?.sync_id === id && Number(event.ts) > 0);
}

export function summarizeTrace(trace, startId, endId) {
  const events = Array.isArray(trace.traceEvents) ? trace.traceEvents : [];
  const start = marker(events, startId);
  const end = marker(events, endId);
  if (!start || !end || Number(end.ts) <= Number(start.ts)) {
    throw new Error(`Trace window ${startId}..${endId} is missing or invalid`);
  }

  const threads = new Map(
    events
      .filter((event) => event.name === "thread_name")
      .map((event) => [event.tid, event.args?.name || ""]),
  );
  const inside = events.filter((event) => Number(event.ts) >= Number(start.ts) && Number(event.ts) <= Number(end.ts));
  const rendererThreadIds = new Set(
    [...threads]
      .filter(([, name]) => /CrRendererMain|RendererMain/i.test(name))
      .map(([threadId]) => threadId),
  );
  const taskEvents = inside.filter(
    (event) => event.name === "RunTask" && Number(event.dur) >= 0
      && (rendererThreadIds.size === 0 || rendererThreadIds.has(event.tid)),
  );
  const taskMilliseconds = taskEvents.map((event) => Number((Number(event.dur) / 1000).toFixed(3)));
  const frames = inside.filter(
    (event) => event.name === "AnimationFrame" && event.ph === "b" && Number(event.ts) > 0,
  );
  const frameDurations = frames
    .map((event) => Number(event.args?.animation_frame_timing_info?.duration_ms))
    .filter(Number.isFinite)
    .map((duration) => Number(duration.toFixed(3)));

  if (frameDurations.length === 0) {
    throw new Error(`Trace window ${startId}..${endId} has zero usable AnimationFrame duration samples`);
  }

  const compositorSamples = inside.filter(
    (event) => event.name === "DrawFrame" || event.name === "CompositeLayers" || event.name === "AnimationFrame::Presentation",
  ).length;

  if (compositorSamples === 0) {
    throw new Error(`Trace window ${startId}..${endId} has zero compositor/presentation samples`);
  }

  return {
    taskMilliseconds,
    rendererThreadNames: [...rendererThreadIds].map((threadId) => threads.get(threadId)),
    frameSamples: frameDurations.length,
    frameDurations,
    droppedFrames: frameDurations.filter((duration) => duration > 25).length,
    compositorSamples,
  };
}
