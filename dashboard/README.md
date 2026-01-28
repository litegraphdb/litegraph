<img src="../assets/favicon.png" height="48">

# LiteGraph Web Dashboard

A web interface for visualizing and managing network graphs, built with Next.js and React.

This dashboard is part of the [LiteGraph monorepo](../README.md).

## Documentation

Full documentation is available at [https://litegraph.readme.io/](https://litegraph.readme.io/).

## Features

Tenant Dashboard
This can be accessed by logging in to `localhost:3000/login`

- **Home**: Main graph visualization interface
- **Graphs**: Manage and edit graph definitions
- **Nodes**: View and edit graph nodes
- **Edges**: Manage connections between nodes
- **Labels**: Manage and edit labels
- **Tags**: Manage and edit tags
- **Vectors**: Manage and edit vectors

Admin Dashboard
This can be accessed by logging in to `localhost:3000/login/admin`

- **Tenants**: Manage and edit tenants
- **Users**: Manage and edit users
- **Credentials**: Manage and edit credentials

## Requirements

- Node.js v18.20.4
- npm

## Quick Start

### Development Setup

#### Install dependencies:

```bash
npm install
```

#### Set the LiteGraph instance URL

Update the `liteGraphInstanceURL` in [`src/constants/config.ts`](src/constants/config.ts) to point to your LiteGraph server instance:

```typescript
export const liteGraphInstanceURL = 'http://localhost:3000';
```

#### Start the production server (for using web ui locally):

```bash
npm run build
```

```bash
npm run start
```

OR

#### Start the development server (for development, can be used to test web ui locally as well):

```bash
npm run dev
```

The application will be available at `http://localhost:3000`.

### Testing

Run the test suite:

```bash
# Run all tests
npm test

# Run tests with coverage
npm run test:coverage

# Watch mode for development
npm run test:watch
```

## Deployment Process

#### Build the Application

Prepare the app for production:

```bash
npm run build
```

#### Start the Production Server

Start the built application:

```bash
npm run start
```

The app will be available at http://localhost:3000.

### Code Quality

The project uses several tools to maintain code quality:

- ESLint for code linting
- Prettier for code formatting
- Jest for testing
- Husky for pre-commit hooks

## Development Guidelines

1. **Code Style**

   - Follow the Prettier configuration
   - Use TypeScript for type safety
   - Follow component-based architecture

2. **Testing**
   - Write unit tests for components
