// G8: an SBOM that is empty or malformed is worse than none - it reads as compliance. Structural checks
// plus a "canary" component per BOM (a package the shipped app certainly depends on) so a BOM generated
// from the wrong project or an empty lock file cannot pass.
import { readFileSync } from "node:fs";
import { join } from "node:path";

const dir = process.argv[2] ?? "sbom";
const expectations = [
  { file: "backend.cdx.json", canary: "Microsoft.EntityFrameworkCore" },
  { file: "frontend.cdx.json", canary: "react" },
];

let failed = false;
const fail = (message) => { console.error(`validate-sbom: FAILED - ${message}`); failed = true; };

for (const { file, canary } of expectations) {
  let bom;
  try {
    bom = JSON.parse(readFileSync(join(dir, file), "utf8"));
  } catch (error) {
    fail(`${file} is missing or not valid JSON (${error.message})`);
    continue;
  }
  if (bom.bomFormat !== "CycloneDX") fail(`${file}: bomFormat is '${bom.bomFormat}', expected 'CycloneDX'`);
  if (typeof bom.specVersion !== "string") fail(`${file}: specVersion is missing`);
  const components = Array.isArray(bom.components) ? bom.components : [];
  if (components.length === 0) fail(`${file}: no components - the BOM describes nothing`);
  const unnamed = components.filter((c) => !c.name || !c.version).length;
  if (unnamed > 0) fail(`${file}: ${unnamed} component(s) without a name or version`);
  if (!components.some((c) => c.name === canary)) fail(`${file}: canary component '${canary}' is absent - wrong project or empty lock file?`);
  if (!failed) console.log(`validate-sbom: ${file} ok - CycloneDX ${bom.specVersion}, ${components.length} components.`);
}

process.exit(failed ? 1 : 0);
