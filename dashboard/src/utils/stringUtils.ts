import { UserMetadata } from 'litegraphdb/dist/types/types';
import { v4 } from 'uuid';

export const toTitleCase = (str: string): string => {
  return str
    .toLowerCase()
    .replace(/-/g, ' ') // Replace hyphens with spaces
    .split(' ')
    .map((word: string) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
};

export const getFirstLetterOfTheWord = (value: string) => {
  return (value?.substring(0, 1) || '').toUpperCase();
};

export const uuid = () => {
  return v4();
};

export const getUserName = (user: UserMetadata | null) => {
  return user?.FirstName || user?.LastName ? [user?.FirstName, user?.LastName].join(' ') : 'User';
};

export const pluralize = (count: number, word: string, suffix: string = 's'): string => {
  return `${count} ${word}${count > 1 ? suffix : ''}`;
};
