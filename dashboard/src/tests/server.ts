import { RequestHandler } from 'msw';
import { setupServer } from 'msw/node';

export const getServer = (handlers: Array<RequestHandler>) => {
  return setupServer(...handlers);
};
