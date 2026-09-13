import { createServer } from "node:http";
import { dirname, extname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { existsSync, readFileSync, statSync, writeFileSync } from "node:fs";
import { chromium, firefox, webkit } from "playwright-core";

const fixture = dirname(fileURLToPath(import.meta.url));
const dist = resolve(fixture, "dist");
const browserIndex = process.argv.indexOf("--browser");
const family = browserIndex < 0 ? "chromium" : process.argv[browserIndex + 1];
const outputIndex = process.argv.indexOf("--out");
const output = outputIndex < 0 ? null : resolve(process.argv[outputIndex + 1]);
const browserType = { chromium, firefox, webkit }[family];
if (!browserType) throw new Error(`unsupported browser family: ${family}`);

function mime(path) {
  if (extname(path) === ".html") return "text/html; charset=utf-8";
  if (extname(path) === ".js") return "text/javascript; charset=utf-8";
  return "application/octet-stream";
}
const server = createServer((request, response) => {
  const target = request.url === "/" ? resolve(dist, "persistence.html") : resolve(dist, request.url.replace(/^\//, ""));
  if (!target.startsWith(`${dist}/`) || !existsSync(target) || statSync(target).isDirectory()) {
    response.writeHead(404).end(); return;
  }
  response.writeHead(200, { "content-type": mime(target), "cache-control": "no-store" });
  response.end(readFileSync(target));
});
await new Promise((done) => server.listen(0, "127.0.0.1", done));
const browser = await browserType.launch({ headless: true });
const page = await browser.newPage();
const consoleErrors = [];
page.on("console", (message) => { if (message.type() === "error") consoleErrors.push(message.text()); });
page.on("pageerror", (error) => consoleErrors.push(error.stack || error.message));

const db = `svg-present-persistence-${family}-${Date.now()}`;
const deleteDatabase = (name) => page.evaluate((value) => new Promise((resolveDelete, reject) => {
  const request = indexedDB.deleteDatabase(value);
  request.onsuccess = () => resolveDelete();
  request.onerror = () => reject(request.error);
  request.onblocked = () => reject(new Error("delete blocked"));
}), name);
const observe = () => page.evaluate(() => window.svgPersistence.observe());
const waitFor = async (predicate, description, argument = undefined) => {
  await page.waitForFunction(predicate, argument, { timeout: 5000 }).catch(async (error) => {
    throw new Error(`${description}: ${JSON.stringify(await observe())}: ${consoleErrors.join("\n") || error.message}`);
  });
  return observe();
};
const eventCount = async () => (await observe()).events.length;
const waitForNext = async (before, kind) => waitFor(
  ({ before, kind }) => window.svgPersistence.observe().events.slice(before).some((event) => event.kind === kind),
  `missing ${kind} event`,
  { before, kind });
const load = async (familyId, slot) => {
  const before = await eventCount();
  await page.evaluate(({ familyId, slot }) => window.svgPersistence.load(familyId, slot), { familyId, slot });
  const state = await waitForNext(before, "loaded");
  return state.events.slice(before).find((event) => event.kind === "loaded");
};
const persist = async (familyId, slot, schema, hash, payload, generation, operation) => {
  const before = await eventCount();
  await page.evaluate((args) => window.svgPersistence.persist(...args), [familyId, slot, schema, hash, payload, generation, operation]);
  return waitFor(
    ({ before, operation }) => window.svgPersistence.observe().events.slice(before).some((event) => event.operation === operation),
    `operation ${operation} did not settle`,
    { before, operation });
};
const importArchive = async (archive, generation, operation) => {
  const before = await eventCount();
  await page.evaluate((args) => window.svgPersistence.importArchive(...args), [archive, generation, operation]);
  return waitFor(
    ({ before, operation }) => window.svgPersistence.observe().events.slice(before).some((event) => event.operation === operation),
    `import ${operation} did not settle`,
    { before, operation });
};
const exportArchive = async () => {
  const before = await eventCount();
  await page.evaluate(() => window.svgPersistence.exportArchive());
  return (await waitForNext(before, "exported")).archive;
};

let evidence;
try {
  await page.goto(`http://127.0.0.1:${server.address().port}/`, { waitUntil: "networkidle" });
  await page.waitForFunction(() => window.svgPersistence !== undefined);
  await deleteDatabase(db);
  await page.evaluate((name) => window.svgPersistence.mount(name, 10000), db);
  await waitFor(() => window.svgPersistence.observe().ready, "database did not become ready");

  const families = ["project", "assets", "save", "workspace"];
  for (let index = 0; index < families.length; index += 1) {
    const result = await persist(families[index], "primary", 1, `payload-${index}`, `first-${index}`, 1, index + 1);
    if (!result.events.some((event) => event.kind === "persisted" && event.operation === index + 1)) throw new Error(`first save failed for ${families[index]}`);
  }
  await persist("save", "primary", 2, "save-update", "updated", 1, 5);
  if ((await load("save", "primary")).value?.payload !== "updated") throw new Error("update was not readable");

  await page.evaluate(() => window.svgPersistence.dispose());
  await page.evaluate((name) => window.svgPersistence.mount(name, 10000), db);
  await waitFor(() => window.svgPersistence.observe().ready, "reload database did not become ready");
  const reloaded = await load("save", "primary");
  if (reloaded.value?.schema !== 2 || reloaded.value?.payload !== "updated") throw new Error(`reload lost update: ${JSON.stringify(reloaded)}`);

  await persist("save", "primary", 99, "newer", "preserved-newer", 2, 6);
  const stale = await persist("save", "primary", 1, "stale", "must-not-write", 1, 7);
  if (!stale.events.some((event) => event.operation === 7 && event.failure === "stale")) throw new Error("stale generation was accepted");
  const quota = await persist("save", "primary", 1, "large", "x".repeat(10001), 2, 8);
  if (!quota.events.some((event) => event.operation === 8 && event.failure === "quota")) throw new Error("quota refusal was not explicit");
  if ((await load("save", "primary")).value?.payload !== "preserved-newer") throw new Error("negative persistence case changed committed data");

  const validArchive = await exportArchive();
  const parsed = JSON.parse(validArchive);
  if (parsed.schema !== "fsgg.browser-archive/v1" || parsed.members.length !== 4 || parsed.declaredMembers.length !== 4) throw new Error("export manifest incomplete");
  if ([...parsed.members].map((member) => member.path).join() !== [...parsed.members].map((member) => member.path).sort().join()) throw new Error("export ordering is unstable");

  const invalids = [];
  const traversal = structuredClone(parsed);
  traversal.members[0].path = "../escape.json";
  traversal.declaredMembers[0] = "../escape.json";
  invalids.push(JSON.stringify(traversal));
  const missing = structuredClone(parsed);
  missing.members.pop();
  invalids.push(JSON.stringify(missing));
  const corrupt = structuredClone(parsed);
  corrupt.members[0].hash = "0".repeat(64);
  invalids.push(JSON.stringify(corrupt));
  for (let index = 0; index < invalids.length; index += 1) {
    const result = await importArchive(invalids[index], 2, 20 + index);
    if (!result.events.some((event) => event.operation === 20 + index && event.failure?.startsWith("invalid:"))) throw new Error(`invalid archive ${index} was accepted`);
    if ((await load("save", "primary")).value?.payload !== "preserved-newer") throw new Error(`invalid archive ${index} changed committed data`);
  }

  const retry = await importArchive(validArchive, 2, 23);
  if (!retry.events.some((event) => event.kind === "imported" && event.operation === 23)) throw new Error("valid retry did not commit");
  await persist("workspace", "extra", 1, "extra", "remove-me", 2, 24);
  await importArchive(validArchive, 2, 25);
  if ((await load("workspace", "extra")).value != null) throw new Error("archive replacement was not atomic/complete");

  await persist("save", "primary", 100, "protected", "protected-before-interruption", 3, 26);
  await page.evaluate((args) => { window.svgPersistence.importArchive(...args); window.svgPersistence.dispose(); }, [validArchive, 3, 27]);
  await page.evaluate((name) => window.svgPersistence.mount(name, 10000), db);
  await waitFor(() => window.svgPersistence.observe().ready, "database did not reopen after interruption");
  const afterInterruption = await load("save", "primary");
  if (afterInterruption.value?.payload !== "protected-before-interruption") throw new Error(`interrupted import changed committed data: ${JSON.stringify(afterInterruption)}`);

  const failureDb = `${db}-failure`;
  await deleteDatabase(failureDb);
  await page.evaluate((name) => {
    window.svgPersistence.mount(name, 10000);
    window.svgPersistence.persist("save", "primary", 1, "early", "early", 1, 1);
  }, failureDb);
  const earlyFailure = await waitFor(
    () => window.svgPersistence.observe().events.some((event) => event.failure?.startsWith("database:")),
    "early database failure was not reported");
  await waitFor(() => window.svgPersistence.observe().ready, "failed database did not recover readiness");
  const recovered = await persist("save", "primary", 1, "retry", "retry", 1, 2);
  if (!recovered.events.some((event) => event.kind === "persisted" && event.operation === 2)) throw new Error("database retry did not recover");
  const disposed = await page.evaluate(() => window.svgPersistence.dispose());
  if (!disposed.disposed || disposed.ready || disposed.pending !== 0 || !disposed.events.some((event) => event.kind === "disposed")) throw new Error(`dispose leaked ownership: ${JSON.stringify(disposed)}`);

  evidence = {
    schema: "fsgg.svg-present.persistence-browser/v1",
    browserFamily: family,
    result: "pass",
    families,
    reload: reloaded,
    staleRefused: true,
    quotaRefused: true,
    newerBytesPreserved: true,
    archive: { memberCount: parsed.members.length, traversalRefused: true, missingRefused: true, hashRefused: true, retryCommitted: true, replacementRemovedUnexpectedRecord: true },
    interruptionPreservedCommit: true,
    databaseFailureRecovered: earlyFailure.events.some((event) => event.failure?.startsWith("database:")),
    disposed: { ready: disposed.ready, pending: disposed.pending, disposed: disposed.disposed },
    eventOrder: recovered.events.map((event) => event.kind),
  };
  if (consoleErrors.length) throw new Error(`browser errors: ${consoleErrors.join("\n")}`);
  if (output) writeFileSync(output, `${JSON.stringify(evidence, null, 2)}\n`);
  console.log(`svg-persistence-browser: browser=${family} result=pass members=${parsed.members.length}`);
} finally {
  await browser.close();
  await new Promise((done) => server.close(done));
}
