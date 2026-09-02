/**
 * EnumerationResult class representing a paginated enumeration envelope returned by list endpoints.
 */
export default class EnumerationResult {
  /**
   * @param {Object} result - Information about the enumeration result.
   * @param {boolean} [result.Success=true] - Indicates if the enumeration was successful.
   * @param {Object|null} [result.Timestamp=null] - Start and end timestamps for the enumeration.
   * @param {number} [result.MaxResults=1000] - Maximum number of results retrieved.
   * @param {string|null} [result.ContinuationToken=null] - Continuation token for the next page, or null when exhausted.
   * @param {boolean} [result.EndOfResults=true] - Indicates the end of the result set.
   * @param {number} [result.TotalRecords=0] - Total number of records.
   * @param {number} [result.RecordsRemaining=0] - Number of records remaining in the enumeration.
   * @param {Array<Object>} [result.Objects=[]] - Objects in the current page.
   * @param {Function|null} [ItemConstructor=null] - Optional constructor used to instantiate each entry of Objects.
   */
  constructor(result = {}, ItemConstructor = null) {
    const {
      Success = true,
      Timestamp = null,
      MaxResults = 1000,
      ContinuationToken = null,
      EndOfResults = true,
      TotalRecords = 0,
      RecordsRemaining = 0,
      Objects = [],
    } = result;

    this.Success = Success; // Indicates if the enumeration was successful
    this.Timestamp = Timestamp; // Start and end timestamps
    this.MaxResults = MaxResults; // Maximum number of results retrieved
    this.ContinuationToken = ContinuationToken; // Continuation token for the next page
    this.EndOfResults = EndOfResults; // Indicates the end of the result set
    this.TotalRecords = TotalRecords; // Total number of records
    this.RecordsRemaining = RecordsRemaining; // Number of records remaining
    const items = Array.isArray(Objects) ? Objects : [];
    this.Objects = ItemConstructor ? items.map((item) => new ItemConstructor(item)) : items; // Objects in the current page
  }
}
