import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import ApiSearchFilters from '@/page/graphs/components/ApiSearchFilters';

// Mock the child components
jest.mock('@/components/base/flex/Flex', () => {
  return function MockFlex({ children, ...props }: any) {
    return (
      <div data-testid="flex-container" {...props}>
        {children}
      </div>
    );
  };
});

jest.mock('@/components/inputs/label-input/LabelInput', () => {
  return function MockLabelInput(props: any) {
    return (
      <div data-testid="label-input" {...props}>
        Label Input
      </div>
    );
  };
});

jest.mock('@/components/inputs/tags-input/TagsInput', () => {
  return function MockTagsInput(props: any) {
    return (
      <div data-testid="tags-input" {...props}>
        Tags Input
      </div>
    );
  };
});

// Mock antd Form
jest.mock('antd', () => ({
  Form: {
    useForm: jest.fn(() => [{}]),
  },
  Tag: jest.fn(({ children, ...props }) => (
    <span data-testid="tag" {...props}>
      {children}
    </span>
  )),
}));

describe('ApiSearchFilters', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render without crashing', () => {
    render(<ApiSearchFilters />);

    expect(screen.getByTestId('flex-container')).toBeInTheDocument();
  });

  it('should render LabelInput component', () => {
    render(<ApiSearchFilters />);

    const labelInput = screen.getByTestId('label-input');
    expect(labelInput).toBeInTheDocument();
    expect(labelInput).toHaveTextContent('Label Input');
  });

  it('should render TagsInput component', () => {
    render(<ApiSearchFilters />);

    const tagsInput = screen.getByTestId('tags-input');
    expect(tagsInput).toBeInTheDocument();
    expect(tagsInput).toHaveTextContent('Tags Input');
  });

  it('should render both input components', () => {
    render(<ApiSearchFilters />);

    expect(screen.getByTestId('label-input')).toBeInTheDocument();
    expect(screen.getByTestId('tags-input')).toBeInTheDocument();
  });

  it('should render Flex container with children', () => {
    const { container } = render(<ApiSearchFilters />);

    const flexContainer = screen.getByTestId('flex-container');
    expect(flexContainer).toBeInTheDocument();

    // Check that both inputs are children of the flex container
    expect(flexContainer).toContainElement(screen.getByTestId('label-input'));
    expect(flexContainer).toContainElement(screen.getByTestId('tags-input'));
  });

  it('should pass correct props to LabelInput', () => {
    render(<ApiSearchFilters />);

    const labelInput = screen.getByTestId('label-input');
    expect(labelInput).toHaveClass('w-100');
    expect(labelInput).toHaveAttribute('name', 'labels');
  });

  it('should pass correct props to TagsInput', () => {
    render(<ApiSearchFilters />);

    const tagsInput = screen.getByTestId('tags-input');
    expect(tagsInput).toHaveAttribute('name', 'tags');
  });

  it('should render consistently', () => {
    const { rerender } = render(<ApiSearchFilters />);

    expect(screen.getByTestId('flex-container')).toBeInTheDocument();
    expect(screen.getByTestId('label-input')).toBeInTheDocument();
    expect(screen.getByTestId('tags-input')).toBeInTheDocument();

    // Re-render to ensure consistency
    rerender(<ApiSearchFilters />);

    expect(screen.getByTestId('flex-container')).toBeInTheDocument();
    expect(screen.getByTestId('label-input')).toBeInTheDocument();
    expect(screen.getByTestId('tags-input')).toBeInTheDocument();
  });

  it('should have proper component structure', () => {
    const { container } = render(<ApiSearchFilters />);

    // Should have the flex container as the main wrapper
    expect(container.firstChild).toHaveAttribute('data-testid', 'flex-container');

    // Should contain both input components
    expect(container).toHaveTextContent('Label Input');
    expect(container).toHaveTextContent('Tags Input');
  });
});
