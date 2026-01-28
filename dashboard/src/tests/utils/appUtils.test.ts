import {
  formatBytes,
  downloadBase64File,
  calculateTooltipPosition,
  getCreateEditViewModelTitle,
  decodeToJSON,
} from '@/utils/appUtils';

// Mock DOM methods
const mockCreateElement = jest.fn();
const mockAppendChild = jest.fn();
const mockRemoveChild = jest.fn();
const mockClick = jest.fn();
const mockRevokeObjectURL = jest.fn();
const mockCreateObjectURL = jest.fn();

// Mock console methods
const consoleSpy = {
  error: jest.spyOn(console, 'error').mockImplementation(() => {}),
  log: jest.spyOn(console, 'log').mockImplementation(() => {}),
};

// Mock atob and btoa
global.atob = jest.fn();
global.btoa = jest.fn();

// Mock setTimeout
jest.useFakeTimers();

describe('App Utils', () => {
  let consoleSpy: { error: jest.SpyInstance; log: jest.SpyInstance };

  beforeEach(() => {
    jest.clearAllMocks();

    // Setup console spies
    consoleSpy = {
      error: jest.spyOn(console, 'error').mockImplementation(() => {}),
      log: jest.spyOn(console, 'log').mockImplementation(() => {}),
    };

    // Reset DOM mocks
    Object.defineProperty(document, 'createElement', {
      value: mockCreateElement,
      writable: true,
    });

    Object.defineProperty(document, 'body', {
      value: {
        appendChild: mockAppendChild,
        removeChild: mockRemoveChild,
      },
      writable: true,
    });

    Object.defineProperty(global, 'URL', {
      value: {
        createObjectURL: mockCreateObjectURL,
        revokeObjectURL: mockRevokeObjectURL,
      },
      writable: true,
    });

    // Mock window dimensions
    Object.defineProperty(window, 'innerWidth', {
      value: 1920,
      writable: true,
    });

    Object.defineProperty(window, 'innerHeight', {
      value: 1080,
      writable: true,
    });
  });

  afterEach(() => {
    consoleSpy.error.mockRestore();
    consoleSpy.log.mockRestore();
  });

  describe('formatBytes', () => {
    it('should format 0 bytes correctly', () => {
      expect(formatBytes(0)).toBe('0 Bytes');
    });

    it('should format bytes correctly', () => {
      expect(formatBytes(1024)).toBe('1 KB');
    });

    it('should format megabytes correctly', () => {
      expect(formatBytes(1048576)).toBe('1 MB');
    });

    it('should format gigabytes correctly', () => {
      expect(formatBytes(1073741824)).toBe('1 GB');
    });

    it('should handle decimal values', () => {
      expect(formatBytes(1536)).toBe('1.5 KB');
    });

    it('should handle very large values', () => {
      expect(formatBytes(1099511627776)).toBe('1 TB');
    });
  });

  describe('downloadBase64File', () => {
    const mockLink = {
      href: '',
      download: '',
      click: mockClick,
    };

    beforeEach(() => {
      mockCreateElement.mockReturnValue(mockLink);
      (global.atob as jest.Mock).mockReturnValue('mock-binary-data');
      mockCreateObjectURL.mockReturnValue('mock-url');
    });

    it('should download file successfully with valid data', async () => {
      const base64Data = 'bW9jay1kYXRh';
      const filename = 'test.txt';
      const mimeType = 'text/plain';

      downloadBase64File(base64Data, filename, mimeType);

      expect(mockCreateElement).toHaveBeenCalledWith('a');
      expect(mockAppendChild).toHaveBeenCalledWith(mockLink);
      expect(mockRemoveChild).toHaveBeenCalledWith(mockLink);
      expect(mockClick).toHaveBeenCalled();
      expect(mockCreateObjectURL).toHaveBeenCalled();
    });

    it('should use default filename when not provided', () => {
      const base64Data = 'bW9jay1kYXRh';

      downloadBase64File(base64Data, '');

      expect(mockLink.download).toBe('backup.bin');
    });

    it('should use default mime type when not provided', () => {
      const base64Data = 'bW9jay1kYXRh';
      const filename = 'test.txt';

      downloadBase64File(base64Data, filename);

      expect(mockCreateElement).toHaveBeenCalledWith('a');
    });

    it('should handle empty base64 data', () => {
      downloadBase64File('', 'test.txt');

      expect(consoleSpy.error).toHaveBeenCalledWith('No base64 data provided for download.');
      expect(mockCreateElement).not.toHaveBeenCalled();
    });

    it('should handle null base64 data', () => {
      downloadBase64File(null as any, 'test.txt');

      expect(consoleSpy.error).toHaveBeenCalledWith('No base64 data provided for download.');
      expect(mockCreateElement).not.toHaveBeenCalled();
    });

    it('should handle download errors gracefully', () => {
      const base64Data = 'invalid-base64';
      (global.atob as jest.Mock).mockImplementation(() => {
        throw new Error('Invalid base64');
      });

      downloadBase64File(base64Data, 'test.txt');

      expect(consoleSpy.error).toHaveBeenCalledWith('Download failed:', expect.any(Error));
    });

    it('should revoke object URL after download', () => {
      const base64Data = 'bW9jay1kYXRh';

      downloadBase64File(base64Data, 'test.txt');

      jest.runAllTimers();

      expect(mockRevokeObjectURL).toHaveBeenCalledWith('mock-url');
    });
  });

  describe('calculateTooltipPosition', () => {
    it('should return original position when no overflow', () => {
      const result = calculateTooltipPosition(100, 100);

      expect(result).toEqual({ x: 100, y: 100 });
    });

    it('should adjust X position when overflowing right', () => {
      const result = calculateTooltipPosition(1900, 100);

      expect(result.x).toBe(1500); // 1920 - 410 - 10
      expect(result.y).toBe(100);
    });

    it('should adjust Y position when overflowing bottom', () => {
      const result = calculateTooltipPosition(100, 1000);

      expect(result.x).toBe(100);
      expect(result.y).toBe(750); // 1080 - 320 - 10
    });

    it('should adjust both positions when overflowing', () => {
      const result = calculateTooltipPosition(1900, 1000);

      expect(result.x).toBe(1500);
      expect(result.y).toBe(750);
    });

    it('should use custom dimensions when provided', () => {
      const result = calculateTooltipPosition(1900, 1000, 200, 300);

      expect(result.x).toBe(1610); // 1920 - 300 - 10
      expect(result.y).toBe(870); // 1080 - 200 - 10
    });
  });

  describe('getCreateEditViewModelTitle', () => {
    it('should return loading title when isLoading is true', () => {
      const result = getCreateEditViewModelTitle('User', true);
      expect(result).toBe('Loading User...');
    });

    it('should return view title when isView is true', () => {
      const result = getCreateEditViewModelTitle('User', false, false, false, true);
      expect(result).toBe('View User');
    });

    it('should return create title when isCreate is true', () => {
      const result = getCreateEditViewModelTitle('User', false, true);
      expect(result).toBe('Create User');
    });

    it('should return edit title when isEdit is true', () => {
      const result = getCreateEditViewModelTitle('User', false, false, true);
      expect(result).toBe('Edit User');
    });

    it('should return default title when no flags are set', () => {
      const result = getCreateEditViewModelTitle('User', false);
      expect(result).toBe('User');
    });

    it('should prioritize loading over other flags', () => {
      const result = getCreateEditViewModelTitle('User', true, true, true, true);
      expect(result).toBe('Loading User...');
    });
  });

  describe('decodeToJSON', () => {
    it('should parse valid JSON string', () => {
      const jsonString = '{"name": "John", "age": 30}';
      const result = decodeToJSON(jsonString);

      expect(result).toEqual({ name: 'John', age: 30 });
    });

    it('should return null for empty string', () => {
      const result = decodeToJSON('');
      expect(result).toBeNull();
    });

    it('should return null for undefined input', () => {
      const result = decodeToJSON(undefined);
      expect(result).toBeNull();
    });

    it('should return null for null input', () => {
      const result = decodeToJSON(null as any);
      expect(result).toBeNull();
    });

    it('should handle invalid JSON gracefully', () => {
      const invalidJson = '{"name": "John", "age": 30,}'; // Invalid trailing comma
      const result = decodeToJSON(invalidJson);

      expect(result).toBeNull();
      expect(consoleSpy.error).toHaveBeenCalledWith('Invalid JSON:', expect.any(String));
    });

    it('should handle malformed JSON', () => {
      const malformedJson = '{"name": "John" "age": 30}'; // Missing comma
      const result = decodeToJSON(malformedJson);

      expect(result).toBeNull();
      expect(consoleSpy.error).toHaveBeenCalledWith('Invalid JSON:', expect.any(String));
    });
  });
});
