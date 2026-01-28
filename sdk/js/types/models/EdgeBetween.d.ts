/**
 * EdgeBetween class representing an existence check for multiple identifiers.
 */
export default class EdgeBetween {
    /**
     * @param {Object} edgeBetween - Information about the edge connection.
     * @param {string} edgeBetween.From - The GUID for the starting node.
     * @param {string} edgeBetween.To - The GUID for the ending node.
     */
    constructor(edgeBetween?: {
        From: string;
        To: string;
    });
    from: any;
    to: any;
}
