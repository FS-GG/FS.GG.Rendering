// Validate every arm run and write the calibration, gate or smoke summary.
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { analyse, markdown } from "./hosted-benchmark-analysis.mjs";

const argument = (name) => { const i = process.argv.indexOf(name); if (i < 0 || i + 1 >= process.argv.length) throw new Error(`${name} required`); return process.argv[i + 1]; };
const here = dirname(fileURLToPath(import.meta.url));
const workload = JSON.parse(readFileSync(resolve(here, "hosted-benchmark-workload.json"), "utf8"));
const runsDir = argument("--runs");
const runs = readdirSync(runsDir).filter((name) => /^run-t\d-(baseline|candidate)\.json$/.test(name)).sort().map((name) => JSON.parse(readFileSync(resolve(runsDir, name), "utf8")));
const options = { mode: argument("--mode"), sustainedSeconds: Number(argument("--sustained-seconds")) };
if (process.argv.includes("--tolerance")) options.tolerance = Number(argument("--tolerance"));
const summary = analyse(runs, workload, options);
writeFileSync(argument("--out"), `${JSON.stringify(summary, null, 2)}\n`);
writeFileSync(argument("--markdown"), markdown(summary));
console.log(JSON.stringify({ mode: summary.mode, gated: summary.gatedRecordCount, spread: summary.observedSpread.value, tolerance: summary.tolerance?.value ?? null, gate: summary.gate?.verdict ?? null }));
if (summary.gate?.verdict === "regression") process.exitCode = 3;
