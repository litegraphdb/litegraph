export default class GenericExceptionHandlers {
    /**
     * @param {string} argName
     */
    static ArgumentNullException: (argName: string) => never;
    static GenericException: (message: any) => never;
}
