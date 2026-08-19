type TransactionOperationFailure = {
  Index?: number;
  OperationType?: string;
  ObjectType?: string;
  GUID?: string;
  Success?: boolean;
  Error?: string;
};

type TransactionFailure = {
  Success?: boolean;
  TransactionId?: string;
  State?: string;
  RolledBack?: boolean;
  ValidationFailure?: boolean;
  FailedOperationIndex?: number;
  Error?: string;
  Provider?: string;
  Retryable?: boolean;
  ConcurrencyConflict?: boolean;
  ProviderErrorCode?: string;
  Operations?: TransactionOperationFailure[];
};

type Translator = (key: string, values?: Record<string, string | number>) => string;

const asRecord = (value: unknown): Record<string, unknown> | null => {
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    return value as Record<string, unknown>;
  }
  return null;
};

export const getTransactionFailureSummary = (json: unknown, t: Translator): string | null => {
  const record = asRecord(json) as TransactionFailure | null;
  if (!record || record.Success !== false) return null;

  const failedIndex = record.FailedOperationIndex;
  const failedOperation = Array.isArray(record.Operations)
    ? record.Operations.find((operation) => operation.Index === failedIndex || operation.Success === false)
    : undefined;
  const operationType = failedOperation?.OperationType || t('summary.defaultOperation');
  const objectType = failedOperation?.ObjectType || t('summary.defaultObject');
  const guid = failedOperation?.GUID ? t('summary.guid', { guid: failedOperation.GUID }) : '';
  const error = failedOperation?.Error || record.Error || t('summary.noError');
  const validation = record.ValidationFailure ? t('summary.validation') : '';
  const rolledBack = record.RolledBack ? t('summary.rolledBack') : '';
  const indexText = typeof failedIndex === 'number' ? t('summary.indexText', { index: failedIndex }) : '';
  const transactionId = record.TransactionId ? t('summary.transactionId', { transactionId: record.TransactionId }) : '';
  const state = record.State ? t('summary.state', { state: record.State }) : '';
  const provider = record.Provider ? t('summary.provider', { provider: record.Provider }) : '';
  const providerError = record.ProviderErrorCode
    ? t('summary.providerError', { providerErrorCode: record.ProviderErrorCode })
    : '';
  const retryable = record.Retryable ? t('summary.retryable') : '';
  const conflict = record.ConcurrencyConflict ? t('summary.conflict') : '';

  const main = t('summary.transactionMain', { operationType, objectType, guid, indexText, error });
  return `${main}${validation}${rolledBack}${transactionId}${state}${provider}${providerError}${retryable}${conflict}`;
};

export const getQueryErrorSummary = (json: unknown, t: Translator): string | null => {
  const record = asRecord(json);
  if (!record) return null;

  const description =
    typeof record.Description === 'string'
      ? record.Description
      : typeof record.Message === 'string'
        ? record.Message
        : '';

  if (!description || !/query|MATCH|CREATE|CALL|line \d+, column \d+/i.test(description)) return null;

  const location = description.match(/line \d+, column \d+/i)?.[0];
  return location
    ? t('summary.queryErrorAt', { location, description })
    : t('summary.queryError', { description });
};
