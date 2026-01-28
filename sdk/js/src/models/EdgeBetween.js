import { v4 as uuidV4 } from 'uuid';

/**
 * EdgeBetween class representing an existence check for multiple identifiers.
 */
export default class EdgeBetween {
  /**
   * @param {Object} edgeBetween - Information about the edge connection.
   * @param {string} edgeBetween.From - The GUID for the starting node.
   * @param {string} edgeBetween.To - The GUID for the ending node.
   */
  constructor(edgeBetween = {}) {
    const { From = uuidV4(), To = uuidV4() } = edgeBetween;

    this.from = From; // Starting node GUID
    this.to = To; // Ending node GUID
  }
}
