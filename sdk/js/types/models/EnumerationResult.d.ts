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
    constructor(result?: {
        Success?: boolean;
        Timestamp?: any | null;
        MaxResults?: number;
        ContinuationToken?: string | null;
        EndOfResults?: boolean;
        TotalRecords?: number;
        RecordsRemaining?: number;
        Objects?: Array<any>;
    }, ItemConstructor?: Function | null);
    Success: boolean;
    Timestamp: any;
    MaxResults: number;
    ContinuationToken: string;
    EndOfResults: boolean;
    TotalRecords: number;
    RecordsRemaining: number;
    Objects: any[];
}
