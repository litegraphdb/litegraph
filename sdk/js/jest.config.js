// Add any custom config to be passed to Jest
const config = {
  coverageProvider: 'v8',
  collectCoverageFrom: ['src/**/*.js', '!src/**/*.d.ts', '!**/mocks/**', '!**/lib/**', '!**/data/**', '!**/tests/**'],
};

// createJestConfig is exported this way to ensure that next/jest can load the Next.js config which is async
module.exports = config;
