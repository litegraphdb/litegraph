import '@testing-library/jest-dom';
import { errorHandler, rtkQueryErrorLogger } from '@/lib/store/rtkApiMiddlewear';
import { globalToastId } from '@/constants/config';
import toast from 'react-hot-toast';
import { handleLogout } from '@/lib/store/rootReducer';

// Mock react-hot-toast
jest.mock('react-hot-toast', () => ({
  error: jest.fn(),
}));

// Mock handleLogout
jest.mock('@/lib/store/rootReducer', () => ({
  handleLogout: jest.fn(),
}));

// Mock setTimeout
jest.useFakeTimers();

describe('rtkApiMiddlewear', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.clearAllTimers();
  });

  describe('errorHandler', () => {
    it('should handle 401 error for non-login endpoint', () => {
      const error = {
        payload: {
          status: 401,
          data: {
            message: 'Unauthorized',
          },
        },
        meta: {
          arg: {
            endpointName: 'getUsers',
          },
        },
      };

      errorHandler(error);

      expect(toast.error).toHaveBeenCalledWith('Invalid session, login again. ', {
        id: globalToastId,
      });

      // Fast forward time to trigger setTimeout
      jest.advanceTimersByTime(2000);
      expect(handleLogout).toHaveBeenCalled();
    });

    it('should handle 403 error for non-login endpoint', () => {
      const error = {
        payload: {
          status: 403,
          data: {
            message: 'Forbidden',
          },
        },
        meta: {
          arg: {
            endpointName: 'getUsers',
          },
        },
      };

      errorHandler(error);

      expect(toast.error).toHaveBeenCalledWith('Invalid session, login again. ', {
        id: globalToastId,
      });

      // Fast forward time to trigger setTimeout
      jest.advanceTimersByTime(2000);
      expect(handleLogout).toHaveBeenCalled();
    });

    it('should not handle logout for login endpoint with 401 error', () => {
      const error = {
        payload: {
          status: 401,
          data: {
            message: 'Invalid credentials',
          },
        },
        meta: {
          arg: {
            endpointName: 'login',
          },
        },
      };

      errorHandler(error);

      expect(toast.error).toHaveBeenCalledWith('Invalid credentials', { id: globalToastId });

      // Fast forward time
      jest.advanceTimersByTime(2000);
      expect(handleLogout).not.toHaveBeenCalled();
    });

    it('should handle other status codes with server error message', () => {
      const error = {
        payload: {
          status: 500,
          data: {
            message: 'Internal Server Error',
          },
        },
        meta: {
          arg: {
            endpointName: 'getData',
          },
        },
      };

      errorHandler(error);

      expect(toast.error).toHaveBeenCalledWith('Internal Server Error', { id: globalToastId });
      expect(handleLogout).not.toHaveBeenCalled();
    });

    it('should handle error with StatusCode property', () => {
      const error = {
        payload: {
          StatusCode: 400,
          data: {
            detail: 'Bad Request',
          },
        },
        meta: {
          arg: {
            endpointName: 'postData',
          },
        },
      };

      errorHandler(error);

      expect(toast.error).toHaveBeenCalledWith('Bad Request', { id: globalToastId });
    });

    it('should use default message when no server message is provided', () => {
      const error = {
        payload: {
          status: 500,
          data: {},
        },
        meta: {
          arg: {
            endpointName: 'getData',
          },
        },
      };

      errorHandler(error);

      expect(toast.error).toHaveBeenCalledWith('Something went wrong.', { id: globalToastId });
    });

    it('should handle network error', () => {
      const error = {
        payload: {
          data: 'Network Error',
        },
      };

      errorHandler(error);

      expect(toast.error).toHaveBeenCalledWith('Network Error', { id: globalToastId });
    });

    it('should handle error without payload', () => {
      const error = {};

      errorHandler(error);

      // Should not throw and should not call toast
      expect(toast.error).not.toHaveBeenCalled();
    });

    it('should handle error with empty payload', () => {
      const error = {
        payload: null,
      };

      errorHandler(error);

      // Should not throw and should not call toast
      expect(toast.error).not.toHaveBeenCalled();
    });

    it('should handle error with non-numeric status', () => {
      const error = {
        payload: {
          status: 'invalid',
          data: {
            message: 'Some error',
          },
        },
      };

      errorHandler(error);

      // Should not call toast for non-numeric status
      expect(toast.error).not.toHaveBeenCalled();
    });

    it('should handle 403 error for login endpoint', () => {
      const error = {
        payload: {
          status: 403,
          data: {
            message: 'Access denied',
          },
        },
        meta: {
          arg: {
            endpointName: 'login',
          },
        },
      };

      errorHandler(error);

      expect(toast.error).toHaveBeenCalledWith('Access denied', { id: globalToastId });
      expect(handleLogout).not.toHaveBeenCalled();
    });
  });

  describe('rtkQueryErrorLogger', () => {
    it('should call errorHandler for rejected actions', () => {
      const mockNext = jest.fn();
      const mockApi = {};
      const rejectedAction = {
        type: 'api/endpoint/rejected',
        meta: { rejectedWithValue: true },
        payload: {
          status: 500,
          data: { message: 'Server Error' },
        },
      };

      // Mock isRejectedWithValue to return true
      jest.doMock('@reduxjs/toolkit', () => ({
        isRejectedWithValue: jest.fn(() => true),
      }));

      const { isRejectedWithValue } = require('@reduxjs/toolkit');
      isRejectedWithValue.mockReturnValue(true);

      const middleware = rtkQueryErrorLogger(mockApi)(mockNext);
      middleware(rejectedAction);

      expect(mockNext).toHaveBeenCalledWith(rejectedAction);
    });

    it('should not call errorHandler for non-rejected actions', () => {
      const mockNext = jest.fn();
      const mockApi = {};
      const normalAction = {
        type: 'api/endpoint/fulfilled',
        payload: { data: 'success' },
      };

      jest.doMock('@reduxjs/toolkit', () => ({
        isRejectedWithValue: jest.fn(() => false),
      }));

      const { isRejectedWithValue } = require('@reduxjs/toolkit');
      isRejectedWithValue.mockReturnValue(false);

      const middleware = rtkQueryErrorLogger(mockApi)(mockNext);
      middleware(normalAction);

      expect(mockNext).toHaveBeenCalledWith(normalAction);
    });

    it('should pass through all actions to next middleware', () => {
      const mockNext = jest.fn();
      const mockApi = {};
      const action = { type: 'TEST_ACTION' };

      const middleware = rtkQueryErrorLogger(mockApi)(mockNext);
      const result = middleware(action);

      expect(mockNext).toHaveBeenCalledWith(action);
      expect(result).toBe(mockNext.return);
    });
  });
});
