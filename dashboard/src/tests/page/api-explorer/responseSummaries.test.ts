import { getQueryErrorSummary, getTransactionFailureSummary } from '@/page/api-explorer/responseSummaries';

describe('API Explorer response summaries', () => {
  it('summarizes failed graph transaction responses', () => {
    const summary = getTransactionFailureSummary({
      Success: false,
      TransactionId: 'tx-1',
      RolledBack: true,
      FailedOperationIndex: 1,
      Error: 'duplicate node',
      Provider: 'Postgresql',
      ProviderErrorCode: '40001',
      Retryable: true,
      ConcurrencyConflict: true,
      Operations: [
        { Index: 0, OperationType: 'Create', ObjectType: 'Node', Success: true },
        {
          Index: 1,
          OperationType: 'Create',
          ObjectType: 'Node',
          GUID: 'node-1',
          Success: false,
          Error: 'duplicate node',
        },
      ],
    });

    expect(summary).toContain('Create Node');
    expect(summary).toContain('node-1');
    expect(summary).toContain('index 1');
    expect(summary).toContain('duplicate node');
    expect(summary).toContain('rolled back');
    expect(summary).toContain('tx-1');
    expect(summary).toContain('Postgresql');
    expect(summary).toContain('40001');
    expect(summary).toContain('retryable');
    expect(summary).toContain('Concurrency conflict');
  });

  it('ignores successful responses', () => {
    expect(getTransactionFailureSummary({ Success: true })).toBeNull();
  });

  it('summarizes transaction validation failures', () => {
    const summary = getTransactionFailureSummary({
      Success: false,
      RolledBack: false,
      ValidationFailure: true,
      FailedOperationIndex: 0,
      Error: 'Transaction operation 0 requires a payload.',
      Operations: [{ Index: 0, OperationType: 'Create', ObjectType: 'Node', Success: false }],
    });

    expect(summary).toContain('failed validation before starting');
    expect(summary).not.toContain('rolled back');
  });

  it('summarizes graph query line and column errors', () => {
    const summary = getQueryErrorSummary({
      Error: 'BadRequest',
      Description: "WHERE operator expected at line 2, column 14.",
    });

    expect(summary).toContain('line 2, column 14');
    expect(summary).toContain('WHERE operator expected');
  });

  it('ignores non-query errors', () => {
    expect(getQueryErrorSummary({ Error: 'NotFound', Description: 'Graph not found.' })).toBeNull();
  });
});
