'use client';
import React from 'react';
import CopyButton from '@/components/base/copy-button/CopyButton';

/**
 * Dependency-free GFM-flavored markdown renderer for assistant replies.
 * Supports headings, bold/italic/strikethrough, inline code, fenced code
 * blocks (with language label and copy button), links, tables, blockquotes,
 * horizontal rules, task lists, nested unordered/ordered lists, and
 * paragraphs. Builds React elements (never raw HTML), so content is
 * inherently escaped. Tolerates unterminated fences/tables mid-stream.
 */

const INLINE_TOKEN =
  /(`[^`\n]+`|\*\*[^*\n](?:[^\n]*?[^*\n])?\*\*|__[^_\n](?:[^\n]*?[^_\n])?__|~~[^~\n](?:[^\n]*?[^~\n])?~~|\*[^*\s][^*\n]*?\*|_[^_\s][^_\n]*?_|\[[^\]\n]+\]\([^)\s]+\))/g;

const renderInline = (text: string, keyPrefix: string): React.ReactNode[] => {
  const parts = text.split(INLINE_TOKEN);
  return parts.map((part, index) => {
    const key = `${keyPrefix}-${index}`;
    if (part === undefined || part === '') return <React.Fragment key={key} />;
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
    if (/^\*\*[\s\S]+\*\*$/.test(part) || /^__[\s\S]+__$/.test(part)) {
      return <strong key={key}>{renderInline(part.slice(2, -2), key)}</strong>;
    }
    if (/^~~[\s\S]+~~$/.test(part)) {
      return <del key={key}>{renderInline(part.slice(2, -2), key)}</del>;
    }
    if (/^\*[^*\s][\s\S]*\*$/.test(part) || /^_[^_\s][\s\S]*_$/.test(part)) {
      return <em key={key}>{renderInline(part.slice(1, -1), key)}</em>;
    }
    const linkMatch = part.match(/^\[([^\]\n]+)\]\(([^)\s]+)\)$/);
    if (linkMatch) {
      const href = linkMatch[2];
      const safe = /^https?:\/\//i.test(href);
      return safe ? (
        <a key={key} href={href} target="_blank" rel="noreferrer noopener">
          {renderInline(linkMatch[1], key)}
        </a>
      ) : (
        <span key={key}>{linkMatch[1]}</span>
      );
    }
    return <React.Fragment key={key}>{part}</React.Fragment>;
  });
};

type ListItem = { text: string; indent: number; task: 'none' | 'open' | 'done' };

type Block =
  | { kind: 'code'; language: string; lines: string[] }
  | { kind: 'heading'; level: number; text: string }
  | { kind: 'list'; ordered: boolean; items: ListItem[] }
  | { kind: 'table'; align: ('left' | 'center' | 'right')[]; header: string[]; rows: string[][] }
  | { kind: 'blockquote'; content: string }
  | { kind: 'hr' }
  | { kind: 'paragraph'; lines: string[] };

const splitTableRow = (line: string): string[] => {
  const trimmed = line.trim().replace(/^\|/, '').replace(/\|$/, '');
  return trimmed.split(/(?<!\\)\|/).map((cell) => cell.trim().replace(/\\\|/g, '|'));
};

const isTableSeparator = (line: string): boolean =>
  /^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$/.test(line) && line.includes('-');

const LIST_ITEM = /^(\s*)([-*+]|\d+[.)])\s+(.*)$/;

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
    if (/^\s*(-{3,}|\*{3,}|_{3,})\s*$/.test(line)) {
      blocks.push({ kind: 'hr' });
      index += 1;
      continue;
    }
    const heading = line.match(/^(#{1,6})\s+(.*)$/);
    if (heading) {
      blocks.push({ kind: 'heading', level: heading[1].length, text: heading[2] });
      index += 1;
      continue;
    }
    if (/^\s*>/.test(line)) {
      const quoteLines: string[] = [];
      while (index < lines.length && /^\s*>/.test(lines[index])) {
        quoteLines.push(lines[index].replace(/^\s*>\s?/, ''));
        index += 1;
      }
      blocks.push({ kind: 'blockquote', content: quoteLines.join('\n') });
      continue;
    }
    if (
      line.includes('|') &&
      index + 1 < lines.length &&
      isTableSeparator(lines[index + 1]) &&
      splitTableRow(line).length > 1
    ) {
      const header = splitTableRow(line);
      const align = splitTableRow(lines[index + 1]).map((cell) => {
        const left = cell.startsWith(':');
        const right = cell.endsWith(':');
        if (left && right) return 'center' as const;
        if (right) return 'right' as const;
        return 'left' as const;
      });
      index += 2;
      const rows: string[][] = [];
      while (index < lines.length && lines[index].includes('|') && !/^\s*$/.test(lines[index])) {
        rows.push(splitTableRow(lines[index]));
        index += 1;
      }
      blocks.push({ kind: 'table', align, header, rows });
      continue;
    }
    const listMatch = line.match(LIST_ITEM);
    if (listMatch) {
      const ordered = /^\d/.test(listMatch[2]);
      const items: ListItem[] = [];
      while (index < lines.length) {
        const itemMatch = lines[index].match(LIST_ITEM);
        if (!itemMatch) break;
        let text = itemMatch[3];
        let task: ListItem['task'] = 'none';
        const taskMatch = text.match(/^\[([ xX])\]\s+(.*)$/);
        if (taskMatch) {
          task = taskMatch[1] === ' ' ? 'open' : 'done';
          text = taskMatch[2];
        }
        items.push({ text, indent: Math.floor(itemMatch[1].length / 2), task });
        index += 1;
      }
      blocks.push({ kind: 'list', ordered, items });
      continue;
    }
    const paragraphLines: string[] = [];
    while (
      index < lines.length &&
      !/^\s*$/.test(lines[index]) &&
      !/^```/.test(lines[index]) &&
      !/^(#{1,6})\s+/.test(lines[index]) &&
      !/^\s*>/.test(lines[index]) &&
      !/^\s*(-{3,}|\*{3,}|_{3,})\s*$/.test(lines[index]) &&
      !LIST_ITEM.test(lines[index]) &&
      !(
        lines[index].includes('|') &&
        index + 1 < lines.length &&
        isTableSeparator(lines[index + 1])
      )
    ) {
      paragraphLines.push(lines[index]);
      index += 1;
    }
    if (paragraphLines.length > 0) blocks.push({ kind: 'paragraph', lines: paragraphLines });
    else index += 1;
  }
  return blocks;
};

/** Render a flat, indent-annotated item list as properly nested ul/ol elements. */
const renderList = (
  items: ListItem[],
  ordered: boolean,
  keyPrefix: string
): React.ReactNode => {
  const build = (start: number, depth: number): [React.ReactNode[], number] => {
    const nodes: React.ReactNode[] = [];
    let i = start;
    while (i < items.length && items[i].indent >= depth) {
      if (items[i].indent > depth) {
        const [children, next] = build(i, items[i].indent);
        const ListTag = ordered ? 'ol' : 'ul';
        nodes.push(<ListTag key={`${keyPrefix}-sub-${i}`}>{children}</ListTag>);
        i = next;
        continue;
      }
      const item = items[i];
      const itemKey = `${keyPrefix}-${i}`;
      nodes.push(
        <li key={itemKey} style={item.task !== 'none' ? { listStyle: 'none', marginInlineStart: -18 } : undefined}>
          {item.task !== 'none' && (
            <input type="checkbox" checked={item.task === 'done'} readOnly style={{ marginInlineEnd: 6 }} />
          )}
          {renderInline(item.text, itemKey)}
        </li>
      );
      i += 1;
    }
    return [nodes, i];
  };
  const [nodes] = build(0, items.length > 0 ? items[0].indent : 0);
  const ListTag = ordered ? 'ol' : 'ul';
  return <ListTag>{nodes}</ListTag>;
};

const cellStyle: React.CSSProperties = {
  border: '1px solid var(--ant-color-border-secondary)',
  padding: '4px 10px',
  fontSize: 12.5,
};

const Markdown = ({ content }: { content: string }) => {
  const blocks = parseBlocks(content || '');
  return (
    <div className="lg-markdown" data-testid="chat-markdown">
      {blocks.map((block, blockIndex) => {
        const key = `b-${blockIndex}`;
        switch (block.kind) {
          case 'code': {
            const codeText = block.lines.join('\n');
            return (
              <div
                key={key}
                style={{
                  borderRadius: 8,
                  overflow: 'hidden',
                  margin: '8px 0',
                  border: '1px solid var(--ant-color-border-secondary)',
                }}
                data-testid="chat-md-code"
              >
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    padding: '2px 8px 2px 12px',
                    background: 'var(--ant-color-fill-secondary)',
                    fontSize: 11,
                    color: 'var(--ant-color-text-secondary)',
                  }}
                >
                  <span>{block.language || 'text'}</span>
                  <CopyButton text={codeText} />
                </div>
                <pre
                  style={{
                    background: 'var(--ant-color-fill-tertiary)',
                    padding: 12,
                    overflowX: 'auto',
                    fontSize: 12.5,
                    margin: 0,
                  }}
                >
                  <code>{codeText}</code>
                </pre>
              </div>
            );
          }
          case 'heading': {
            const HeadingTag = `h${Math.min(block.level + 2, 6)}` as keyof React.JSX.IntrinsicElements;
            return <HeadingTag key={key}>{renderInline(block.text, key)}</HeadingTag>;
          }
          case 'list':
            return <React.Fragment key={key}>{renderList(block.items, block.ordered, key)}</React.Fragment>;
          case 'table':
            return (
              <div key={key} style={{ overflowX: 'auto', margin: '8px 0' }} data-testid="chat-md-table">
                <table style={{ borderCollapse: 'collapse', width: 'auto' }}>
                  <thead>
                    <tr>
                      {block.header.map((cell, cellIndex) => (
                        <th
                          key={`${key}-h-${cellIndex}`}
                          style={{
                            ...cellStyle,
                            background: 'var(--ant-color-fill-quaternary)',
                            textAlign: block.align[cellIndex] || 'left',
                            fontWeight: 600,
                          }}
                        >
                          {renderInline(cell, `${key}-h-${cellIndex}`)}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {block.rows.map((row, rowIndex) => (
                      <tr key={`${key}-r-${rowIndex}`}>
                        {row.map((cell, cellIndex) => (
                          <td
                            key={`${key}-r-${rowIndex}-${cellIndex}`}
                            style={{ ...cellStyle, textAlign: block.align[cellIndex] || 'left' }}
                          >
                            {renderInline(cell, `${key}-r-${rowIndex}-${cellIndex}`)}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            );
          case 'blockquote':
            return (
              <blockquote
                key={key}
                style={{
                  borderInlineStart: '3px solid var(--ant-color-border)',
                  margin: '8px 0',
                  paddingInlineStart: 12,
                  color: 'var(--ant-color-text-secondary)',
                }}
                data-testid="chat-md-blockquote"
              >
                <Markdown content={block.content} />
              </blockquote>
            );
          case 'hr':
            return (
              <hr
                key={key}
                style={{ border: 'none', borderTop: '1px solid var(--ant-color-border-secondary)', margin: '12px 0' }}
              />
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
