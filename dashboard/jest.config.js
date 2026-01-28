const nextJest = require('next/jest');

const createJestConfig = nextJest({
  dir: './',
});

const customJestConfig = {
  coverageProvider: 'v8',
  testEnvironment: './jest.environment.js',
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    '^antd/es/(.*)$': '<rootDir>/node_modules/antd/lib/$1',
    uuid: require.resolve('uuid'),
  },
  setupFilesAfterEnv: ['<rootDir>/jest.setup.js'],
  testEnvironmentOptions: {
    customExportConditions: [''],
  },
  transformIgnorePatterns: [
    '<rootDir>/node_modules/(?!react-hot-toast|@react-sigma/core|graphology|sigma)/',
  ],
  collectCoverageFrom: [
    'src/**/*.{ts,tsx}',
    'src/components/**/*.{ts,tsx}',
    'src/hooks/**/*.{ts,tsx}',
    'src/hoc/**/*.{ts,tsx}',
    'src/page/**/*.{ts,tsx}',
    'src/lib/**/*.{ts,tsx}',
    '!**/*.d.ts',
    '!**/node_modules/**',
    '!**/.next/**',
    '!**/coverage/**',
  ],
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 80,
      lines: 80,
      statements: 80,
    },
  },
  coverageReporters: ['json', 'lcov', 'text', 'clover'],
  coverageDirectory: 'coverage',
  transformIgnorePatterns: ['<rootDir>/node_modules/(?!react-hot-toast)/'],
};

module.exports = createJestConfig(customJestConfig);
