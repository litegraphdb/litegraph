// Add any custom config to be passed to Jest
const config = {
  coverageProvider: 'v8',
  collectCoverageFrom: ['src/**/*.js', '!src/**/*.d.ts', '!**/mocks/**', '!**/lib/**', '!**/data/**', '!**/tests/**'],
  transform: {
    '^.+\\.[jt]sx?$': ['babel-jest', {
      presets: [['@babel/preset-env', { modules: 'commonjs', targets: { node: 'current' } }]],
    }],
  },
  transformIgnorePatterns: [
    '[/\\\\]node_modules[/\\\\](?!(@bundled-es-modules|@mswjs|headers-polyfill|msw|outvariant|strict-event-emitter|until-async)[/\\\\])',
  ],
};

// createJestConfig is exported this way to ensure that next/jest can load the Next.js config which is async
module.exports = config;
