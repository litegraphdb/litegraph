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
     * Serialize an object to JSON.
     * @param {object} obj - Object to serialize.
     * @param {boolean} pretty - Whether to pretty print the JSON.
     * @returns {string} - Serialized JSON string.
     */
    static serializeJson(obj: object, pretty?: boolean): string;
    static jsonReplacer(key: any, value: any): any;
    static jsonReviver(key: any, value: any): any;
}
