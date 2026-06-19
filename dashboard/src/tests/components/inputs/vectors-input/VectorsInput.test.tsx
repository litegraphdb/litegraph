import '@testing-library/jest-dom';
import React from 'react';
import { Form } from 'antd';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import VectorsInput from '@/components/inputs/vectors-input.tsx/VectorsInput';

jest.mock('uuid', () => ({
  v4: () => 'test-vector-key',
}));

jest.mock('jsoneditor-react', () => ({
  JsonEditor: ({ value, onChange }: any) => (
    <input
      data-testid="vector-json-editor"
      value={JSON.stringify(value)}
      onChange={(event) => onChange(JSON.parse(event.target.value))}
    />
  ),
}));

const Harness = ({
  initialVectors = [],
  onSubmit,
  onValidate,
}: {
  initialVectors?: any[];
  onSubmit: (vectors: any[]) => void;
  onValidate?: (isValid: boolean) => void;
}) => {
  const [form] = Form.useForm();

  return (
    <Form form={form} initialValues={{ vectors: initialVectors }}>
      <VectorsInput name="vectors" />
      <button type="button" onClick={() => onSubmit(form.getFieldValue('vectors'))}>
        Capture
      </button>
      <button
        type="button"
        onClick={() => {
          form
            .validateFields()
            .then(() => onValidate?.(true))
            .catch(() => onValidate?.(false));
        }}
      >
        Validate
      </button>
    </Form>
  );
};

describe('VectorsInput', () => {
  it('binds the vector values editor to the nested vectors form field', async () => {
    const onSubmit = jest.fn();
    render(
      <Harness
        initialVectors={[
          {
            Model: 'model-a',
            Dimensionality: 3,
            Content: 'content-a',
            Vectors: [0.1, 0.2, 0.3],
          },
        ]}
        onSubmit={onSubmit}
      />
    );

    const vectorEditor = screen.getByTestId('vector-json-editor');
    expect(vectorEditor).toHaveValue('[0.1,0.2,0.3]');

    await act(async () => {
      fireEvent.change(vectorEditor, { target: { value: '[0.4,0.5,0.6]' } });
    });
    fireEvent.click(screen.getByRole('button', { name: 'Capture' }));

    expect(onSubmit).toHaveBeenCalledWith([
      {
        Model: 'model-a',
        Dimensionality: 3,
        Content: 'content-a',
        Vectors: [0.4, 0.5, 0.6],
      },
    ]);
  });

  it('adds a new vector row with editable vector values', async () => {
    const onSubmit = jest.fn();
    render(<Harness onSubmit={onSubmit} />);

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /add vector/i }));
    });

    const vectorEditor = screen.getByTestId('vector-json-editor');
    expect(vectorEditor).toHaveValue('[0.1,0.2,0.3]');

    await act(async () => {
      fireEvent.change(vectorEditor, { target: { value: '[1,2,3]' } });
    });
    fireEvent.click(screen.getByRole('button', { name: 'Capture' }));

    expect(onSubmit).toHaveBeenCalledWith([
      {
        Model: '',
        Dimensionality: 3,
        Content: '',
        Vectors: [1, 2, 3],
      },
    ]);
  });

  it('rejects vector values that do not match dimensionality', async () => {
    const onValidate = jest.fn();
    render(
      <Harness
        initialVectors={[
          {
            Model: 'model-a',
            Dimensionality: 3,
            Content: 'content-a',
            Vectors: [1, 2, 3, 4],
          },
        ]}
        onSubmit={jest.fn()}
        onValidate={onValidate}
      />
    );

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Validate' }));
    });

    await waitFor(() => expect(onValidate).toHaveBeenCalledWith(false));
  });
});
