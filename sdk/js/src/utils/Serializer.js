import util from 'util';
export default class Serializer {
  /**
   * Deserialize JSON to an instance of the specified type.
   * @template T
   * @param {jsonString} jsonString
   * @param {Class} typeConstructor
   * @return {T}
   */
  static deserializeJson(json, TypeConstructor) {
    if (typeof json === 'string') {
      try {
        const data = JSON.parse(json, this.jsonReviver);

        if (Array.isArray(data)) {
          return data.map((item) => new TypeConstructor(item));
        } else {
          return new TypeConstructor(data);
        }
      } catch (err) {
        return json;
        //in case fails return data as it as
      }
    } else if (ArrayBuffer.isView(json)) {
      try {
        return this.deserializeJson(new util.TextDecoder().decode(json));
      } catch (err) {
        console.log(err, 'chk err');
        throw new Error('Invalid JSON input');
      }
    }
  }

  /**
   * Serialize an object to JSON.
   * @param {object} obj - Object to serialize.
   * @param {boolean} pretty - Whether to pretty print the JSON.
   * @returns {string} - Serialized JSON string.
   */
  static serializeJson(obj, pretty = true) {
    if (obj === null) return null;

    return JSON.stringify(obj, this.jsonReplacer, pretty ? 2 : 0);
  }

  // JSON Replacer to handle specific types
  static jsonReplacer(key, value) {
    if (value instanceof Date) {
      return value.toISOString();
    }
    return value;
  }

  // JSON Reviver to convert string dates back to Date objects
  static jsonReviver(key, value) {
    if (typeof value === 'string') {
      const dateMatch = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z$/.test(value);
      if (dateMatch) {
        return new Date(value);
      }
    }
    return value;
  }
}
