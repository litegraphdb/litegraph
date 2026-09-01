'use client';
import React from 'react';

/**
 * Minimal, dependency-free markdown renderer for assistant replies. Supports
 * headings, bold/italic, inline code, fenced code blocks, links, unordered and
 * ordered lists, and paragraphs. Builds React elements (never raw HTML), so
 * content is inherently escaped.
 */

const INLINE_TOKEN =
  /(\*\*[^*]+\*\*|\*[^*\n]+\*|`[^`\n]+`|\[[^\]\n]+\]\([^)\s]+\))/g;

const renderInline = (text: string, keyPrefix: string): React.ReactNode[] => {
  const parts = text.split(INLINE_TOKEN);
  return parts.map((part, index) => {
    const key = `${keyPrefix}-${index}`;
    if (/^\*\*[^*]+\*\*$/.test(part)) {
      return <strong key={key}>{part.slice(2, -2)}</strong>;
    }
    if (/^\*[^*\n]+\*$/.test(part)) {
      return <em key={key}>{part.slice(1, -1)}</em>;
    }
    if (/^`[^`\n]+`$/.test(part)) {
      return (
        <code
          key={key}
          style={{
            background: 'var(--ant-color-fill-tertiary)',
            borderRadius: 4,
            padding: '1px 5px',
            fontSize: '0.9em',
          }}
        >
          {part.slice(1, -1)}
        </code>
      );
    }
    const linkMatch = part.match(/^\[([^\]\n]+)\]\(([^)\s]+)\)$/);
    if (linkMatch) {
      const href = linkMatch[2];
      const safe = /^https?:\/\//i.test(href);
      return safe ? (
        <a key={key} href={href} target="_blank" rel="noreferrer noopener">
          {linkMatch[1]}
        </a>
      ) : (
        <span key={key}>{linkMatch[1]}</span>
      );
    }
    return <React.Fragment key={key}>{part}</React.Fragment>;
  });
};

type Block =
  | { kind: 'code'; language: string; lines: string[] }
  | { kind: 'heading'; level: number; text: string }
  | { kind: 'ul'; items: string[] }
  | { kind: 'ol'; items: string[] }
  | { kind: 'paragraph'; lines: string[] };

const parseBlocks = (markdown: string): Block[] => {
  const lines = markdown.replace(/\r\n/g, '\n').split('\n');
  const blocks: Block[] = [];
  let index = 0;
  while (index < lines.length) {
    const line = lines[index];
    if (/^\s*$/.test(line)) {
      index += 1;
      continue;
    }
    const fence = line.match(/^```(\S*)\s*$/);
    if (fence) {
      const codeLines: string[] = [];
      index += 1;
      while (index < lines.length && !/^```\s*$/.test(lines[index])) {
        codeLines.push(lines[index]);
        index += 1;
      }
      index += 1; // Skip the closing fence (or run off the end mid-stream).
      blocks.push({ kind: 'code', language: fence[1] || '', lines: codeLines });
      continue;
    }
    const heading = line.match(/^(#{1,6})\s+(.*)$/);
    if (heading) {
      blocks.push({ kind: 'heading', level: heading[1].length, text: heading[2] });
      index += 1;
      continue;
    }
    if (/^\s*[-*]\s+/.test(line)) {
      const items: string[] = [];
      while (index < lines.length && /^\s*[-*]\s+/.test(lines[index])) {
        items.push(lines[index].replace(/^\s*[-*]\s+/, ''));
        index += 1;
      }
      blocks.push({ kind: 'ul', items });
      continue;
    }
    if (/^\s*\d+[.)]\s+/.test(line)) {
      const items: string[] = [];
      while (index < lines.length && /^\s*\d+[.)]\s+/.test(lines[index])) {
        items.push(lines[index].replace(/^\s*\d+[.)]\s+/, ''));
        index += 1;
      }
      blocks.push({ kind: 'ol', items });
      continue;
    }
    const paragraphLines: string[] = [];
    while (
      index < lines.length &&
      !/^\s*$/.test(lines[index]) &&
      !/^```/.test(lines[index]) &&
      !/^(#{1,6})\s+/.test(lines[index]) &&
      !/^\s*[-*]\s+/.test(lines[index]) &&
      !/^\s*\d+[.)]\s+/.test(lines[index])
    ) {
      paragraphLines.push(lines[index]);
      index += 1;
    }
    blocks.push({ kind: 'paragraph', lines: paragraphLines });
  }
  return blocks;
};

const Markdown = ({ content }: { content: string }) => {
  const blocks = parseBlocks(content || '');
  return (
    <div className="lg-markdown" data-testid="chat-markdown">
      {blocks.map((block, blockIndex) => {
        const key = `b-${blockIndex}`;
        switch (block.kind) {
          case 'code':
            return (
              <pre
                key={key}
                style={{
                  background: 'var(--ant-color-fill-tertiary)',
                  borderRadius: 8,
                  padding: 12,
                  overflowX: 'auto',
                  fontSize: 12.5,
                  margin: '8px 0',
                }}
              >
                <code>{block.lines.join('\n')}</code>
              </pre>
            );
          case 'heading': {
            const HeadingTag = `h${Math.min(block.level + 2, 6)}` as keyof React.JSX.IntrinsicElements;
            return (
              <HeadingTag key={key}>{renderInline(block.text, key)}</HeadingTag>
            );
          }
          case 'ul':
            return (
              <ul key={key}>
                {block.items.map((item, itemIndex) => (
                  <li key={`${key}-${itemIndex}`}>{renderInline(item, `${key}-${itemIndex}`)}</li>
                ))}
              </ul>
            );
          case 'ol':
            return (
              <ol key={key}>
                {block.items.map((item, itemIndex) => (
                  <li key={`${key}-${itemIndex}`}>{renderInline(item, `${key}-${itemIndex}`)}</li>
                ))}
              </ol>
            );
          case 'paragraph':
          default:
            return <p key={key}>{renderInline(block.lines.join(' '), key)}</p>;
        }
      })}
    </div>
  );
};

export default Markdown;
