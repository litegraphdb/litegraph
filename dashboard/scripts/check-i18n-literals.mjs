#!/usr/bin/env node
/**
 * i18n literal guard.
 *
 * Scans the FULLY-migrated dashboard files for suspicious hard-coded,
 * user-facing string literals and fails (exit 1) if any are found. This keeps
 * newly-introduced English strings from silently landing in surfaces that are
 * supposed to be fully localized.
 *
 * It is intentionally pragmatic: it only enforces the files listed in
 * ENFORCED_GLOBS (the surfaces we have fully migrated), and it allowlists
 * technical tokens, icons, keys, css and test ids. Partially-migrated pages are
 * NOT enforced yet (they carry a `TODO(i18n)` marker).
 *
 * Usage: node scripts/check-i18n-literals.mjs
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, '..');
const SRC = path.join(ROOT, 'src');

// Directories / files that must be fully localized.
const ENFORCED_DIRS = [
  'page/graphs',
  'page/nodes',
  'page/edges',
  'page/login',
  'page/admin-login',
  'page/user-dashboard/home',
];
const ENFORCED_FILES = ['components/layout/DashboardLayout.tsx', 'components/navigation.tsx'];

// JSX attributes whose string values are user-facing.
const UI_ATTRS = [
  'title',
  'placeholder',
  'label',
  'okText',
  'cancelText',
  'tooltip',
  'description',
  'content',
  'alt',
  'aria-label',
  'tooltipTitle',
  'confirmText',
];

// Technical tokens that are fine to appear verbatim.
const ALLOWED_TOKENS = new Set([
  'GUID',
  'JSON',
  'GEXF',
  'JSONL',
  'HNSW',
  'UTC',
  'N/A',
  'LiteGraph',
  'ID',
  'URL',
  'API',
  'SSO',
  'HTTP',
  'HTTPS',
  'DB',
  'RAM',
  'Sqlite',
  'GB',
  'MB',
  'KB',
]);

const findings = [];

const listFiles = (dir) => {
  const out = [];
  if (!fs.existsSync(dir)) return out;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...listFiles(full));
    else if (/\.(tsx|ts)$/.test(entry.name) && !/\.d\.ts$/.test(entry.name)) out.push(full);
  }
  return out;
};

const targetFiles = () => {
  const files = new Set();
  for (const d of ENFORCED_DIRS) listFiles(path.join(SRC, d)).forEach((f) => files.add(f));
  for (const f of ENFORCED_FILES) {
    const full = path.join(SRC, f);
    if (fs.existsSync(full)) files.add(full);
  }
  return [...files];
};

// A "word-ish" phrase looks like human copy: has a letter and either a space
// or starts with an uppercase letter, and is not purely an allowlisted token.
const looksLikeCopy = (raw) => {
  const text = raw.trim();
  if (!text) return false;
  if (!/[A-Za-z]/.test(text)) return false; // no letters => not copy
  if (text.length < 3) return false;
  // Strip surrounding punctuation for token check.
  const tokens = text.split(/\s+/).filter(Boolean);
  const allAllowed = tokens.every((tk) => ALLOWED_TOKENS.has(tk.replace(/[.,:!?]/g, '')));
  if (allAllowed) return false;
  // ICU / interpolation-only or expression-only fragments are fine.
  if (/^[{}\s.]*$/.test(text)) return false;
  const hasSpace = /\s/.test(text);
  const startsUpper = /^[A-Z]/.test(text);
  return hasSpace || startsUpper;
};

const scanFile = (file) => {
  const rel = path.relative(ROOT, file);
  const src = fs.readFileSync(file, 'utf8');
  const lines = src.split(/\r?\n/);

  lines.forEach((line, idx) => {
    const trimmed = line.trim();
    // Skip comments and imports.
    if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('/*')) return;
    if (trimmed.startsWith('import ')) return;
    if (trimmed.includes('eslint-disable')) return;
    let m;
    // 1) toast / message calls with a raw string literal.
    const toastRe = /\b(?:toast|message)\.(?:success|error|warning|info|loading)\(\s*(['"])(.*?)\1/g;
    while ((m = toastRe.exec(line))) {
      if (looksLikeCopy(m[2])) {
        findings.push({ rel, line: idx + 1, kind: 'toast/message', text: m[2] });
      }
    }
    // 2) UI attributes with a raw string literal: attr="literal"
    for (const attr of UI_ATTRS) {
      const attrRe = new RegExp(`(?:^|\\s)${attr}=(['"])(.*?)\\1`, 'g');
      while ((m = attrRe.exec(line))) {
        if (looksLikeCopy(m[2])) {
          findings.push({ rel, line: idx + 1, kind: `attr:${attr}`, text: m[2] });
        }
      }
    }
    // 3) JSX text between tags: >Some Copy<
    // Guard against TypeScript that looks like tags: arrow return types
    // (`=> Promise<void>`) and generics. Skip when the leading `>` is part of
    // `=>`, and skip captures that are a single PascalCase type-ish token.
    const textRe = />([^<>{}]+)</g;
    while ((m = textRe.exec(line))) {
      const before = line[m.index - 1];
      if (before === '=' || before === '-') continue; // arrow `=>` / `->`
      const captured = m[1].trim();
      // A lone identifier immediately opening a generic is almost certainly a
      // type (e.g. ` Promise ` from `=> Promise<void>`, ` Record `), not copy.
      if (/^[A-Za-z][A-Za-z0-9]*$/.test(captured) && !/\s/.test(captured)) {
        // Single word with no space: only flag if it clearly reads as a UI word
        // AND isn't a known TS type token.
        if (/^(Promise|Record|Array|Partial|Readonly|Map|Set|ReturnType)$/.test(captured)) continue;
      }
      if (looksLikeCopy(m[1])) {
        findings.push({ rel, line: idx + 1, kind: 'jsx-text', text: captured });
      }
    }
  });
};

targetFiles().forEach(scanFile);

if (findings.length > 0) {
  console.error(`\n✖ i18n literal check failed: ${findings.length} hard-coded string(s) found in fully-migrated files.\n`);
  for (const f of findings) {
    console.error(`  ${f.rel}:${f.line}  [${f.kind}]  "${f.text}"`);
  }
  console.error('\nMove these strings into messages/*.json and use useTranslations()/getTranslator().\n');
  process.exit(1);
}

console.log('✓ i18n literal check passed — no hard-coded user-facing strings in fully-migrated files.');
