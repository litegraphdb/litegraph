import { formatDate } from '@/utils/date';

// Mock toLocaleString to return predictable results
const mockToLocaleString = jest.fn();
const originalToLocaleString = Date.prototype.toLocaleString;

describe('Date Utils', () => {
  beforeEach(() => {
    // Mock Date.prototype.toLocaleString
    Date.prototype.toLocaleString = mockToLocaleString;
  });

  afterEach(() => {
    // Restore original method
    Date.prototype.toLocaleString = originalToLocaleString;
    jest.clearAllMocks();
  });

  describe('formatDate', () => {
    it('should format date string correctly', () => {
      mockToLocaleString.mockReturnValue('Jan 15, 2024, 02:30:45 PM EST');

      const result = formatDate('2024-01-15T14:30:45.000Z');

      expect(mockToLocaleString).toHaveBeenCalledWith('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        timeZoneName: 'short',
      });
      expect(result).toBe('Jan 15, 2024, 02:30:45 PM EST');
    });

    it('should format Date object correctly', () => {
      mockToLocaleString.mockReturnValue('Dec 25, 2023, 12:00:00 PM PST');

      const date = new Date('2023-12-25T20:00:00.000Z');
      const result = formatDate(date);

      expect(mockToLocaleString).toHaveBeenCalledWith('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        timeZoneName: 'short',
      });
      expect(result).toBe('Dec 25, 2023, 12:00:00 PM PST');
    });

    it('should return empty string for falsy date string', () => {
      const result = formatDate('');
      expect(result).toBe('');
    });

    it('should return empty string for null date', () => {
      const result = formatDate(null as any);
      expect(result).toBe('');
    });

    it('should return empty string for undefined date', () => {
      const result = formatDate(undefined as any);
      expect(result).toBe('');
    });

    it('should return empty string for zero date', () => {
      const result = formatDate(0 as any);
      expect(result).toBe('');
    });

    it('should handle ISO date string format', () => {
      mockToLocaleString.mockReturnValue('Mar 10, 2024, 09:15:30 AM UTC');

      const result = formatDate('2024-03-10T09:15:30.000Z');

      expect(result).toBe('Mar 10, 2024, 09:15:30 AM UTC');
    });

    it('should handle date string without time', () => {
      mockToLocaleString.mockReturnValue('Apr 20, 2024, 12:00:00 AM UTC');

      const result = formatDate('2024-04-20');

      expect(result).toBe('Apr 20, 2024, 12:00:00 AM UTC');
    });

    it('should handle date string with timezone offset', () => {
      mockToLocaleString.mockReturnValue('May 5, 2024, 03:45:20 PM EDT');

      const result = formatDate('2024-05-05T15:45:20-04:00');

      expect(result).toBe('May 5, 2024, 03:45:20 PM EDT');
    });

    it('should handle invalid date string gracefully', () => {
      mockToLocaleString.mockReturnValue('Invalid Date');

      const result = formatDate('invalid-date-string');

      expect(result).toBe('Invalid Date');
    });

    it('should use correct locale and options', () => {
      mockToLocaleString.mockReturnValue('Jun 15, 2024, 06:30:15 AM CDT');

      formatDate('2024-06-15T11:30:15.000Z');

      expect(mockToLocaleString).toHaveBeenCalledWith('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        timeZoneName: 'short',
      });
    });

    it('should handle edge case dates', () => {
      mockToLocaleString.mockReturnValue('Jan 1, 1970, 12:00:00 AM UTC');

      const result = formatDate(new Date(0)); // Unix epoch

      expect(result).toBe('Jan 1, 1970, 12:00:00 AM UTC');
    });

    it('should handle future dates', () => {
      mockToLocaleString.mockReturnValue('Dec 31, 2099, 11:59:59 PM UTC');

      const result = formatDate('2099-12-31T23:59:59.000Z');

      expect(result).toBe('Dec 31, 2099, 11:59:59 PM UTC');
    });
  });
});
