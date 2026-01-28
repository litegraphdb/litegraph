import moment from 'moment';

export const dateInMonthWordsTimeFormat = 'Do MMM YYYY, HH:mm';

export const formatDateTime = (dateTime: string, format?: string) => {
  try {
    if (dateTime) {
      return moment(dateTime).format(format || dateInMonthWordsTimeFormat);
    }
    return 'Invalid Date';
  } catch (error) {
    //eslint-disable-next-line no-console
    console.log('Error', error);
    return 'Invalid Date';
  }
};

// Utility function to format seconds as MM:SS
export const formatSecondsForTimer = (seconds: number): string => {
  const minutes = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
};
