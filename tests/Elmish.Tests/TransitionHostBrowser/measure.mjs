import { createHash } from "node:crypto";
import { createServer } from "node:http";
import { cpus, platform, release } from "node:os";
import { dirname, extname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { existsSync, mkdirSync, readFileSync, readdirSync, statSync, writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { chromium } from "playwright-core";
import { summarizeTrace } from "./trace-evidence.mjs";

const fixtureDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(fixtureDirectory, "../../..");
const distDirectory = resolve(fixtureDirectory, "dist");
const workloadPath = resolve(repositoryRoot, "work/1256-transition-aware-elmish-host-bridge/contracts/workspace-transition-workload.json");
const implementationPath = resolve(repositoryRoot, "src/Elmish/TransitionHost.fs");
const releaseMetadataPaths = [
  resolve(repositoryRoot, ".template.package/FS.GG.UI.Template.fsproj"),
  resolve(repositoryRoot, "src/Meta/FS.GG.UI.nuspec"),
  resolve(repositoryRoot, "template/base/Directory.Packages.props"),
];
const templateElmishDirectory = resolve(repositoryRoot, "template/fragments/elmish");
const defaultOutput = resolve(repositoryRoot, "readiness/1256-transition-aware-elmish-host-bridge/transition-host-production-performance.json");
const outputIndex = process.argv.indexOf("--out");
const outputPath = outputIndex >= 0 ? resolve(process.argv[outputIndex + 1]) : defaultOutput;
const traceDirectoryIndex = process.argv.indexOf("--trace-dir");
const traceDirectory = traceDirectoryIndex >= 0 ? resolve(process.argv[traceDirectoryIndex + 1]) : undefined;
const requiredWorkloadDigest = "a49fc53e890dc93961e68d821c6b680a7f10f1f8d1aadd2cc96787cdbdd73acb";
const actualGitHead = execFileSync("git", ["rev-parse", "HEAD"], { cwd: repositoryRoot, encoding: "utf8" }).trim();
const expectedGitHead = process.env.FS_GG_EXPECTED_GIT_HEAD?.trim();

if (expectedGitHead && !/^[0-9a-f]{40}$/.test(expectedGitHead)) {
  throw new Error(`FS_GG_EXPECTED_GIT_HEAD must be a full lowercase git SHA, got ${expectedGitHead}`);
}

if (expectedGitHead && actualGitHead !== expectedGitHead) {
  throw new Error(`Candidate checkout ${actualGitHead} does not equal required PR head ${expectedGitHead}`);
}

const sha256 = (value) => createHash("sha256").update(value).digest("hex");
const read = (path) => readFileSync(path);

function filesRecursively(directory) {
  return readdirSync(directory)
    .flatMap((name) => {
      const path = resolve(directory, name);
      return statSync(path).isDirectory() ? filesRecursively(path) : [path];
    })
    .sort();
}

function buildDigest() {
  const hash = createHash("sha256");
  for (const path of filesRecursively(distDirectory)) {
    hash.update(path.slice(distDirectory.length + 1));
    hash.update(read(path));
  }
  return hash.digest("hex");
}

function digestFiles(paths, base = repositoryRoot) {
  const hash = createHash("sha256");
  for (const path of paths.sort()) {
    hash.update(path.slice(base.length + 1));
    hash.update(read(path));
  }
  return hash.digest("hex");
}

function percentile(values, percentage) {
  if (!values.length) throw new Error("No renderer RunTask samples were recorded");
  const sorted = [...values].sort((left, right) => left - right);
  return sorted[Math.max(0, Math.ceil((percentage / 100) * sorted.length) - 1)];
}

function mime(path) {
  switch (extname(path)) {
    case ".html": return "text/html; charset=utf-8";
    case ".js": return "text/javascript; charset=utf-8";
    case ".css": return "text/css; charset=utf-8";
    default: return "application/octet-stream";
  }
}

if (!existsSync(resolve(distDirectory, "index.html"))) {
  throw new Error("Production fixture is not built; run npm run build first");
}

const workloadDigest = sha256(read(workloadPath));
if (workloadDigest !== requiredWorkloadDigest) {
  throw new Error(`Workload definition digest drifted: expected ${requiredWorkloadDigest}, observed ${workloadDigest}`);
}

const workload = JSON.parse(readFileSync(workloadPath, "utf8"));
if (
  workload.measurement.rendererTaskMaxMs !== 16
  || workload.measurement.p95Ms !== 16
  || workload.measurement.p99Ms !== 32
  || workload.measurement.droppedFrames !== 0
) {
  throw new Error("Filed performance thresholds were changed");
}

const server = createServer((request, response) => {
  if (request.url === "/favicon.ico") {
    response.writeHead(204).end();
    return;
  }
  const requestPath = request.url === "/" ? "index.html" : request.url.replace(/^\//, "");
  const path = resolve(distDirectory, requestPath);
  if (!path.startsWith(`${distDirectory}/`) || !existsSync(path) || statSync(path).isDirectory()) {
    response.writeHead(404).end();
    return;
  }
  response.writeHead(200, { "content-type": mime(path), "cache-control": "no-store" });
  response.end(read(path));
});

await new Promise((resolveListen) => server.listen(0, "127.0.0.1", resolveListen));
const address = server.address();
const executablePath = [
  process.env.PLAYWRIGHT_EXECUTABLE_PATH,
  "/usr/sbin/chromium",
  "/usr/bin/chromium",
  "/usr/bin/google-chrome",
  "/usr/bin/google-chrome-stable",
].find((candidate) => candidate && existsSync(candidate));
if (!executablePath) throw new Error("A production Chromium/Chrome executable is required");
const browserVersion = execFileSync(executablePath, ["--version"], { encoding: "utf8" }).trim();
const browser = await chromium.launch({ executablePath, headless: true });
const context = await browser.newContext({ viewport: { width: 1280, height: 900 }, deviceScaleFactor: 1 });
const page = await context.newPage();
const consoleErrors = [];
page.on("console", (message) => {
  if (message.type() === "error") consoleErrors.push(message.text());
});
page.on("pageerror", (error) => consoleErrors.push(error.stack || error.message));

const runResults = [];
const traceDigests = [];
const traceSummaries = [];
let rowContract;

if (traceDirectory) mkdirSync(traceDirectory, { recursive: true });

try {
  await page.goto(`http://127.0.0.1:${address.port}/`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.transitionHostReady === true);
  await page.waitForTimeout(100);

  const resetJourney = async (label) => {
    const reset = await page.evaluate(() => window.resetTransitionJourney());
    if (reset.target !== "Editor" || reset.ledgerEntries !== 0 || reset.pending) {
      throw new Error(`${label} did not reset to an independent mounted-Editor journey: ${JSON.stringify(reset)}`);
    }
  };

  for (let warmup = 0; warmup < workload.measurement.warmupRuns; warmup += 1) {
    await resetJourney(`Warmup ${warmup}`);
    const result = await page.evaluate((run) => window.runTransitionJourney(`warmup-${run}`), warmup);
    if (result.rows !== workload.maximumExpectedScale.workspaceRows || result.pending) {
      throw new Error(`Warmup ${warmup} did not converge at the declared scale`);
    }
  }

  const cdp = await context.newCDPSession(page);
  const captureTrace = async (startId, endId, journey) => {
    await cdp.send("Tracing.start", {
      categories: "devtools.timeline,blink.user_timing,v8,disabled-by-default-devtools.timeline",
      transferMode: "ReturnAsStream",
    });
    await cdp.send("Tracing.recordClockSyncMarker", { syncId: startId });
    const result = await journey();
    await cdp.send("Tracing.recordClockSyncMarker", { syncId: endId });

    const complete = new Promise((resolveComplete) => cdp.once("Tracing.tracingComplete", resolveComplete));
    await cdp.send("Tracing.end");
    const { stream } = await complete;
    const chunks = [];
    while (true) {
      const part = await cdp.send("IO.read", { handle: stream });
      chunks.push(part.data);
      if (part.eof) break;
    }
    await cdp.send("IO.close", { handle: stream });
    return { result, traceText: chunks.join("") };
  };

  // Chromium performs one-time compositor/tracing initialization during the first journey captured
  // by a fresh CDP session. Exercise that pipeline once before the declared twenty measured journeys;
  // this is an additional full-scale warmup and never replaces or removes a filed workload sample.
  await resetJourney("Tracing pipeline warmup");
  const traceWarmup = await captureTrace(
    "fsgg-transition-trace-warmup-start",
    "fsgg-transition-trace-warmup-end",
    () => page.evaluate(() => window.runTransitionJourney("trace-warmup")),
  );
  if (traceWarmup.result.rows !== workload.maximumExpectedScale.workspaceRows || traceWarmup.result.pending) {
    throw new Error(`Tracing pipeline warmup did not converge at the declared scale: ${JSON.stringify(traceWarmup.result)}`);
  }

  for (let run = 0; run < workload.measurement.measuredRuns; run += 1) {
    // Every filed sample is one independent journey from the mounted Editor state. Reset outside
    // tracing so a previous run's retained ledger/view cannot become measured work in the next run.
    await resetJourney(`Measured journey ${run}`);
    const startId = `fsgg-transition-start-${run}`;
    const endId = `fsgg-transition-end-${run}`;
    const capture = await captureTrace(
      startId,
      endId,
      () => page.evaluate((index) => window.runTransitionJourney(`measured-${index}`), run),
    );
    const { result, traceText } = capture;
    const trace = JSON.parse(traceText);
    const traceFile = `trace-${String(run).padStart(2, "0")}.json`;
    if (traceDirectory) writeFileSync(resolve(traceDirectory, traceFile), traceText);
    const traceSummary = summarizeTrace(trace, startId, endId);

    if (
      result.target !== "Simulate"
      || result.revision !== 2
      || result.rows !== workload.maximumExpectedScale.workspaceRows
      || result.resumePresentations !== 1
      || result.releaseEffects !== 1
      || result.suppressEffects !== workload.maximumExpectedScale.unsafeInputAttempts
      || result.stagedPendingChecks <= 0
      || !result.stagedPendingValid
      || result.pending
    ) {
      throw new Error(`Measured journey ${run} violated the production route contract: ${JSON.stringify(result)}`);
    }

    runResults.push(result);
    traceDigests.push(sha256(traceText));
    traceSummaries.push(traceSummary);
  }

  rowContract = await page.evaluate(() => window.inspectWorkspaceRows());
  if (
    rowContract.count !== workload.maximumExpectedScale.workspaceRows
    || !rowContract.semantics
    || !rowContract.visible
    || rowContract.columns !== 6
    || rowContract.distinctColumnPositions !== 6
    || rowContract.widthSpread > 1
  ) {
    throw new Error(`Rendered row semantics/geometry drifted: ${JSON.stringify(rowContract)}`);
  }
} finally {
  await browser.close();
  await new Promise((resolveClose) => server.close(resolveClose));
}

if (consoleErrors.length) throw new Error(`Browser console errors:\n${consoleErrors.join("\n")}`);

const taskMilliseconds = traceSummaries.flatMap((trace) => trace.taskMilliseconds);
const maximum = Number(Math.max(...taskMilliseconds).toFixed(3));
const p95 = Number(percentile(taskMilliseconds, 95).toFixed(3));
const p99 = Number(percentile(taskMilliseconds, 99).toFixed(3));
const droppedFrames = traceSummaries.reduce((total, trace) => total + trace.droppedFrames, 0);
const compositorSamples = traceSummaries.reduce((total, trace) => total + trace.compositorSamples, 0);
const everyTraceUsable = traceSummaries.every(
  (trace) => trace.frameSamples > 0 && trace.compositorSamples > 0 && trace.droppedFrames === 0,
);
const result = maximum <= 16 && p95 <= 16 && p99 <= 32 && droppedFrames === 0 && everyTraceUsable ? "pass" : "fail";
const implementationSha256 = sha256(read(implementationPath));
const capturedAtUtc = new Date().toISOString();
const currencyToken = `implementation-sha256:${implementationSha256}`;
const summary = {
  contractVersion: "performance-evidence-v1",
  claimedBudgetPassed: result === "pass",
  sampleSets: [{
    workloadId: workload.id,
    workloadDefinitionDigest: `sha256:${workloadDigest}`,
    workloadClass: "normal-play",
    targetFps: 60,
    maxP95Ms: 16,
    maxP99Ms: 32,
    maxCatchUpFrames: 0,
    measurementScope: "workspace-transition-production",
    requiredCapability: "production-fable-react-chromium",
    hostProfile: `${platform()}-${process.arch}-chromium-151`,
    packageVersions: ["FS.GG.UI.Elmish@source"],
    measurementMode: "live-compositor",
    capabilities: ["production-fable-react-chromium", "live-compositor"],
    warmupPolicy: "2 complete production journeys plus 1 full-scale tracing-pipeline journey before 20 measured traces",
    samplePolicy: "nearest-rank over every renderer RunTask across 20 independent traced journeys; hard maximum retained separately",
    capturedAtUtc,
    currencyToken,
    probeReadbackContaminated: false,
    durationSamplesMs: taskMilliseconds,
    catchUpFrames: traceSummaries.map((trace) => trace.droppedFrames),
  }],
  schema: "fsgg.elmish.transition-production-performance/v1",
  workId: "1256-transition-aware-elmish-host-bridge",
  result,
  route: workload.route,
  workload: {
    id: workload.id,
    sha256: workloadDigest,
    warmupRuns: workload.measurement.warmupRuns,
    tracingWarmupRuns: 1,
    measuredRuns: workload.measurement.measuredRuns,
    workspaceRows: workload.maximumExpectedScale.workspaceRows,
  },
  candidate: {
    gitHead: actualGitHead,
    gitTree: execFileSync("git", ["rev-parse", "HEAD^{tree}"], { cwd: repositoryRoot, encoding: "utf8" }).trim(),
    implementationSha256,
    releaseMetadataSha256: digestFiles(releaseMetadataPaths),
    templateElmishSha256: digestFiles(filesRecursively(templateElmishDirectory), templateElmishDirectory),
    productionBuildSha256: buildDigest(),
  },
  environment: {
    browser: browserVersion,
    browserExecutable: executablePath,
    node: process.version,
    os: `${platform()} ${release()}`,
    logicalCpuCount: cpus().length,
    liveCompositor: compositorSamples > 0,
    headless: true,
  },
  measurement: {
    source: workload.measurement.source,
    rendererTaskSamples: taskMilliseconds.length,
    rendererTaskMilliseconds: taskMilliseconds,
    rendererTaskMaxMs: maximum,
    p95Ms: p95,
    p99Ms: p99,
    droppedFrames,
    frameSamples: traceSummaries.reduce((total, trace) => total + trace.frameSamples, 0),
    compositorSamples,
    thresholds: {
      rendererTaskMaxMs: 16,
      p95Ms: 16,
      p99Ms: 32,
      droppedFrames: 0,
    },
  },
  traceRuns: traceDigests.map((digest, index) => ({
    run: index,
    rawTraceFile: `traces/trace-${String(index).padStart(2, "0")}.json`,
    rawTraceSha256: digest,
    rendererTaskSamples: traceSummaries[index].taskMilliseconds.length,
    rendererTaskMaxMs: Number(Math.max(...traceSummaries[index].taskMilliseconds).toFixed(3)),
    p95Ms: Number(percentile(traceSummaries[index].taskMilliseconds, 95).toFixed(3)),
    p99Ms: Number(percentile(traceSummaries[index].taskMilliseconds, 99).toFixed(3)),
    frameSamples: traceSummaries[index].frameSamples,
    droppedFrames: traceSummaries[index].droppedFrames,
    compositorSamples: traceSummaries[index].compositorSamples,
  })),
  integrity: {
    algorithm: "sha256",
    rawTraceSetSha256: sha256(traceDigests.join("\n")),
    rawTraceCount: traceDigests.length,
  },
  acceptance: {
    targets: [...new Set(runResults.map((run) => run.target))],
    revisions: [...new Set(runResults.map((run) => run.revision))],
    controlledValuesPreserved: runResults.every((run) => run.controlled.startsWith("mission-measured-")),
    exactlyOneResumePresentation: runResults.every((run) => run.resumePresentations === 1),
    pointerCaptureReleased: runResults.every((run) => run.releaseEffects === 1),
    unsafeInputsSuppressed: runResults.every((run) => run.suppressEffects === 4),
    deterministicLedgerObserved: runResults.every((run) => run.ledgerEntries > 0),
    independentLedgerBounded: runResults.every((run) => run.ledgerEntries === runResults[0].ledgerEntries),
    stagedRowsRemainPending: runResults.every((run) => run.stagedPendingChecks > 0 && run.stagedPendingValid),
    semanticRows: rowContract,
  },
};

writeFileSync(outputPath, `${JSON.stringify(summary, null, 2)}\n`);
console.log(JSON.stringify({
  result,
  candidateGitHead: summary.candidate.gitHead,
  maximum,
  p95,
  p99,
  droppedFrames,
  frameSamples: summary.measurement.frameSamples,
  samples: taskMilliseconds.length,
  output: outputPath,
}));
if (result !== "pass") process.exitCode = 1;
