#!/usr/bin/env node
/**
 * One-shot build helper: deep-merges every messages/fragments/*.<locale>.json
 * into the canonical messages/<locale>.json catalogs, then removes the
 * fragments directory. Fragments are how parallel migration workers hand off
 * new namespaces without conflicting on the shared catalog files.
 *
 * Usage: node scripts/merge-i18n-fragments.mjs
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MESSAGES = path.resolve(__dirname, '..', 'messages');
const FRAGMENTS = path.join(MESSAGES, 'fragments');

const isObject = (v) => v && typeof v === 'object' && !Array.isArray(v);

const deepMerge = (target, source) => {
  for (const key of Object.keys(source)) {
    if (isObject(source[key]) && isObject(target[key])) deepMerge(target[key], source[key]);
    else target[key] = source[key];
  }
  return target;
};

if (!fs.existsSync(FRAGMENTS)) {
  console.log('No fragments directory; nothing to merge.');
  process.exit(0);
}

const locales = ['en', 'es'];
for (const locale of locales) {
  const catalogPath = path.join(MESSAGES, `${locale}.json`);
  const catalog = JSON.parse(fs.readFileSync(catalogPath, 'utf8'));
  const fragments = fs
    .readdirSync(FRAGMENTS)
    .filter((f) => f.endsWith(`.${locale}.json`))
    .sort();
  for (const frag of fragments) {
    const fragObj = JSON.parse(fs.readFileSync(path.join(FRAGMENTS, frag), 'utf8'));
    deepMerge(catalog, fragObj);
    console.log(`merged ${frag} -> ${locale}.json`);
  }
  fs.writeFileSync(catalogPath, JSON.stringify(catalog, null, 2) + '\n', 'utf8');
}

fs.rmSync(FRAGMENTS, { recursive: true, force: true });
console.log('Fragments merged and removed.');
