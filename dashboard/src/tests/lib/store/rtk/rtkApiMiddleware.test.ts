import '@testing-library/jest-dom';
import { errorHandler, rtkQueryErrorLogger } from '@/lib/store/rtk/rtkApiMiddleware';

// Mock antd message
jest.mock('antd', () => ({
  message: {
    error: jest.fn(),
  },
}));

// Mock the logOut action
jest.mock('@/lib/store/litegraph/actions', () => ({
  logOut: jest.fn(),
}));

// Mock isRejectedWithValue
jest.mock('@reduxjs/toolkit', () => ({
  isRejectedWithValue: jest.fn(),
}));

describe('RTK API Middleware', () => {
  let mockDispatch: jest.Mock;
  let mockMessage: any;
  let mockIsRejectedWithValue: jest.Mock;

  beforeEach(() => {
    jest.clearAllMocks();
    mockDispatch = jest.fn();
    mockMessage = require('antd').message;
    mockIsRejectedWithValue = require('@reduxjs/toolkit').isRejectedWithValue;
  });

  describe('errorHandler', () => {
    it('should handle error with Message property', () => {
      const error = {
        payload: {
          Message: 'Test error message',
        },
      };

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('Test error message');
    });

    it('should handle error with Description property', () => {
      const error = {
        payload: {
          Description: 'Test error description',
        },
      };

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('Test error description');
    });

    it('should handle error with message property', () => {
      const error = {
        payload: {
          message: 'Test error message',
        },
      };

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('Test error message');
    });

    it('should handle network error', () => {
      const error = {
        payload: {
          data: 'Network Error',
        },
      };

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('Network Error');
    });

    it('should handle generic error when no specific error message is found', () => {
      const error = {
        payload: {},
      };

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('Something went wrong.');
    });

    it('should handle NotAuthorized error and dispatch logout', () => {
      const { logOut } = require('@/lib/store/litegraph/actions');
      const error = {
        payload: {
          Error: 'NotAuthorized',
          Message: 'Session expired',
        },
      };

      // Mock setTimeout
      jest.useFakeTimers();

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('Session expired');
      expect(mockMessage.error).toHaveBeenCalledWith(
        'Session expired. Redirecting to login page...'
      );

      // Fast-forward time
      jest.runAllTimers();

      expect(mockDispatch).toHaveBeenCalledWith(logOut());

      jest.useRealTimers();
    });

    it('should handle error with null payload', () => {
      const error = {
        payload: null,
      };

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('Something went wrong.');
    });

    it('should handle error with undefined payload', () => {
      const error = {
        payload: undefined,
      };

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('Something went wrong.');
    });

    it('should handle error with whitespace message', () => {
      const error = {
        payload: {
          Message: '   ',
        },
      };

      errorHandler(error, mockDispatch);

      expect(mockMessage.error).toHaveBeenCalledWith('   ');
    });
  });

  describe('rtkQueryErrorLogger', () => {
    let mockApi: any;
    let mockNext: jest.Mock;
    let mockAction: any;

    beforeEach(() => {
      mockNext = jest.fn((action) => action);
      mockApi = {
        dispatch: mockDispatch,
      };
    });

    it('should call next for non-rejected actions', () => {
      mockAction = { type: 'SOME_ACTION' };
      mockIsRejectedWithValue.mockReturnValue(false);

      const result = rtkQueryErrorLogger(mockApi)(mockNext)(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(result).toBe(mockAction);
    });

    it('should handle rejected actions with value', () => {
      mockAction = {
        type: 'SOME_ACTION/rejected',
        payload: {
          Message: 'Test error',
        },
      };

      mockIsRejectedWithValue.mockReturnValue(true);

      const result = rtkQueryErrorLogger(mockApi)(mockNext)(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(mockMessage.error).toHaveBeenCalledWith('Test error');
      expect(result).toBe(mockAction);
    });

    it('should handle rejected actions without calling errorHandler for non-rejected-with-value', () => {
      mockAction = {
        type: 'SOME_ACTION/rejected',
        payload: {
          Message: 'Test error',
        },
      };

      mockIsRejectedWithValue.mockReturnValue(false);

      const result = rtkQueryErrorLogger(mockApi)(mockNext)(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(mockMessage.error).not.toHaveBeenCalled();
      expect(result).toBe(mockAction);
    });

    it('should handle rejected actions with NotAuthorized error', () => {
      const { logOut } = require('@/lib/store/litegraph/actions');
      mockAction = {
        type: 'SOME_ACTION/rejected',
        payload: {
          Error: 'NotAuthorized',
          Message: 'Session expired',
        },
      };

      mockIsRejectedWithValue.mockReturnValue(true);

      // Mock setTimeout
      jest.useFakeTimers();

      const result = rtkQueryErrorLogger(mockApi)(mockNext)(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(mockMessage.error).toHaveBeenCalledWith('Session expired');
      expect(mockMessage.error).toHaveBeenCalledWith(
        'Session expired. Redirecting to login page...'
      );

      // Fast-forward time
      jest.runAllTimers();

      expect(mockDispatch).toHaveBeenCalledWith(logOut());
      expect(result).toBe(mockAction);

      jest.useRealTimers();
    });

    it('should handle rejected actions with network error', () => {
      mockAction = {
        type: 'SOME_ACTION/rejected',
        payload: {
          data: 'Network Error',
        },
      };

      mockIsRejectedWithValue.mockReturnValue(true);

      const result = rtkQueryErrorLogger(mockApi)(mockNext)(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(mockMessage.error).toHaveBeenCalledWith('Network Error');
      expect(result).toBe(mockAction);
    });

    it('should handle rejected actions with generic error', () => {
      mockAction = {
        type: 'SOME_ACTION/rejected',
        payload: {},
      };

      mockIsRejectedWithValue.mockReturnValue(true);

      const result = rtkQueryErrorLogger(mockApi)(mockNext)(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(mockMessage.error).toHaveBeenCalledWith('Something went wrong.');
      expect(result).toBe(mockAction);
    });

    it('should handle rejected actions with null payload', () => {
      mockAction = {
        type: 'SOME_ACTION/rejected',
        payload: null,
      };

      mockIsRejectedWithValue.mockReturnValue(true);

      const result = rtkQueryErrorLogger(mockApi)(mockNext)(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(mockMessage.error).toHaveBeenCalledWith('Something went wrong.');
      expect(result).toBe(mockAction);
    });

    it('should handle rejected actions with undefined payload', () => {
      mockAction = {
        type: 'SOME_ACTION/rejected',
        payload: undefined,
      };

      mockIsRejectedWithValue.mockReturnValue(true);

      const result = rtkQueryErrorLogger(mockApi)(mockNext)(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(mockMessage.error).toHaveBeenCalledWith('Something went wrong.');
      expect(result).toBe(mockAction);
    });

    it('should handle middleware chain correctly', () => {
      mockAction = { type: 'SOME_ACTION' };
      mockIsRejectedWithValue.mockReturnValue(false);

      const middleware = rtkQueryErrorLogger(mockApi);
      const nextHandler = middleware(mockNext);
      const result = nextHandler(mockAction);

      expect(mockNext).toHaveBeenCalledWith(mockAction);
      expect(result).toBe(mockAction);
    });
  });
});
