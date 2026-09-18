// G10: SPDX license policy over the CycloneDX SBOMs. Usage: node scripts/check-licenses.mjs [sbomDir] [policyFile]
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";

const dir = process.argv[2] ?? "sbom";
const policy = JSON.parse(readFileSync(process.argv[3] ?? "license-policy.json", "utf8"));
const allowed = new Set(policy.allowed);
const exceptions = policy.exceptions ?? {};

// --- SPDX license expression: id | id+ | id WITH exception | expr AND expr | expr OR expr | ( expr ) ---
function parse(expression) {
  const tokens = expression.match(/\(|\)|[^\s()]+/g) ?? [];
  let i = 0;
  const peek = () => tokens[i]?.toUpperCase();
  const parseOr = () => {
    let left = parseAnd();
    while (peek() === "OR") { i++; left = { op: "OR", left, right: parseAnd() }; }
    return left;
  };
  const parseAnd = () => {
    let left = parsePrimary();
    while (peek() === "AND") { i++; left = { op: "AND", left, right: parsePrimary() }; }
    return left;
  };
  const parsePrimary = () => {
    const token = tokens[i++];
    if (token === "(") {
      const inner = parseOr();
      if (tokens[i++] !== ")") throw new Error("unbalanced parentheses");
      return inner;
    }
    if (token === undefined || ["AND", "OR", ")", "WITH"].includes(token.toUpperCase())) throw new Error(`unexpected '${token}'`);
    if (peek() === "WITH") { i++; return { id: token, exception: tokens[i++] }; }
    return { id: token };
  };
  const tree = parseOr();
  if (i !== tokens.length) throw new Error(`trailing '${tokens[i]}'`);
  return tree;
}

const satisfied = (node) => {
  if (node.op === "OR") return satisfied(node.left) || satisfied(node.right);
  if (node.op === "AND") return satisfied(node.left) && satisfied(node.right);
  if (node.exception) return false;
  return allowed.has(node.id) || allowed.has(node.id.replace(/\+$/, ""));
};

// Every license the BOM lists for a component must be acceptable (strictest reading: CycloneDX does not
// say whether several entries are alternatives or cumulative).
function verdict(component) {
  const choices = component.licenses ?? [];
  if (choices.length === 0) return { ok: false, why: "no license information" };
  for (const choice of choices) {
    const text = choice.expression ?? choice.license?.id;
    if (!text) return { ok: false, why: `license '${choice.license?.name ?? "?"}' is not an SPDX id` };
    try {
      if (!satisfied(parse(text))) return { ok: false, why: `'${text}' is not allowed by the policy` };
    } catch (error) {
      return { ok: false, why: `'${text}' is not a valid SPDX expression (${error.message})` };
    }
  }
  return { ok: true };
}

let violations = 0;
let checked = 0;
const seen = new Set();
const excepted = [];

for (const file of readdirSync(dir).filter((f) => f.endsWith(".cdx.json")).sort()) {
  const bom = JSON.parse(readFileSync(join(dir, file), "utf8"));
  for (const component of bom.components ?? []) {
    checked++;
    for (const choice of component.licenses ?? []) seen.add(choice.expression ?? choice.license?.id ?? choice.license?.name);
    const { ok, why } = verdict(component);
    if (ok) continue;
    const reason = exceptions[`${component.name}@${component.version}`] ?? exceptions[component.name];
    if (reason) { excepted.push(`${component.name}@${component.version} (${reason})`); continue; }
    console.error(`check-licenses: VIOLATION ${file}: ${component.name}@${component.version} - ${why}`);
    violations++;
  }
}

for (const note of excepted) console.log(`check-licenses: exception recorded for ${note}`);
if (violations > 0) {
  console.error(`check-licenses: FAILED - ${violations} component(s) outside the policy (add an allowed license or an exception WITH a reason in license-policy.json).`);
  process.exit(1);
}
console.log(`check-licenses: ${checked} components checked, all within the policy (${[...seen].sort().join(", ")}).`);
