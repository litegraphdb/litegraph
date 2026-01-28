export default class Logger {
    /**
     * @param {string} type
     * @param {string} message
     */
    static log: (severity: any, message: string) => void;
}
export const LoggerInstance: Logger;
