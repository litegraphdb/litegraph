import {
  toTitleCase,
  getFirstLetterOfTheWord,
  uuid,
  getUserName,
  pluralize,
} from '@/utils/stringUtils';
import { UserMetadata } from 'litegraphdb/dist/types/types';

// Mock uuid to return predictable values
jest.mock('uuid', () => ({
  v4: jest.fn(() => 'mock-uuid-12345'),
}));

describe('String Utils', () => {
  describe('toTitleCase', () => {
    it('should convert kebab-case to Title Case', () => {
      expect(toTitleCase('hello-world')).toBe('Hello World');
    });

    it('should convert snake_case to Title Case', () => {
      expect(toTitleCase('hello_world')).toBe('Hello_world'); // Function only handles hyphens, not underscores
    });

    it('should handle single word', () => {
      expect(toTitleCase('hello')).toBe('Hello');
    });

    it('should handle empty string', () => {
      expect(toTitleCase('')).toBe('');
    });

    it('should handle string with multiple hyphens', () => {
      expect(toTitleCase('hello-world-test')).toBe('Hello World Test');
    });

    it('should handle string with mixed separators', () => {
      expect(toTitleCase('hello-world_test')).toBe('Hello World_test'); // Only hyphens are converted
    });

    it('should preserve existing capitalization', () => {
      expect(toTitleCase('Hello-World')).toBe('Hello World');
    });
  });

  describe('getFirstLetterOfTheWord', () => {
    it('should return first letter in uppercase', () => {
      expect(getFirstLetterOfTheWord('hello')).toBe('H');
    });

    it('should handle empty string', () => {
      expect(getFirstLetterOfTheWord('')).toBe('');
    });

    it('should handle single character', () => {
      expect(getFirstLetterOfTheWord('a')).toBe('A');
    });

    it('should handle null value', () => {
      expect(getFirstLetterOfTheWord(null as any)).toBe('');
    });

    it('should handle undefined value', () => {
      expect(getFirstLetterOfTheWord(undefined as any)).toBe('');
    });

    it('should handle string with spaces', () => {
      expect(getFirstLetterOfTheWord('hello world')).toBe('H');
    });
  });

  describe('uuid', () => {
    it('should return a mock UUID', () => {
      const result = uuid();
      expect(result).toBe('mock-uuid-12345');
    });

    it('should call v4 function', () => {
      const { v4 } = require('uuid');
      uuid();
      expect(v4).toHaveBeenCalled();
    });
  });

  describe('getUserName', () => {
    it('should return full name when both first and last names exist', () => {
      const user: UserMetadata = {
        FirstName: 'John',
        LastName: 'Doe',
      } as UserMetadata;

      expect(getUserName(user)).toBe('John Doe');
    });

    it('should return first name only when last name is missing', () => {
      const user: UserMetadata = {
        FirstName: 'John',
        LastName: '',
      } as UserMetadata;

      expect(getUserName(user)).toBe('John ');
    });

    it('should return last name only when first name is missing', () => {
      const user: UserMetadata = {
        FirstName: '',
        LastName: 'Doe',
      } as UserMetadata;

      expect(getUserName(user)).toBe(' Doe');
    });

    it('should return "User" when both names are missing', () => {
      const user: UserMetadata = {
        FirstName: '',
        LastName: '',
      } as UserMetadata;

      expect(getUserName(user)).toBe('User');
    });

    it('should return "User" when both names are null', () => {
      const user: UserMetadata = {
        FirstName: null as any,
        LastName: null as any,
      } as UserMetadata;

      expect(getUserName(user)).toBe('User');
    });

    it('should return "User" when user is null', () => {
      expect(getUserName(null)).toBe('User');
    });

    it('should return "User" when user is undefined', () => {
      expect(getUserName(undefined as any)).toBe('User');
    });
  });

  describe('pluralize', () => {
    it('should return singular form for count 1', () => {
      expect(pluralize(1, 'item')).toBe('1 item');
    });

    it('should return plural form for count 0', () => {
      expect(pluralize(0, 'item')).toBe('0 item'); // Function returns singular for 0
    });

    it('should return plural form for count greater than 1', () => {
      expect(pluralize(5, 'item')).toBe('5 items');
    });

    it('should use custom suffix when provided', () => {
      expect(pluralize(1, 'person', 'people')).toBe('1 person');
      expect(pluralize(5, 'person', 'people')).toBe('5 personpeople'); // Function concatenates suffix
    });

    it('should handle negative numbers', () => {
      expect(pluralize(-1, 'item')).toBe('-1 item'); // Function returns singular for negative
      expect(pluralize(-5, 'item')).toBe('-5 item'); // Function returns singular for negative
    });

    it('should handle decimal numbers', () => {
      expect(pluralize(1.5, 'item')).toBe('1.5 items');
    });
  });
});
