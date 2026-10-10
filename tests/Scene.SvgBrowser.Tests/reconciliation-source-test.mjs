import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

// Evaluate the producer's exact Emit, rather than a second reconciler implementation.
const source = await readFile(new URL("../../src/Scene.SvgBrowser/SvgBrowser.fs", import.meta.url), "utf8");
const matches = [...source.matchAll(/\[<Emit\("([^\r\n]+)"\)>\]\s*let reconcileSvg/g)];
assert.equal(matches.length, 1, "one actual document reconciler Emit");
const emit = JSON.parse(`"${matches[0][1]}"`);
const reconcile = new Function("$0", "$1", `return ${emit};`);

// Only the DOM properties used by these empty-container cases are modeled here.
// The packed browser route remains responsible for actual browser behavior.
class Element {
  constructor(tagName, children = [], text = "") {
    this.tagName = tagName;
    this.children = children;
    this.value = text;
    this.id = "";
    this.attributes = new Map();
    this.textWrites = 0;
    for (const child of children) child.parentNode = this;
  }
  getAttribute(name) { return this.attributes.get(name) ?? null; }
  getAttributeNames() { return [...this.attributes.keys()]; }
  hasAttribute(name) { return this.attributes.has(name); }
  setAttribute(name, value) { this.attributes.set(name, value); }
  removeAttribute(name) { this.attributes.delete(name); }
  get textContent() { return this.children.length ? this.children.map(child => child.textContent).join("") : this.value; }
  set textContent(value) {
    this.textWrites++;
    for (const child of this.children) child.parentNode = null;
    this.children = [];
    this.value = value;
  }
}

function emptyCases(sync) {
  const symbol = new Element("symbol", [new Element("g", [new Element("circle")])]);
  const definitions = new Element("defs", [symbol]);
  assert.equal(sync(definitions, new Element("defs")), definitions, "definitions owner retained");
  assert.equal(definitions.children.length, 0, "textless definitions cleared");
  assert.equal(symbol.parentNode, null, "obsolete symbol detached");

  const circle = new Element("circle");
  const group = new Element("g", [circle]);
  sync(group, new Element("g"));
  assert.equal(group.children.length, 0, "generic textless container cleared");
  assert.equal(circle.parentNode, null, "obsolete primitive detached");

  const child = new Element("text", [], "same");
  const textWrapper = new Element("g", [child]);
  sync(textWrapper, new Element("g", [], "same"));
  assert.equal(textWrapper.children.length, 0, "equal text does not preserve element children");
  assert.equal(textWrapper.textContent, "same", "candidate text retained");
  assert.equal(child.parentNode, null, "old text element detached");

  const nestedSymbol = new Element("symbol", [new Element("g", [new Element("circle")])]);
  const nested = new Element("defs", [nestedSymbol]);
  sync(nested, new Element("defs", [new Element("symbol")]));
  assert.equal(nested.children[0], nestedSymbol, "aligned keyed/tag owner retained");
  assert.equal(nestedSymbol.children.length, 0, "recursive empty-container replacement");
}
emptyCases(reconcile);

const leaf = new Element("text", [], "same");
assert.equal(reconcile(leaf, new Element("text", [], "same")), leaf, "text leaf identity retained");
assert.equal(leaf.textWrites, 0, "equal text leaf remains a no-op");
reconcile(leaf, new Element("text", [], "changed"));
assert.equal(leaf.textContent, "changed", "changed text leaf updated");
assert.equal(leaf.textWrites, 1, "changed text written once");
const empty = new Element("g");
reconcile(empty, new Element("g"));
assert.equal(empty.textWrites, 0, "already empty container remains a no-op");

// Removing the repair must fail the behavioral corpus at the original predicate.
const repaired = "if(ac.length!==0||a.textContent!==b.textContent)";
assert.equal(emit.split(repaired).length, 2, "mutation bound to actual repaired condition");
const mutant = new Function("$0", "$1", `return ${emit.replace(repaired, "if(a.textContent!==b.textContent)")};`);
assert.throws(() => emptyCases(mutant), /textless definitions cleared/, "original producer mutant killed");
console.log("document reconciler: empty containers, retained owners, text-leaf no-op and original mutant passed");
