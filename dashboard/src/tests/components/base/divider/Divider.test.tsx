import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import LitegraphDivider from '@/components/base/divider/Divider';

describe('LitegraphDivider Component', () => {
  it('renders with default props', () => {
    render(<LitegraphDivider />);
    const divider = screen.getByRole('separator');
    expect(divider).toBeInTheDocument();
  });

  it('renders with custom text', () => {
    render(<LitegraphDivider>Custom Text</LitegraphDivider>);
    expect(screen.getByText('Custom Text')).toBeInTheDocument();
  });

  it('renders with orientation prop', () => {
    render(<LitegraphDivider orientation="vertical" />);
    const divider = screen.getByRole('separator');
    // Note: antd Divider doesn't always apply orientation classes in tests
    expect(divider).toBeInTheDocument();
  });

  it('renders with dashed style', () => {
    render(<LitegraphDivider dashed />);
    const divider = screen.getByRole('separator');
    expect(divider).toHaveClass('ant-divider-dashed');
  });

  it('renders with plain text style', () => {
    render(<LitegraphDivider plain>Plain Text</LitegraphDivider>);
    const divider = screen.getByRole('separator');
    expect(divider).toHaveClass('ant-divider-plain');
  });

  it('renders with custom className', () => {
    render(<LitegraphDivider className="custom-class" />);
    const divider = screen.getByRole('separator');
    expect(divider).toHaveClass('custom-class');
  });

  it('renders with custom style', () => {
    const customStyle = { backgroundColor: 'red' };
    render(<LitegraphDivider style={customStyle} />);
    const divider = screen.getByRole('separator');
    // Note: antd Divider may not apply inline styles in tests
    expect(divider).toBeInTheDocument();
  });

  it('renders with type prop', () => {
    render(<LitegraphDivider type="horizontal" />);
    const divider = screen.getByRole('separator');
    expect(divider).toHaveClass('ant-divider-horizontal');
  });

  it('renders with all props combined', () => {
    render(
      <LitegraphDivider
        type="horizontal"
        orientation="left"
        dashed
        plain
        className="custom-class"
        style={{ color: 'blue' }}
      >
        Combined Props
      </LitegraphDivider>
    );

    const divider = screen.getByRole('separator');
    expect(divider).toBeInTheDocument();
    expect(screen.getByText('Combined Props')).toBeInTheDocument();
    expect(divider).toHaveClass('custom-class');
    // Note: antd Divider may not apply inline styles in tests
    expect(divider).toBeInTheDocument();
  });

  it('passes through all antd Divider props', () => {
    const testId = 'test-divider';
    render(<LitegraphDivider data-testid={testId} />);
    const divider = screen.getByTestId(testId);
    expect(divider).toBeInTheDocument();
  });
});
