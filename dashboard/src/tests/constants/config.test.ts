import {
  configuredLiteGraphInstanceURL,
  liteGraphInstanceURL,
  globalToastId,
} from '@/constants/config';

describe('Config Constants', () => {
  describe('liteGraphInstanceURL', () => {
    it('should be a valid URL', () => {
      expect(liteGraphInstanceURL).toBe('http://localhost:8701/');
    });

    it('should be a string', () => {
      expect(typeof liteGraphInstanceURL).toBe('string');
    });

    it('should not report a configured URL when no environment override is set', () => {
      expect(configuredLiteGraphInstanceURL).toBe('');
    });

    it('should expose configured server URLs with a trailing slash', () => {
      jest.isolateModules(() => {
        const original = process.env.LITEGRAPH_SERVER;
        process.env.LITEGRAPH_SERVER = 'http://litegraph.example:8701';

        const config = require('@/constants/config');

        expect(config.configuredLiteGraphInstanceURL).toBe('http://litegraph.example:8701/');
        expect(config.liteGraphInstanceURL).toBe('http://litegraph.example:8701/');

        if (original === undefined) {
          delete process.env.LITEGRAPH_SERVER;
        } else {
          process.env.LITEGRAPH_SERVER = original;
        }
      });
    });
  });

  describe('globalToastId', () => {
    it('should have the correct value', () => {
      expect(globalToastId).toBe('global-toast');
    });

    it('should be a string', () => {
      expect(typeof globalToastId).toBe('string');
    });
  });
});
