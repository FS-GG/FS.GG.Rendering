import { createHash } from "node:crypto";
import { readFileSync, readdirSync, statSync, writeFileSync } from "node:fs";
import { resolve, relative } from "node:path";
import { suiteContract } from "./suite-contract.js";

const work = process.argv[process.argv.indexOf("--work") + 1];
if (!work || !process.argv.includes("--work")) throw new Error("--work is required");
const dist = resolve(work, "Browser/dist");
const hash = (bytes) => `sha256:${createHash("sha256").update(bytes).digest("hex")}`;
const inventory = (root) => readdirSync(root).sort().flatMap((name) => {
  const path = resolve(root, name);
  return statSync(path).isDirectory() ? inventory(path) : [path];
});
const files = inventory(dist).map((path) => ({ path: relative(dist, path), sha256: hash(readFileSync(path)), bytes: statSync(path).size }));
const packages = inventory(resolve(work, "feed")).filter((path) => path.endsWith(".nupkg")).map((path) => ({ path: relative(resolve(work, "feed"), path), sha256: hash(readFileSync(path)) }));
const source = readFileSync(resolve(work, "source-manifest.tsv"));
const manifest = {
  schema: "fsgg.svg-coherence.fixture/v1",
  artifactId: hash(JSON.stringify({ files, packages, sourceSha256: hash(source), workloadSha256: hash(JSON.stringify(suiteContract)) })),
  sourceSha256: hash(source), packages, files,
  workloadSha256: hash(JSON.stringify(suiteContract)), workload: suiteContract,
};
writeFileSync(resolve(dist, "artifact-manifest.json"), `${JSON.stringify(manifest, null, 2)}\n`);
console.log(JSON.stringify({ artifactId: manifest.artifactId, files: files.length, workloadSha256: manifest.workloadSha256 }));
