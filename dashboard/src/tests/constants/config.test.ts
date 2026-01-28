import { liteGraphInstanceURL, globalToastId } from '@/constants/config';

describe('Config Constants', () => {
  describe('liteGraphInstanceURL', () => {
    it('should be a valid URL', () => {
      expect(liteGraphInstanceURL).toBe('http://localhost:8701/');
    });

    it('should be a string', () => {
      expect(typeof liteGraphInstanceURL).toBe('string');
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
