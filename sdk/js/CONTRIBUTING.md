# Contributing

Thank you for your interest in contributing to the View AI JavaScript SDK!

The following is a set of guidelines for contributing to our project on Github. These are mostly guidelines, not rules.

## Code of Conduct

This project and everyone participating in it is governed by the Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to project moderators.

## Development Setup

### Development Installation

- Node.js v18.20.4
- npm

Use the command `npm i` to install dependencies and then `npm run build` to build the project.

#### Linking the project locally (for development and testing)

To use the library locally without publishing to a remote npm registry, first install the dependencies with the command:

```shell
npm install
```

Next, [link](https://docs.npmjs.com/cli/link) it globally in local system's npm with the following command:

```shell
npm link
```

To use the link you just defined in your project, switch to the directory you want to use your litegraphdb from, and run:

```shell
npm link litegraphdb
```

Finally, you need to build the sdk, use command:

```shell
npm run build
```

You can then import the SDK with either import or require based on the ES Module (esm) or CommonJS (cjs) project, as shown below:

```javascript
import sdk from 'litegraphdb';
//or
var sdk = require('litegraphdb');
```

## Code Style

We use several tools to maintain code quality:

- **Eslint**: For code standardization and linting
- **Prettier**: For code formatting
- **Type hints**: Required for all function parameters and return values

## Pull Requests

Please follow these guidelines when submitting pull requests (PRs):

- PRs should be manageable in size to make it easy for us to validate and integrate
- Each PR should be contained to a single fix or a single feature
- Include tests for new functionality
- Update documentation as needed
- Ensure all tests pass by running `npm run test`
- Include a clear description of the changes and their motivation

## Testing

We use `jest` for testing. Run the test suite:

```bash
# Run all tests
npm run test

```

## Asking Questions

Prior to asking questions, please review:

- Closed issues
- Wiki pages
- SDK documentation

If your question isn't answered in these resources, please file an issue! This helps us improve our documentation.

## Reporting Bugs

When reporting bugs, please provide the following information:

````
--- Bug Report ---

Operating system and version: (e.g., Ubuntu 20.04)
Node version: (e.g., Node 3.8.10)
SDK version: (e.g., view-sdk 1.0.0)
Issue encountered: (What went wrong?)
Expected behavior: (What should have happened?)
Steps to reproduce: (How can we replicate the issue?)

Sample code demonstrating the problem:
```javascript
var { LiteGraphSdk } = require('litegraphdb');

var api = new LiteGraphSdk(
  'http://example.com', //endpoint
  'default', //tenant Id
  'default' //access token
);

# Add code that demonstrates the issue
````

Error message and traceback: (if applicable)

--- End ---

```

## Suggesting Enhancements

When suggesting enhancements, please use this template:

```

--- Enhancement Request ---

Enhancement request title: (Clear, descriptive title)
Use case: (Why do you want this feature?)
Current behavior: (How does it work now?)
Requested behavior: (How should it work?)
Recommended implementation: (Optional: How would you implement this?)
Usefulness of the enhancement: (Who would benefit and how?)

## License

By contributing to this project, you agree that your contributions will be licensed under the project's [Fair Core License (FCL)](https://fcl.dev/) with graduation after two years to an Apache 2.0 license.
