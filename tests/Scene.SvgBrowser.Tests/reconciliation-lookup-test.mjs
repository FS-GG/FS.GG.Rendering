import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

// FS-GG/FS.GG.Rendering#1497: the document reconciler must find the existing sibling for each
// candidate child by keyed lookup, not by scanning the sibling array once per child.
//
// The producer's exact Emit is evaluated (as in reconciliation-source-test.mjs), against a small
// DOM model whose `getAttribute` reads are counted. The count is a deterministic operation count:
// it does not depend on time, machine or load. Behavior is pinned by a differential comparison with
// the earlier linear-search reconciler, kept below only as the oracle.
const source = await readFile(new URL("../../src/Scene.SvgBrowser/SvgBrowser.fs", import.meta.url), "utf8");
const matches = [...source.matchAll(/\[<Emit\("([^\r\n]+)"\)>\]\s*let reconcileSvg/g)];
assert.equal(matches.length, 1, "one actual document reconciler Emit");
const emit = JSON.parse(`"${matches[0][1]}"`);
// Operation counts: attribute reads, tag-name reads (every candidate probe compares tags) and
// `Set.has` probes of the used-sibling set. A probe scan shows up in the last two even when its keys
// are precomputed.
const counters = { reads: 0, tags: 0, probes: 0 };
const operations = () => counters.reads + counters.tags + counters.probes;
class CountingSet extends Set {
  has(value) { counters.probes++; return super.has(value); }
}
const compile = text => {
  const build = new Function("$0", "$1", "Set", `return ${text};`);
  return (existing, candidate) => build(existing, candidate, CountingSet);
};
const reconcile = compile(emit);

// The reconciler before #1497: `ac.find(...)` per candidate child, recomputing every key.
const LINEAR_ORACLE =
  "(function sync(a,b){const key=n=>n.getAttribute?.('data-fsgg-element-id')||n.getAttribute?.('data-fsgg-node')||n.id||null;for(const n of Array.from(a.getAttributeNames()))if(!b.hasAttribute(n))a.removeAttribute(n);for(const n of Array.from(b.getAttributeNames()))if(a.getAttribute(n)!==b.getAttribute(n))a.setAttribute(n,b.getAttribute(n));const ac=Array.from(a.children),bc=Array.from(b.children),used=new Set();if(bc.length===0){if(ac.length!==0||a.textContent!==b.textContent)a.textContent=b.textContent;return a;}if(ac.length===bc.length&&ac.every((x,i)=>key(x)===key(bc[i])&&x.tagName===bc[i].tagName)){for(let i=0;i<bc.length;i++)sync(ac[i],bc[i]);return a;}for(let i=0;i<bc.length;i++){const c=bc[i],k=key(c);let m=k?ac.find(x=>!used.has(x)&&key(x)===k&&x.tagName===c.tagName):ac.find((x,j)=>!used.has(x)&&!key(x)&&x.tagName===c.tagName&&j===i);if(m){used.add(m);sync(m,c);a.appendChild(m);}else a.appendChild(c.cloneNode(true));}for(const x of ac)if(!used.has(x)&&x.parentNode===a)x.remove();return a;})($0,$1)";
const linearOracle = compile(LINEAR_ORACLE);

class Element {
  constructor(tagName, attributes = {}, children = [], text = "") {
    this.tag = tagName;
    this.children = [];
    this.value = text;
    this.id = "";
    this.parentNode = null;
    this.attributes = new Map(Object.entries(attributes));
    for (const child of children) this.appendChild(child);
  }
  get tagName() { counters.tags++; return this.tag; }
  getAttribute(name) { counters.reads++; return this.attributes.get(name) ?? null; }
  getAttributeNames() { return [...this.attributes.keys()]; }
  hasAttribute(name) { return this.attributes.has(name); }
  setAttribute(name, value) { this.attributes.set(name, value); }
  removeAttribute(name) { this.attributes.delete(name); }
  appendChild(child) {
    if (child.parentNode) child.parentNode.children = child.parentNode.children.filter(x => x !== child);
    child.parentNode = this;
    this.children.push(child);
    return child;
  }
  remove() {
    if (this.parentNode) this.parentNode.children = this.parentNode.children.filter(x => x !== this);
    this.parentNode = null;
  }
  cloneNode() {
    return new Element(this.tag, Object.fromEntries(this.attributes), this.children.map(x => x.cloneNode()), this.value);
  }
  get textContent() { return this.children.length ? this.children.map(child => child.textContent).join("") : this.value; }
  set textContent(value) {
    for (const child of this.children) child.parentNode = null;
    this.children = [];
    this.value = value;
  }
}

const keyed = (key, tag = "g", extra = {}) => new Element(tag, { "data-fsgg-element-id": key, ...extra });
const parent = children => new Element("svg", {}, children);
const keysOf = element => element.children.map(x => x.getAttribute("data-fsgg-element-id") ?? "");

// A candidate whose children are the existing keys in reverse order: every child matches, none is
// aligned, so the keyed matching path runs for all of them.
function reversedReads(count) {
  const existing = Array.from({ length: count }, (_, i) => keyed(`k${i}`, "g", { label: `old${i}` }));
  const root = parent(existing);
  const candidate = parent(Array.from({ length: count }, (_, i) => keyed(`k${count - 1 - i}`, "g", { label: `new${i}` })));
  counters.reads = counters.tags = counters.probes = 0;
  reconcile(root, candidate);
  const reads = operations();
  assert.deepEqual(keysOf(root), Array.from({ length: count }, (_, i) => `k${count - 1 - i}`), "candidate order applied");
  assert.equal(root.children.length, count, "no child cloned or lost");
  for (const element of existing) assert.equal(element.parentNode, root, "existing element identity retained");
  return reads;
}

const small = reversedReads(100);
const large = reversedReads(400);
// The keyed lookup costs 11 operations per child (key, tag and attribute reads and used-set
// probes). A probe scan per child needs about count^2 / 2 (about 84,000 at 400 children, 163,000 for
// the earlier reconciler), so a fixed per-child budget separates them by an order of magnitude.
assert.ok(large <= 16 * 400, `keyed matching is linear in the sibling count: ${large} reads for 400 children`);
assert.ok(large <= 4.5 * small, `4x the siblings costs about 4x the reads: ${small} -> ${large}`);

// Differential: the keyed reconciler yields the same tree, element identities and attributes as the
// earlier linear one for keyed reorders, duplicates, key-with-different-tag, unkeyed positions,
// inserts and removals.
function build(spec) {
  return parent(spec.map(([key, tag = "g", label = ""]) => key === null ? new Element(tag, { label }) : keyed(key, tag, { label })));
}
function shape(root, identities) {
  return root.children.map(x => `${x.tagName}:${x.getAttribute("data-fsgg-element-id") ?? "-"}:${x.getAttribute("label")}:${identities.get(x) ?? "new"}`);
}
function runBoth(existingSpec, candidateSpec) {
  const outputs = [];
  for (const sync of [linearOracle, reconcile]) {
    const root = build(existingSpec);
    const identities = new Map(root.children.map((x, i) => [x, i]));
    sync(root, build(candidateSpec));
    outputs.push(shape(root, identities));
  }
  assert.deepEqual(outputs[1], outputs[0], `keyed reconciler equals the linear oracle for ${JSON.stringify([existingSpec, candidateSpec])}`);
  return outputs[1];
}

runBoth([["a"], ["b"], ["c"]], [["c"], ["a"], ["b"]]);
runBoth([["a"], ["b"], ["c"]], [["b"], ["x"], ["a"]]);
runBoth([["a"], ["a"], ["b"]], [["a", "g", "first"], ["b"], ["a", "g", "second"]]);
runBoth([["a"], ["a"]], [["a", "g", "1"], ["a", "g", "2"], ["a", "g", "3"]]);
runBoth([["a", "g"], ["b", "g"]], [["b", "rect"], ["a", "g"]]);
runBoth([["a", "g"]], [["a", "rect"]]);
runBoth([[null, "g", "u0"], ["a"], [null, "g", "u2"]], [["a"], [null, "g", "v1"], [null, "g", "v2"], [null, "rect", "v3"]]);
runBoth([[null, "g"], [null, "rect"]], [[null, "rect"], [null, "g"]]);
runBoth([["a"], ["b"]], []);
runBoth([], [["a"], [null, "g"]]);

// A seeded sweep of mixed structures (small key alphabet, so collisions, duplicates and tag
// mismatches are common).
let seed = 20261031;
const next = limit => (seed = (seed * 1103515245 + 12345) & 0x7fffffff) % limit;
const tags = ["g", "g", "g", "rect", "circle"];
const random = () => Array.from({ length: next(9) }, () => [next(4) === 0 ? null : `k${next(5)}`, tags[next(tags.length)], `l${next(100)}`]);
for (let round = 0; round < 400; round++) runBoth(random(), random());

// Mutants. Each stays behaviorally correct (the differential holds) and loses only the bound, so
// the operation count is what kills it.
function reversedOperations(sync, count) {
  const root = parent(Array.from({ length: count }, (_, i) => keyed(`k${i}`)));
  const candidate = parent(Array.from({ length: count }, (_, i) => keyed(`k${count - 1 - i}`)));
  counters.reads = counters.tags = counters.probes = 0;
  sync(root, candidate);
  return operations();
}
const bound = 16 * 400;
const lookup = "const q=byKey.get(k)?.get(c.tagName);m=q&&q.next<q.items.length?q.items[q.next++]:undefined;";
assert.equal(emit.split(lookup).length, 2, "mutation bound to the actual keyed lookup");
// (1) per-candidate probe over the precomputed keys
const scanning = compile(emit.replace(lookup, "m=ac.find((x,j)=>!used.has(x)&&ak[j]===k&&x.tagName===c.tagName);"));
// (2) the earlier reconciler: probe scan that also recomputes every key
const scanningOps = reversedOperations(scanning, 400);
const earlierOps = reversedOperations(linearOracle, 400);
assert.ok(scanningOps > bound, `per-candidate probe scan exceeds the bound: ${scanningOps} operations for 400 children`);
assert.ok(earlierOps > bound, `earlier linear reconciler exceeds the bound: ${earlierOps} operations for 400 children`);
assert.ok(earlierOps > 10 * large, `earlier reconciler costs an order of magnitude more: ${earlierOps} vs ${large}`);

console.log(`document reconciler keyed lookup: ${small} operations for 100 siblings, ${large} for 400; earlier linear reconciler ${earlierOps}, probe-scan mutant ${scanningOps}; differential ${10 + 400} cases passed`);
