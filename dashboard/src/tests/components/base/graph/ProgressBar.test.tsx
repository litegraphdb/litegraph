import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import ProgressBar from '@/components/base/graph/ProgressBar';
import { getPercentage, humanizeNumber } from '@/utils/dataUtils';

// Mock the utility functions
jest.mock('@/utils/dataUtils', () => ({
  getPercentage: jest.fn(),
  humanizeNumber: jest.fn(),
}));

describe('ProgressBar Component', () => {
  const mockGetPercentage = getPercentage as jest.MockedFunction<typeof getPercentage>;
  const mockHumanizeNumber = humanizeNumber as jest.MockedFunction<typeof humanizeNumber>;

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders with correct props', () => {
    mockGetPercentage.mockReturnValue(50);
    mockHumanizeNumber.mockReturnValue('1,000');

    render(<ProgressBar loaded={1000} total={2000} label="Test Progress" />);

    expect(screen.getByText('Test Progress(1,000/1,000)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(1000, 2000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(1000);
  });

  it('displays 100% progress when loaded equals total', () => {
    mockGetPercentage.mockReturnValue(100);
    mockHumanizeNumber.mockReturnValue('1,000');

    render(<ProgressBar loaded={1000} total={1000} label="Complete" />);

    expect(screen.getByText('Complete(1,000/1,000)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(1000, 1000);
  });

  it('handles large numbers correctly', () => {
    mockGetPercentage.mockReturnValue(75);
    mockHumanizeNumber.mockReturnValue('1,000,000');

    render(<ProgressBar loaded={750000} total={1000000} label="Large Numbers" />);

    expect(screen.getByText('Large Numbers(1,000,000/1,000,000)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(750000, 1000000);
  });

  it('handles edge case with very large total', () => {
    mockGetPercentage.mockReturnValue(0.01);
    mockHumanizeNumber.mockReturnValue('1,000,000,000');

    render(<ProgressBar loaded={100000000} total={1000000000000} label="Huge Total" />);

    expect(screen.getByText('Huge Total(1,000,000,000/1,000,000,000)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(100000000, 1000000000000);
  });

  it('renders progress bar with correct styling classes', () => {
    mockGetPercentage.mockReturnValue(50);
    mockHumanizeNumber.mockReturnValue('500');

    const { container } = render(<ProgressBar loaded={500} total={1000} label="Test" />);

    const progressContainer = container.querySelector('.progressContainer');
    expect(progressContainer).toBeInTheDocument();
  });

  it('handles zero values correctly', () => {
    mockGetPercentage.mockReturnValue(0);
    mockHumanizeNumber.mockReturnValue('0');

    render(<ProgressBar loaded={0} total={1000} label="Zero Loaded" />);

    expect(screen.getByText('Zero Loaded(0/0)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(0, 1000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(0);
  });

  it('handles zero total correctly', () => {
    mockGetPercentage.mockReturnValue(0);
    mockHumanizeNumber.mockReturnValue('500');

    render(<ProgressBar loaded={500} total={0} label="Zero Total" />);

    expect(screen.getByText(/Zero Total.*500.*500/)).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(500, 0);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(500);
  });

  it('handles negative values gracefully', () => {
    mockGetPercentage.mockReturnValue(-50);
    mockHumanizeNumber.mockReturnValue('-100');

    render(<ProgressBar loaded={-100} total={200} label="Negative Values" />);

    expect(screen.getByText('Negative Values(-100/-100)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(-100, 200);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(-100);
  });

  it('handles decimal values correctly', () => {
    mockGetPercentage.mockReturnValue(33.33);
    mockHumanizeNumber.mockReturnValue('333.33');

    render(<ProgressBar loaded={333.33} total={1000} label="Decimal Values" />);

    expect(screen.getByText('Decimal Values(333.33/333.33)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(333.33, 1000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(333.33);
  });

  it('handles very small numbers correctly', () => {
    mockGetPercentage.mockReturnValue(0.001);
    mockHumanizeNumber.mockReturnValue('0.001');

    render(<ProgressBar loaded={0.001} total={1000} label="Small Numbers" />);

    expect(screen.getByText('Small Numbers(0.001/0.001)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(0.001, 1000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(0.001);
  });

  it('handles empty label correctly', () => {
    mockGetPercentage.mockReturnValue(50);
    mockHumanizeNumber.mockReturnValue('500');

    render(<ProgressBar loaded={500} total={1000} label="" />);

    expect(screen.getByText('(500/500)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(500, 1000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(500);
  });

  it('handles special characters in label', () => {
    mockGetPercentage.mockReturnValue(50);
    mockHumanizeNumber.mockReturnValue('500');

    render(<ProgressBar loaded={500} total={1000} label="Special & Characters!" />);

    expect(screen.getByText('Special & Characters!(500/500)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(500, 1000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(500);
  });

  it('handles very long label text', () => {
    mockGetPercentage.mockReturnValue(50);
    mockHumanizeNumber.mockReturnValue('500');
    const longLabel = 'This is a very long label that might wrap to multiple lines in the UI';

    render(<ProgressBar loaded={500} total={1000} label={longLabel} />);

    expect(screen.getByText(`${longLabel}(500/500)`)).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(500, 1000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(500);
  });

  it('handles undefined total gracefully', () => {
    mockGetPercentage.mockReturnValue(0);
    mockHumanizeNumber.mockReturnValue('500');

    render(<ProgressBar loaded={500} total={undefined as any} label="Undefined Total" />);

    expect(screen.getByText(/Undefined Total.*500.*500/)).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(500, undefined);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(500);
  });

  it('handles null total gracefully', () => {
    mockGetPercentage.mockReturnValue(0);
    mockHumanizeNumber.mockReturnValue('500');

    render(<ProgressBar loaded={500} total={null as any} label="Null Total" />);

    expect(screen.getByText(/Null Total.*500.*500/)).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(500, null);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(500);
  });

  it('handles undefined loaded gracefully', () => {
    mockGetPercentage.mockReturnValue(0);
    mockHumanizeNumber.mockReturnValue('undefined');

    render(<ProgressBar loaded={undefined as any} total={1000} label="Undefined Loaded" />);

    expect(screen.getByText('Undefined Loaded(undefined/undefined)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(undefined, 1000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(undefined);
  });

  it('handles null loaded gracefully', () => {
    mockGetPercentage.mockReturnValue(0);
    mockHumanizeNumber.mockReturnValue('null');

    render(<ProgressBar loaded={null as any} total={1000} label="Null Loaded" />);

    expect(screen.getByText('Null Loaded(null/null)')).toBeInTheDocument();
    expect(mockGetPercentage).toHaveBeenCalledWith(null, 1000);
    expect(mockHumanizeNumber).toHaveBeenCalledWith(null);
  });
});
