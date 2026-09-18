// G1 gate: the ASVS target level is DECLARED (catalog-info.yaml) and AUDITED (docs/security/asvs-5.0.0-audit.json).
// The audit is complete only when no requirement in scope for the declared level is still `not-assessed`
// and every assessed one carries evidence. Findings (not-met / partially-met) do not fail this gate - they
// are the audit's OUTPUT, listed below as work; the property gated here is that the audit has been done.
// Usage: node scripts/check-asvs.mjs [catalogFile] [auditFile]
import { readFileSync } from "node:fs";

const catalogPath = process.argv[2] ?? "catalog-info.yaml";
const auditPath = process.argv[3] ?? "docs/security/asvs-5.0.0-audit.json";
const validStatuses = new Set(["not-assessed", "met", "partially-met", "not-met", "not-applicable"]);

const errors = [];
const fail = (message) => errors.push(message);

// Anchored to the start of the line so a YAML comment that merely mentions the annotation cannot be mistaken for it.
const declared = readFileSync(catalogPath, "utf8").match(/^\s*voltflow\.io\/asvs-target-level:\s*"?([^"\s#]*)"?/m)?.[1] ?? "";
if (!["1", "2", "3"].includes(declared)) {
  fail(`${catalogPath} does not declare a valid voltflow.io/asvs-target-level (found '${declared || "nothing"}'; must be 1, 2 or 3).`);
}

let audit;
try {
  audit = JSON.parse(readFileSync(auditPath, "utf8"));
} catch (error) {
  fail(`${auditPath} is missing or not valid JSON (${error.message}).`);
}

if (audit) {
  const rows = audit.requirements ?? [];
  if (rows.length !== audit.source?.requirementCount) {
    fail(`the audit lists ${rows.length} requirements but the ASVS release it was generated from has ${audit.source?.requirementCount} - a row was added or deleted.`);
  }
  const ids = new Set(rows.map((row) => row.id));
  if (ids.size !== rows.length) fail("the audit contains duplicate requirement ids.");
  for (const row of rows) {
    if (!validStatuses.has(row.status)) fail(`${row.id}: unknown status '${row.status}'.`);
  }

  if (declared) {
    const level = Number(declared);
    const scope = rows.filter((row) => row.level <= level);
    const by = (status) => scope.filter((row) => row.status === status);
    const open = by("not-assessed");
    const noEvidence = scope.filter((row) => row.status !== "not-assessed" && !(row.evidence ?? "").trim());
    const gaps = [...by("not-met"), ...by("partially-met")];

    console.log(`check-asvs: declared ASVS 5.0 level ${declared} - ${scope.length} requirements in scope.`);
    console.log(`check-asvs: met ${by("met").length}, not-applicable ${by("not-applicable").length}, partially-met ${by("partially-met").length}, not-met ${by("not-met").length}, NOT ASSESSED ${open.length}.`);

    if (open.length > 0) {
      const perChapter = {};
      for (const row of open) perChapter[row.chapter] = (perChapter[row.chapter] ?? 0) + 1;
      console.log(`check-asvs: still to assess, per chapter: ${Object.entries(perChapter).map(([chapter, n]) => `${chapter}=${n}`).join("  ")}`);
      fail(`${open.length} of ${scope.length} in-scope requirements have not been assessed - the audit is incomplete.`);
    }
    if (noEvidence.length > 0) {
      fail(`${noEvidence.length} assessed requirement(s) have no evidence (first: ${noEvidence.slice(0, 5).map((row) => row.id).join(", ")}) - a status nobody can re-check is not an audit.`);
    }
    if (gaps.length > 0) {
      console.log(`check-asvs: ${gaps.length} open finding(s) to remediate: ${gaps.slice(0, 12).map((row) => row.id).join(", ")}${gaps.length > 12 ? ", ..." : ""}`);
    }
  }
}

if (errors.length > 0) {
  for (const message of errors) console.error(`check-asvs: FAILED - ${message}`);
  process.exit(1);
}
console.log("check-asvs: the declared level is audited in full.");
