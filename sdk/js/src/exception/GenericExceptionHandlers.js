export default class GenericExceptionHandlers {
  /**
   * @param {string} argName
   */
  static ArgumentNullException = (argName) => {
    throw Error(`ArgumentNullException: ${argName} is null or empty`);
  };
  static GenericException = (message) => {
    throw Error(message);
  };
}
