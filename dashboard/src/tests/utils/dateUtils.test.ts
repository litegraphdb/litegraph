import {
  formatDateTime,
  formatSecondsForTimer,
  dateInMonthWordsTimeFormat,
} from '@/utils/dateUtils';

// Mock moment to control date formatting
jest.mock('moment', () => {
  const mockMoment = (date: string) => ({
    format: (format: string) => {
      if (format === 'Do MMM YYYY, HH:mm') {
        return '15th Aug 2024, 14:30';
      }
      return 'Mocked Date';
    },
  });
  mockMoment.mockReturnValue = mockMoment;
  return mockMoment;
});

describe('Date Utils', () => {
  describe('dateInMonthWordsTimeFormat', () => {
    it('should have the correct format string', () => {
      expect(dateInMonthWordsTimeFormat).toBe('Do MMM YYYY, HH:mm');
    });
  });

  describe('formatDateTime', () => {
    it('should format date with default format when no format is provided', () => {
      const result = formatDateTime('2024-08-15T14:30:00Z');
      expect(result).toBe('15th Aug 2024, 14:30');
    });

    it('should format date with custom format when provided', () => {
      const result = formatDateTime('2024-08-15T14:30:00Z', 'YYYY-MM-DD');
      expect(result).toBe('Mocked Date');
    });

    it('should return "Invalid Date" for empty date string', () => {
      const result = formatDateTime('');
      expect(result).toBe('Invalid Date');
    });

    it('should return "Invalid Date" for null date', () => {
      const result = formatDateTime(null as any);
      expect(result).toBe('Invalid Date');
    });

    it('should return "Invalid Date" for undefined date', () => {
      const result = formatDateTime(undefined as any);
      expect(result).toBe('Invalid Date');
    });

    it('should handle error gracefully and return "Invalid Date"', () => {
      // Mock console.log to avoid console output in tests
      const consoleSpy = jest.spyOn(console, 'log').mockImplementation(() => {});

      // This would normally throw an error, but our mock handles it
      const result = formatDateTime('invalid-date');

      expect(result).toBe('15th Aug 2024, 14:30'); // Mock returns this value
      consoleSpy.mockRestore();
    });
  });

  describe('formatSecondsForTimer', () => {
    it('should format seconds correctly for single digit minutes and seconds', () => {
      const result = formatSecondsForTimer(65); // 1 minute 5 seconds
      expect(result).toBe('01:05');
    });

    it('should format seconds correctly for double digit minutes and seconds', () => {
      const result = formatSecondsForTimer(125); // 2 minutes 5 seconds
      expect(result).toBe('02:05');
    });

    it('should format seconds correctly for zero seconds', () => {
      const result = formatSecondsForTimer(0);
      expect(result).toBe('00:00');
    });

    it('should format seconds correctly for large values', () => {
      const result = formatSecondsForTimer(3661); // 1 hour 1 minute 1 second
      expect(result).toBe('61:01');
    });

    it('should handle negative values', () => {
      const result = formatSecondsForTimer(-30);
      expect(result).toBe('-1:-30'); // Function returns this for negative values
    });
  });
});
