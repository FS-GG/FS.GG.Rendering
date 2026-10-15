import { createHash } from "node:crypto";
import { readFileSync, readdirSync, statSync, lstatSync, writeFileSync, cpSync } from "node:fs";
import { resolve, relative } from "node:path";
import { suiteContract, workloadForProfile, validateSoldierInput } from "./suite-contract.js";

const work = process.argv[process.argv.indexOf("--work") + 1];
if (!work || !process.argv.includes("--work")) throw new Error("--work is required");
const dist = resolve(work, "Browser/dist");
const hash = (bytes) => `sha256:${createHash("sha256").update(bytes).digest("hex")}`;
const inventory = (root) => readdirSync(root).sort().flatMap((name) => {
  const path = resolve(root, name);
  if (lstatSync(path).isSymbolicLink()) throw new Error("Fixture symlink refused");
  return statSync(path).isDirectory() ? inventory(path) : [path];
});
const profile = process.argv.includes("--profile") ? process.argv[process.argv.indexOf("--profile") + 1] : "controls";
const argument = (name) => { if (!process.argv.includes(name)) throw new Error(`${name} required`); return process.argv[process.argv.indexOf(name) + 1]; };
let input;
if (profile === "soldier-functional") {
  input = validateSoldierInput(JSON.parse(readFileSync(argument("--input-metadata"), "utf8")));
  const player = resolve(argument("--player-dist")), adapter = resolve(argument("--adapter"));
  for (const path of [...inventory(player), adapter]) if (lstatSync(path).isSymbolicLink() || !lstatSync(path).isFile()) throw new Error("Nonregular staged player input");
  if (hash(readFileSync(adapter)) !== input.adapterSha256) throw new Error("Soldier adapter input digest mismatch");
  if (!readFileSync(resolve(player, "index.html"), "utf8").includes('/soldier-player/')) throw new Error("Actual player build base mismatch");
  cpSync(player, resolve(dist, "soldier-player"), { recursive: true, errorOnExist: true, force: false });
  cpSync(adapter, resolve(dist, "soldier-staged-journey.js"), { errorOnExist: true, force: false });
}
const workload = workloadForProfile(profile, input);
const files = inventory(dist).filter((path) => path !== resolve(dist, "artifact-manifest.json")).map((path) => ({ path: relative(dist, path), sha256: hash(readFileSync(path)), bytes: statSync(path).size }));
const packages = inventory(resolve(work, "feed")).filter((path) => path.endsWith(".nupkg")).map((path) => ({ path: relative(resolve(work, "feed"), path), sha256: hash(readFileSync(path)) }));
const source = readFileSync(resolve(work, "source-manifest.tsv"));
const manifest = {
  schema: "fsgg.svg-coherence.fixture/v1",
  artifactId: hash(JSON.stringify({ files, packages, sourceSha256: hash(source), workloadSha256: hash(JSON.stringify(workload)) })),
  sourceSha256: hash(source), packages, files,
  workloadSha256: hash(JSON.stringify(workload)), workload,
};
writeFileSync(resolve(dist, "artifact-manifest.json"), `${JSON.stringify(manifest, null, 2)}\n`);
console.log(JSON.stringify({ artifactId: manifest.artifactId, files: files.length, workloadSha256: manifest.workloadSha256 }));
