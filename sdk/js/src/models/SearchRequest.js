import { v4 as uuidV4 } from 'uuid';
import { EnumerationOrderEnum } from '../enums/EnumerationOrderEnum';

/**
 * SearchRequest class representing a search request in the graph.
 */
export default class SearchRequest {
  /**
   * @param {Object} request - Information about the search request.
   * @param {string} request.GraphGUID - Globally unique identifier for the graph (defaults to an empty GUID).
   * @param {string} request.Ordering - Ordering of the search results (default is CreatedDescending).
   * @param {Object} request.Expr - Expression used for the search (default is null).
   */
  constructor(request = {}) {
    const {
      GraphGUID = uuidV4(), // Assuming default(Guid) means an empty or new GUID in C#
      Ordering = EnumerationOrderEnum.CreatedAscending, // Assuming EnumerationOrderEnum.CreatedDescending is a string
      Expr = null,
    } = request;

    this.GraphGUID = GraphGUID; // Unique identifier for the graph
    this.ordering = Ordering; // Ordering of the search results
    this.expr = Expr; // Expression for the search request
  }
}
