import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import Markdown from '@/page/ai/chat/components/Markdown';

describe('Markdown', () => {
  it('renders a GFM table with header and body cells', () => {
    render(<Markdown content={'| Name | Value |\n|---|---|\n| Alpha | one |'} />);

    const table = screen.getByTestId('chat-md-table');
    expect(table).toBeInTheDocument();
    expect(table.querySelectorAll('th')).toHaveLength(2);
    expect(table.querySelector('tbody')).toHaveTextContent('Alpha');
  });

  it('renders blockquote, strikethrough, and horizontal rule', () => {
    const { container } = render(<Markdown content={'> quoted text\n\n~~gone~~ kept\n\n---'} />);

    expect(screen.getByTestId('chat-md-blockquote')).toHaveTextContent('quoted text');
    expect(container.querySelector('del')).toHaveTextContent('gone');
    expect(container.querySelector('hr')).toBeInTheDocument();
  });

  it('renders fenced code with a language label and the code text', () => {
    render(<Markdown content={'```python\nprint(1)\n```'} />);

    const code = screen.getByTestId('chat-md-code');
    expect(code).toHaveTextContent('python');
    expect(code.querySelector('pre code')).toHaveTextContent('print(1)');
  });

  it('renders nested lists and task-list checkboxes', () => {
    const { container } = render(
      <Markdown content={'- parent\n  - child\n\n- [x] finished task'} />
    );

    expect(container.querySelector('ul ul li')).toHaveTextContent('child');
    const checkbox = container.querySelector('input[type="checkbox"]') as HTMLInputElement;
    expect(checkbox).toBeChecked();
  });

  it('does not crash on an unterminated fence mid-stream', () => {
    render(<Markdown content={'```js\nconst a = 1;'} />);

    expect(screen.getByTestId('chat-md-code')).toHaveTextContent('const a = 1;');
  });
});
