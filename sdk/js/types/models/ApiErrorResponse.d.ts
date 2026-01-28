/**
 * Class representing an API error response.
 */
export default class ApiErrorResponse {
    /**
     * Create an API error response.
     *
     * @param {ApiErrorEnum} error - The error code.
     * @param {Object} [context=null] - Additional contextual information.
     * @param {string} [description=null] - Description of the error.
     */
    constructor(error?: ApiErrorEnum, context?: any, description?: string);
    error: string;
    context: any;
    description: string;
    /**
     * Get the human-readable message corresponding to the error.
     *
     * @returns {string} - The message for the current error.
     */
    get message(): string;
    /**
     * Get the HTTP status code corresponding to the error.
     *
     * @returns {number} - The HTTP status code for the current error.
     */
    get statusCode(): number;
}
import { ApiErrorEnum } from '../enums/ApiErrorEnum';
