export default class Serializer {
    /**
     * Deserialize JSON to an instance of the specified type.
     * @template T
     * @param {jsonString} jsonString
     * @param {Class} typeConstructor
     * @return {T}
     */
    static deserializeJson<T>(json: any, TypeConstructor: any): T;
    /**
     * Deserialize a paginated enumeration envelope, instantiating each entry of Objects with the item constructor.
     * @param {string} json - JSON string of the enumeration envelope.
     * @param {Function|null} ItemConstructor - Optional constructor used to instantiate each entry of Objects.
     * @return {EnumerationResult} - The deserialized enumeration result.
     */
    static deserializeEnumeration(json: string, ItemConstructor: Function | null): EnumerationResult;
    /**
     * Serialize an object to JSON.
     * @param {object} obj - Object to serialize.
     * @param {boolean} pretty - Whether to pretty print the JSON.
     * @returns {string} - Serialized JSON string.
     */
    static serializeJson(obj: object, pretty?: boolean): string;
    static jsonReplacer(key: any, value: any): any;
    static jsonReviver(key: any, value: any): any;
}
import EnumerationResult from '../models/EnumerationResult';
