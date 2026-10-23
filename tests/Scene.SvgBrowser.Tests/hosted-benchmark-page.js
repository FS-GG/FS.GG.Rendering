// Hosted public-fixture software benchmark: deterministic unit states and the in-page record runner.
// The driver injects these exact bytes into the packed fixture page; Node controls import them.
// Only the synchronous compiled document replacement is timed. Oracles run outside timed spans.

export const benchmarkMotions = ["static", "camera-only", "sparse-10", "sparse-50", "churn-10"];
const strides = { "sparse-10": 10, "sparse-50": 2, "churn-10": 10 };
const offsetsX = [0, 3, 0, -3], offsetsY = [0, 1, 0, -1], cameraX = [0, 4, 0, -4], cameraY = [0, 2, 0, -2];

// Number of ticks in 1..tick whose residue selects this unit (stride s, residue i mod s).
export function selectedTicks(index, tick, stride) {
  const first = index % stride === 0 ? stride : index % stride;
  return tick >= first ? Math.floor((tick - first) / stride) + 1 : 0;
}

export function benchmarkState(spec, tick) {
  const { count, motion } = spec;
  if (![100, 250, 500].includes(count)) throw new Error("hosted benchmark count");
  if (!benchmarkMotions.includes(motion)) throw new Error("hosted benchmark motion");
  if (!Number.isInteger(tick) || tick < 0) throw new Error("hosted benchmark tick");
  const stride = strides[motion];
  const units = Array.from({ length: count }, (_, index) => {
    const moves = motion === "sparse-10" || motion === "sparse-50" ? selectedTicks(index, tick, stride) : 0;
    const generation = motion === "churn-10" ? selectedTicks(index, tick, stride) : 0;
    return {
      id: `unit-${index}-g${generation}`, index,
      x: 16 + (index % 20) * 20 + offsetsX[moves % 4], y: 16 + Math.floor(index / 20) * 20 + offsetsY[moves % 4],
      faction: index % 2, health: 1 + (index * 7) % 10, selected: index % 10 === 3,
    };
  });
  const phase = motion === "camera-only" ? tick % 4 : 0;
  return { cameraX: cameraX[phase], cameraY: cameraY[phase], units };
}

// Self-contained record runner; `w` is the packed fixture window exposing svgFoundation.
export async function runBenchmarkRecord(w, spec) {
  const f = w.svgFoundation, doc = w.document;
  const require = (condition, detail) => { if (!condition) throw new Error(`hosted benchmark ${spec.representation}/${spec.count}/${spec.motion}: ${detail}`); };
  require(["shared-symbol", "expanded-geometry"].includes(spec.representation), "representation");
  require(Number.isInteger(spec.warmupTicks) && Number.isInteger(spec.measuredTicks) && spec.measuredTicks > 0, "tick bounds");
  const rootOf = () => doc.querySelector("#document-fixture svg");
  const nextFrame = () => new Promise((done) => w.requestAnimationFrame(done));
  const canonical = (node) => [node.namespaceURI, node.localName, [...node.attributes].map((a) => [a.name, a.value]).sort((a, b) => a[0].localeCompare(b[0])), node.children.length ? null : node.textContent, [...node.children].map(canonical)];
  const arrays = (state) => [state.cameraX, state.cameraY, state.units.map((u) => u.id), state.units.map((u) => u.x), state.units.map((u) => u.y), state.units.map((u) => u.faction), state.units.map((u) => u.health), state.units.map((u) => u.selected)];
  const apply = (args) => f.replaceHostedBenchmarkDocument(spec.representation, ...args);
  let previous = new Map(), mountedRoot;
  const oracle = (state) => {
    const root = rootOf();
    require(root === mountedRoot, "root rebuilt");
    require(root.getAttribute("viewBox") === "0 0 410 510", "viewBox");
    const camera = root.querySelector("[data-fsgg-element-id='bench-camera']");
    require(camera?.getAttribute("transform") === `matrix(1 0 0 1 ${state.cameraX} ${state.cameraY})`, "camera transform");
    const units = [...camera.children].filter((n) => n.hasAttribute("data-fsgg-element-id"));
    require(JSON.stringify(units.map((n) => n.getAttribute("data-fsgg-element-id"))) === JSON.stringify(state.units.map((u) => u.id)), "unit population/order");
    const current = new Map(units.map((n) => [n.getAttribute("data-fsgg-element-id"), n]));
    for (const [id, node] of previous) {
      if (current.has(id)) require(current.get(id) === node, "surviving unit rebuilt");
      else require(!node.isConnected, "removed unit retained");
    }
    // Producer-namespaced symbol ids resolve through their authored geometry identity.
    const symbols = new Map([...root.querySelectorAll("defs > symbol")].map((n) => [`#${n.id}`, n.firstElementChild?.getAttribute("data-fsgg-element-id")]));
    state.units.forEach((u) => {
      const node = current.get(u.id), part = (name) => node.querySelector(`[data-fsgg-element-id='${u.id}/${name}']`);
      require(node.getAttribute("transform") === `matrix(1 0 0 1 ${u.x} ${u.y})`, "unit transform");
      const glyph = part("glyph");
      if (spec.representation === "shared-symbol") require(symbols.get(glyph?.querySelector("use")?.getAttribute("href")) === `bench-faction-${u.faction}-geometry`, "faction symbol reference");
      else require(glyph?.querySelector("circle")?.getAttribute("fill") === (u.faction ? "rgb(220 80 60)" : "rgb(70 130 220)"), "faction colour");
      const bars = part("health")?.querySelectorAll("rect");
      require(bars?.length === 2 && Number(bars[1].getAttribute("width")) === u.health, "health bar");
      require((part("outline")?.querySelectorAll("rect").length ?? 0) === (u.selected ? 4 : 0), "selection outline");
    });
    previous = current;
    return root.querySelectorAll("*").length + 1;
  };
  const liveMatchesExport = () => {
    const parsed = new w.DOMParser().parseFromString(f.documentExport(), "image/svg+xml");
    require(!parsed.querySelector("parsererror") && JSON.stringify(canonical(rootOf())) === JSON.stringify(canonical(parsed.documentElement)), "live/export structure differs");
  };
  const sustained = Number.isFinite(spec.sustainedMilliseconds);
  f.documentMount();
  try {
    require(apply(arrays(benchmarkState(spec, 0))) === null, "initial state refused");
    mountedRoot = rootOf();
    const initialNodes = oracle(benchmarkState(spec, 0));
    await nextFrame();
    const updateMs = [], frameMs = [];
    let tick = 0, oracleTicks = 0;
    const started = w.performance.now();
    while (sustained ? w.performance.now() - started < spec.sustainedMilliseconds : tick < spec.warmupTicks + spec.measuredTicks) {
      tick++;
      require(doc.visibilityState === "visible", "page backgrounded");
      const state = benchmarkState(spec, tick), args = arrays(state);
      const t0 = w.performance.now();
      const error = apply(args);
      const t1 = w.performance.now();
      require(error === null, `tick ${tick} refused: ${error}`);
      await nextFrame();
      const t2 = w.performance.now();
      if (sustained || tick > spec.warmupTicks) { updateMs.push(t1 - t0); frameMs.push(t2 - t1); }
      require(rootOf() === mountedRoot, "root rebuilt");
      if (tick === spec.warmupTicks || tick % 25 === 0) { oracle(state); oracleTicks++; }
    }
    const finalNodes = oracle(benchmarkState(spec, tick)); oracleTicks++;
    liveMatchesExport();
    if (!sustained) require(updateMs.length === spec.measuredTicks, "measured tick coverage");
    require(finalNodes === initialNodes, "live node count changed");
    return { ...spec, ticks: tick, warmupTicks: sustained ? 0 : spec.warmupTicks, updateMs, frameMs, nodes: { initial: initialNodes, final: finalNodes }, oracleTicks, scope: "synchronous compiled document replacement; oracle, export and frame wait outside the timed span" };
  } finally {
    f.documentDispose();
    require(!rootOf(), "disposed root retained");
  }
}

// Smallest positive step of this page's high-resolution clock.
export function clockResolution(w) {
  let smallest = Infinity;
  for (let i = 0; i < 2000; i++) {
    const a = w.performance.now(); let b = w.performance.now();
    while (b === a) b = w.performance.now();
    smallest = Math.min(smallest, b - a);
  }
  return smallest;
}

if (typeof window !== "undefined") window.fsggHostedBenchmark = { benchmarkState, runBenchmarkRecord, clockResolution };
