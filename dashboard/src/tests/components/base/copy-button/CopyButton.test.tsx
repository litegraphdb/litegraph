import '@testing-library/jest-dom';
import React from 'react';
import fs from 'fs';
import path from 'path';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import toast from 'react-hot-toast';
import CopyButton from '@/components/base/copy-button/CopyButton';

jest.mock('react-hot-toast', () => ({
  __esModule: true,
  default: {
    success: jest.fn(),
    error: jest.fn(),
  },
}));

describe('CopyButton', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    Object.defineProperty(window, 'isSecureContext', {
      value: true,
      configurable: true,
    });
    Object.defineProperty(navigator, 'clipboard', {
      value: {
        writeText: jest.fn().mockResolvedValue(undefined),
      },
      configurable: true,
    });
  });

  it('copies without a toast and briefly shows a green check icon', async () => {
    const { container } = render(<CopyButton text="copy-me" tooltipTitle="Copy value" />);

    fireEvent.click(screen.getByRole('button', { name: 'Copy value' }));

    await waitFor(() => {
      expect(navigator.clipboard.writeText).toHaveBeenCalledWith('copy-me');
    });
    expect(toast.success).not.toHaveBeenCalled();

    const checkIcon = await screen.findByTestId('copy-check-icon');
    expect(checkIcon).toBeInTheDocument();
    expect(container.querySelector('.anticon-check')).toBeInTheDocument();

    act(() => {
      jest.advanceTimersByTime(2000);
    });

    await waitFor(() => {
      expect(screen.queryByTestId('copy-check-icon')).not.toBeInTheDocument();
    });
  });

  it('is the only dashboard component/page copy control implementation', () => {
    const sourceRoots = [
      path.join(process.cwd(), 'src', 'components'),
      path.join(process.cwd(), 'src', 'page'),
    ];
    const allowedImplementation = 'src/components/base/copy-button/CopyButton.tsx';
    const disallowedPatterns = [
      /CopyOutlined/,
      /copyJsonToClipboard/,
      /copyTextToClipboard/,
      /navigator\.clipboard/,
      /document\.execCommand\(['"]copy['"]\)/,
      /\.writeText\(/,
    ];

    const readSourceFiles = (directory: string): string[] => {
      return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
        const fullPath = path.join(directory, entry.name);

        if (entry.isDirectory()) {
          return readSourceFiles(fullPath);
        }

        return /\.(ts|tsx)$/.test(entry.name) ? [fullPath] : [];
      });
    };

    const violations = sourceRoots
      .flatMap(readSourceFiles)
      .filter((filePath) => filePath.replace(/\\/g, '/').endsWith(allowedImplementation) === false)
      .filter((filePath) => {
        const source = fs.readFileSync(filePath, 'utf8');
        return disallowedPatterns.some((pattern) => pattern.test(source));
      })
      .map((filePath) => path.relative(process.cwd(), filePath));

    expect(violations).toEqual([]);
  });
});
