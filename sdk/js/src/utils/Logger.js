import { SeverityEnum } from '../enums/SeverityEnum';

export default class Logger {
  /**
   * @param {string} type
   * @param {string} message
   */
  static log = (severity, message) => {
    switch (severity) {
      case SeverityEnum.Warn:
        //eslint-disable-next-line no-console
        console.warn(message);
        break;
      case SeverityEnum.Debug:
        //eslint-disable-next-line no-console
        console.debug(message);
        break;
      default:
        //eslint-disable-next-line no-console
        console.log(message);
    }
  };
}

export const LoggerInstance = new Logger();
