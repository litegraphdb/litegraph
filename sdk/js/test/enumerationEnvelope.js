/**
 * Wraps an array of mock objects in the EnumerationResult envelope returned by
 * every list-shaped route on the LiteGraph server (zero get-all contract).
 * @param {Array<Object>} objects - Objects for the current page.
 * @param {Object} [overrides] - Optional envelope property overrides.
 * @returns {Object} - EnumerationResult envelope.
 */
export const toEnumerationEnvelope = (objects = [], overrides = {}) => ({
  Success: true,
  Timestamp: {
    Start: '2024-10-19T14:35:20.351Z',
    End: '2024-10-19T14:35:20.451Z',
    TotalMs: 100,
    Messages: {},
  },
  MaxResults: 1000,
  ContinuationToken: null,
  EndOfResults: true,
  TotalRecords: objects.length,
  RecordsRemaining: 0,
  Objects: objects,
  ...overrides,
});
