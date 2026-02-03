const Environment = require('jest-environment-jsdom').default;
const { ReadableStream, WritableStream, TransformStream } = require('stream/web');

module.exports = class CustomTestEnvironment extends Environment {
  async setup() {
    await super.setup();
    this.global.TextEncoder = TextEncoder;
    this.global.TextDecoder = TextDecoder;
    this.global.Response = Response;
    this.global.Request = Request;
    this.global.ReadableStream = ReadableStream;
    this.global.WritableStream = WritableStream;
    this.global.TransformStream = TransformStream;
  }
};
