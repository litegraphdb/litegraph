import { setupServer } from 'msw/node';

export const getServer = (handlers) => {
  return setupServer(...handlers);
};
