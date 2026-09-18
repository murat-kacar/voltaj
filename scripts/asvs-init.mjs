// G1: one-off bootstrap of the ASVS audit record from the OFFICIAL, versioned release asset - never from
// memory, so no requirement id or text is invented (R1). Refuses to overwrite an existing audit.
//
//   gh release download v5.0.0_release --repo OWASP/ASVS --pattern '*5.0.0_en.flat.json' --dir <dir>
//   node scripts/asvs-init.mjs <dir>/OWASP_Application_Security_Verification_Standard_5.0.0_en.flat.json
//
// Requirement text (c) OWASP Foundation, CC BY-SA 4.0 - it is reproduced in the audit so an assessor reads
// the requirement next to its status, and is attributed in the file header.
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";

const sourcePath = process.argv[2];
const outputPath = "docs/security/asvs-5.0.0-audit.json";
if (!sourcePath) { console.error("usage: node scripts/asvs-init.mjs <official flat.json>"); process.exit(2); }
if (existsSync(outputPath)) { console.error(`asvs-init: ${outputPath} already exists - refusing to overwrite recorded assessments.`); process.exit(1); }

const raw = readFileSync(sourcePath);
const requirements = JSON.parse(raw.toString("utf8")).requirements;

// Chapters that describe a technology this codebase does not use at all. Each reason is a reproducible check.
const notApplicableChapters = {
  V10: "No OAuth 2.0 / OpenID Connect: grep -i 'oauth|openid|oidc|OpenIdConnect|redirect_uri|client_secret|authorization_code|/authorize' over src/ and frontend/src/ and for OAuth/OIDC PackageReferences in src/**/*.csproj finds nothing (2026-09-18). The API issues and validates its own JWTs (see V9); it is not an OAuth client, authorization server or resource server.",
  V17: "No WebRTC: grep -i 'webrtc|RTCPeerConnection|RTCDataChannel|getUserMedia|STUN|TURN|MediaStream' over src/ and frontend/src/ finds nothing (2026-09-18).",
};

const rows = requirements.map((r) => {
  const reason = notApplicableChapters[r.chapter_id];
  return {
    id: r.req_id,
    chapter: r.chapter_id,
    section: r.section_id,
    level: Number(r.L),
    description: r.req_description,
    status: reason ? "not-applicable" : "not-assessed",
    evidence: reason ?? "",
    assessedOn: reason ? "2026-09-18" : "",
  };
});

const header = {
  standard: "OWASP Application Security Verification Standard 5.0.0",
  license: "Requirement text (c) OWASP Foundation, licensed CC BY-SA 4.0 - https://creativecommons.org/licenses/by-sa/4.0/",
  source: {
    release: "OWASP/ASVS v5.0.0_release",
    asset: "OWASP_Application_Security_Verification_Standard_5.0.0_en.flat.json",
    sha256: createHash("sha256").update(raw).digest("hex"),
    requirementCount: rows.length,
  },
  howToRead: "level = the LOWEST ASVS level at which the requirement applies; a declared Level N covers every requirement with level <= N. statuses: not-assessed | met | partially-met | not-met | not-applicable. Every status except not-assessed needs evidence (a file/test/command someone can re-check).",
};

const body = rows.map((row) => "    " + JSON.stringify(row)).join(",\n");
const headerText = JSON.stringify(header, null, 2).slice(0, -2); // drop the closing "\n}" to append the requirements
mkdirSync("docs/security", { recursive: true });
writeFileSync(outputPath, `${headerText},\n  "requirements": [\n${body}\n  ]\n}\n`);

const count = (status) => rows.filter((r) => r.status === status).length;
console.log(`asvs-init: wrote ${outputPath} - ${rows.length} requirements (${count("not-applicable")} triaged not-applicable, ${count("not-assessed")} not assessed).`);
