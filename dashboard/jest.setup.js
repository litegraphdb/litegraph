import '@testing-library/jest-dom';

// Mock the antd React 19 patch since tests mock antd without unstableSetRender
jest.mock('@ant-design/v5-patch-for-react-19', () => ({}));

// next-intl ships ESM only, and next/jest's transformIgnorePatterns prevent it
// from being transformed here. Provide a faithful, lightweight mock backed by
// the real message catalogs so components using `useTranslations` render real
// (English by default, or provider-selected) translations in tests.
jest.mock('next-intl', () => {
  const React = require('react');
  const catalogs = {
    en: require('./messages/en.json'),
    es: require('./messages/es.json'),
  };

  const IntlContext = React.createContext({ locale: 'en', messages: catalogs.en });

  const resolve = (messages, namespace, key) => {
    const path = (namespace ? `${namespace}.${key}` : key).split('.');
    let node = messages;
    for (const part of path) {
      if (node && typeof node === 'object' && part in node) node = node[part];
      else return undefined;
    }
    return typeof node === 'string' ? node : undefined;
  };

  const formatICU = (message, values = {}) => {
    let out = message.replace(
      /\{(\w+),\s*plural,\s*([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}/g,
      (_, name, body) => {
        const count = Number(values[name]);
        const opts = {};
        const optRe = /(=\d+|zero|one|two|few|many|other)\s*\{([^{}]*)\}/g;
        let mm;
        while ((mm = optRe.exec(body))) opts[mm[1]] = mm[2];
        let choice = opts[`=${count}`];
        if (choice === undefined) choice = opts[count === 1 ? 'one' : 'other'] ?? opts.other ?? '';
        return choice.replace(/#/g, String(count));
      }
    );
    out = out.replace(/\{(\w+)\}/g, (_, k) => (values[k] !== undefined ? String(values[k]) : `{${k}}`));
    return out;
  };

  const makeT = (messages, namespace) => {
    const t = (key, values) => {
      const raw = resolve(messages, namespace, key);
      if (raw === undefined) return namespace ? `${namespace}.${key}` : key;
      return formatICU(raw, values);
    };
    t.rich = (key, values) => t(key, values);
    t.markup = (key, values) => t(key, values);
    t.raw = (key) => resolve(messages, namespace, key);
    return t;
  };

  return {
    __esModule: true,
    NextIntlClientProvider: ({ locale = 'en', messages, children }) =>
      React.createElement(
        IntlContext.Provider,
        { value: { locale, messages: messages || catalogs[locale] || catalogs.en } },
        children
      ),
    useTranslations: (namespace) => {
      const ctx = React.useContext(IntlContext);
      return makeT(ctx.messages || catalogs.en, namespace);
    },
    useLocale: () => React.useContext(IntlContext).locale,
    useMessages: () => React.useContext(IntlContext).messages,
    createTranslator: ({ messages, namespace }) => makeT(messages || catalogs.en, namespace),
  };
});

jest.mock('litegraphdb', () => {
  const mockData = require('./src/tests/pages/mockData');

  const enumerate = (objects) => ({
    Success: true,
    Timestamp: {
      Start: '2025-01-01T00:00:00Z',
      End: '2025-01-01T00:00:00Z',
      TotalMs: 0,
      Messages: {},
    },
    ContinuationToken: undefined,
    MaxResults: 10,
    EndOfResults: true,
    TotalRecords: objects.length,
    RecordsRemaining: 0,
    Objects: objects,
  });

  const resource = (objects) => ({
    // Every list-returning GET route now responds with the EnumerationResult envelope.
    readAll: jest.fn().mockImplementation(() => Promise.resolve(enumerate(objects))),
    enumerate: jest.fn().mockImplementation(() => Promise.resolve(enumerate(objects))),
    enumerateAndSearch: jest.fn().mockImplementation(() => Promise.resolve(enumerate(objects))),
    read: jest.fn().mockImplementation(() => Promise.resolve(objects[0] || null)),
    readMany: jest.fn().mockImplementation(() => Promise.resolve(enumerate(objects))),
    search: jest.fn().mockResolvedValue(objects),
    create: jest.fn().mockImplementation((value) => Promise.resolve(value || objects[0] || {})),
    update: jest.fn().mockImplementation((value) => Promise.resolve(value || objects[0] || {})),
    delete: jest.fn().mockResolvedValue(true),
  });

  class LiteGraphSdk {
    constructor(endpoint) {
      this.config = { endpoint: endpoint || 'http://localhost:8701/' };
      this.Tenant = resource(mockData.mockTenantData);
      this.User = resource(mockData.mockUserData);
      this.Credential = resource(mockData.mockCredentialData);
      this.Graph = {
        ...resource(mockData.mockGraphData),
        exportGexf: jest.fn().mockResolvedValue(mockData.mockGraphData[0]?.gexfContent || ''),
        enableVectorIndex: jest.fn().mockResolvedValue(true),
        readVectorIndexConfig: jest.fn().mockResolvedValue({ VectorIndexType: 'HnswSqlite' }),
        readVectorIndexStats: jest.fn().mockResolvedValue({ VectorCount: 0, Dimensionality: 0 }),
        rebuildVectorIndex: jest.fn().mockResolvedValue(true),
        deleteVectorIndex: jest.fn().mockResolvedValue(true),
        readSubGraph: jest.fn().mockResolvedValue({ Nodes: [], Edges: [] }),
      };
      this.Node = resource(mockData.mockNodeData);
      this.Edge = resource(mockData.mockEdgeData);
      this.Tag = resource(mockData.mockTagData.allTags);
      this.Label = resource(mockData.mockLabelData);
      this.Vector = resource(mockData.mockVectorData);
      this.Backup = resource(mockData.mockBackupData);
      this.Admin = {
        flush: jest.fn().mockResolvedValue(true),
      };
      this.Authentication = {
        getTenantsForEmail: jest
          .fn()
          .mockImplementation(() => Promise.resolve(enumerate(mockData.mockTenantData))),
        generateToken: jest.fn().mockResolvedValue(mockData.mockToken),
        getTokenDetails: jest.fn().mockResolvedValue(mockData.mockToken),
      };
    }

    validateConnectivity() {
      return Promise.resolve(true);
    }
  }

  return { LiteGraphSdk };
});

// Mock msw and msw/node to avoid ESM import issues
jest.mock('msw', () => ({
  http: {
    get: jest.fn((url, handler) => ({ url, handler, method: 'GET' })),
    post: jest.fn((url, handler) => ({ url, handler, method: 'POST' })),
    put: jest.fn((url, handler) => ({ url, handler, method: 'PUT' })),
    delete: jest.fn((url, handler) => ({ url, handler, method: 'DELETE' })),
    patch: jest.fn((url, handler) => ({ url, handler, method: 'PATCH' })),
  },
  HttpResponse: {
    json: jest.fn((data) => data),
    text: jest.fn((data) => data),
    error: jest.fn(() => new Error('Mock error')),
  },
  delay: jest.fn(() => Promise.resolve()),
}));

jest.mock('msw/node', () => ({
  setupServer: jest.fn((...handlers) => ({
    listen: jest.fn(),
    close: jest.fn(),
    resetHandlers: jest.fn(),
    use: jest.fn(),
    restoreHandlers: jest.fn(),
  })),
}));

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter() {
    return {
      push: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    };
  },
  usePathname() {
    return '';
  },
  useSearchParams() {
    return new URLSearchParams();
  },
}));

// Patch unsupported selector behavior in Ant Design
Object.defineProperty(window, 'getComputedStyle', {
  value: () => ({
    getPropertyValue: () => '',
  }),
});

// jest.useFakeTimers();
// // Set a fixed date for consistent snapshots
// jest.setSystemTime(new Date('2023-01-01T12:00:00Z'));
// // ... your test code
// jest.useRealTimers(); // Restore real timers after the test

beforeAll(() => {
  jest.useFakeTimers('modern');
  jest.setSystemTime(new Date('2023-01-01T12:00:00Z'));
});

afterAll(() => {
  jest.useRealTimers();
});

// jest.mock('litegraphdb', () => {
//   return {
//     LiteGraphSdk: jest.fn().mockImplementation(() => ({
//       // Mock fetchEdgeById method
//       fetchEdgeById: jest.fn().mockResolvedValue({
//         GUID: '7b665c02-249a-4029-b782-92526f50d357',
//         GraphGUID: 'd52e9587-f9a8-4348-9cfe-b860ab13ba54',
//         Name: 'new test 23',
//         From: '0be10847-040f-4ce5-8694-72da08824f1e',
//         To: 'ada1be94-0272-45bb-84fd-75e14f56ffe8',
//         Cost: 2,
//         CreatedUtc: '2024-12-24T07:34:47.695616Z',
//         Data: {},
//       }),
//       // Mock exportGraphToGexf method
//       exportGraphToGexf: jest
//         .fn()
//         .mockResolvedValue(
//           `<?xml version="1.0" encoding="utf-8"?> <gexf xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xsi:schemaLocation="http://www.gexf.net/1.3 http://www.gexf.net/1.3/gexf.xsd" version="1.3" xmlns="http://www.gexf.net/1.3"> <meta lastmodifieddate="2025-01-02T07:37:34.9191749Z"> <creator>LiteGraph</creator> <description>Graph from LiteGraph https://github.com/jchristn/litegraph</description> </meta> <graph defaultedgetype="directed"> <attributes class="node"> <attribute id="0" title="props" type="string" /> </attributes> <nodes> <node id="3897b459-a197-48e0-bb8d-e54255675713" label="new one 2"> <attvalues> <attvalue for="Name" value="new one 2" /> </attvalues> </node> <node id="0be10847-040f-4ce5-8694-72da08824f1e" label="new one"> <attvalues> <attvalue for="Name" value="new one" /> </attvalues> </node> <node id="ada1be94-0272-45bb-84fd-75e14f56ffe8" label="test"> <attvalues> <attvalue for="Name" value="test" /> </attvalues> </node> <node id="976e0552-2462-494b-82bc-fd113f89409c" label="My test node 121"> <attvalues> <attvalue for="Name" value="My test node 121" /> </attvalues> </node> </nodes> <edges> <edge id="7b665c02-249a-4029-b782-92526f50d357" source="0be10847-040f-4ce5-8694-72da08824f1e" target="ada1be94-0272-45bb-84fd-75e14f56ffe8"> <attvalues> <attvalue for="Name" value="new test 23" /> <attvalue for="Cost" value="2" /> </attvalues> </edge> <edge id="d35bea13-f548-43be-b005-c79191117334" source="976e0552-2462-494b-82bc-fd113f89409c" target="ada1be94-0272-45bb-84fd-75e14f56ffe8"> <attvalues> <attvalue for="Name" value="test edge" /> <attvalue for="Cost" value="5" /> </attvalues> </edge> </edges> </graph> </gexf>`
//         ),
//       // Mock createEdge method
//       createEdge: jest.fn().mockResolvedValue({
//         GUID: '7b665g72-249a-4029-b782-92526f50d357',
//         GraphGUID: 'd52e9587-f9a8-4348-9cfe-b860ab13ba54',
//         Name: 'new test 23',
//         From: '3fc519b1-5f78-4fc7-810b-c18fade549db',
//         To: '6fe5823b-31ad-4431-9d3d-53d8b6f1ed4c',
//         Cost: 2,
//         CreatedUtc: '2024-12-24T07:34:47.695616Z',
//         Data: {},
//       }),
//       // Mock readEdges method
//       readEdges: jest.fn().mockResolvedValue([
//         {
//           GUID: '7b665c02-249a-4029-b782-92526f50d357',
//           GraphGUID: 'd52e9587-f9a8-4348-9cfe-b860ab13ba54',
//           Name: 'new test 23',
//           From: '3fc519b1-5f78-4fc7-810b-c18fade549db',
//           To: '6fe5823b-31ad-4431-9d3d-53d8b6f1ed4c',
//           Cost: 2,
//           CreatedUtc: '2024-12-24T07:34:47.695616Z',
//           Data: {},
//         },
//         {
//           GUID: 'd35bea13-f548-43be-b005-c79191117334',
//           GraphGUID: 'd52e9587-f9a8-4348-9cfe-b860ab13ba54',
//           Name: 'test edge',
//           From: '976e0552-2462-494b-82bc-fd113f89409c',
//           To: 'ada1be94-0272-45bb-84fd-75e14f56ffe8',
//           Cost: 5,
//           CreatedUtc: '2024-12-17T09:24:39.806284Z',
//           Data: {},
//         },
//       ]),
//       // Mock tag-related methods
//       readTags: jest.fn().mockResolvedValue([
//         {
//           GUID: '123e4567-e89b-12d3-a456-426614174000',
//           TenantGUID: '123e4567-e89b-12d3-a456-426614174111',
//           GraphGUID: '123e4567-e89b-12d3-a456-426614174222',
//           NodeGUID: '123e4567-e89b-12d3-a456-426614174333',
//           Key: 'priority',
//           Value: 'high',
//           CreatedUtc: '2024-01-15T10:00:00.000Z',
//           LastUpdateUtc: '2024-01-15T10:00:00.000Z',
//         },
//         {
//           GUID: '123e4567-e89b-12d3-a456-426614174001',
//           TenantGUID: '123e4567-e89b-12d3-a456-426614174111',
//           GraphGUID: '123e4567-e89b-12d3-a456-426614174222',
//           NodeGUID: '123e4567-e89b-12d3-a456-426614174333',
//           Key: 'status',
//           Value: 'active',
//           CreatedUtc: '2024-01-15T10:00:00.000Z',
//           LastUpdateUtc: '2024-01-15T10:00:00.000Z',
//         },
//       ]),
//       createTag: jest.fn().mockResolvedValue({
//         GUID: '123e4567-e89b-12d3-a456-426614174002',
//         TenantGUID: '123e4567-e89b-12d3-a456-426614174111',
//         GraphGUID: '123e4567-e89b-12d3-a456-426614174222',
//         NodeGUID: '123e4567-e89b-12d3-a456-426614174333',
//         Key: 'newTag',
//         Value: 'newValue',
//         CreatedUtc: '2024-01-15T10:00:00.000Z',
//         LastUpdateUtc: '2024-01-15T10:00:00.000Z',
//       }),
//       updateTag: jest.fn().mockResolvedValue({
//         GUID: '123e4567-e89b-12d3-a456-426614174000',
//         TenantGUID: '123e4567-e89b-12d3-a456-426614174111',
//         GraphGUID: '123e4567-e89b-12d3-a456-426614174222',
//         NodeGUID: '123e4567-e89b-12d3-a456-426614174333',
//         Key: 'priority',
//         Value: 'updated',
//         CreatedUtc: '2024-01-15T10:00:00.000Z',
//         LastUpdateUtc: '2024-01-15T11:00:00.000Z',
//       }),
//       deleteTag: jest.fn().mockResolvedValue({
//         GUID: '123e4567-e89b-12d3-a456-426614174000',
//         TenantGUID: '123e4567-e89b-12d3-a456-426614174111',
//         GraphGUID: '123e4567-e89b-12d3-a456-426614174222',
//         NodeGUID: '123e4567-e89b-12d3-a456-426614174333',
//         Key: 'priority',
//         Value: 'high',
//         CreatedUtc: '2024-01-15T10:00:00.000Z',
//         LastUpdateUtc: '2024-01-15T10:00:00.000Z',
//       }),
//       // Mock readGraphs method
//       readGraphs: jest.fn().mockResolvedValue([
//         {
//           GUID: 'e6d4294e-6f49-4d67-8260-5e44c2b077a6',
//           Name: 'Test Demo Graph',
//           CreatedUtc: '2024-12-23T15:36:07.816289Z',
//           Data: {
//             name: 'value',
//           },
//         },
//         {
//           GUID: 'd52aeab4-4de7-4076-98dd-461d4a61ac88',
//           Name: 'testttt 2',
//           CreatedUtc: '2024-12-18T07:28:53.965316Z',
//           Data: {},
//         },
//       ]),
//       // Mock createGraph method
//       createGraph: jest.fn().mockResolvedValue({
//         GUID: 'b35234a5-e2d9-4401-b9a9-ea74e47f8c16',
//         Name: 'New Graph',
//         CreatedUtc: '2024-12-26T11:50:04.451739Z',
//         Data: {
//           graph: {},
//         },
//       }),
//       // Mock updateGraph method
//       updateGraph: jest.fn().mockResolvedValue({
//         GUID: 'b35234a5-e2d9-4401-b9a9-ea74e47f8c16',
//         Name: 'Updated Graph',
//         CreatedUtc: '2024-12-26T11:50:04.451739Z',
//         Data: {
//           graph: {},
//         },
//       }),
//       // Mock deleteGraph method
//       deleteGraph: jest.fn().mockResolvedValue({
//         GUID: 'b35234a5-e2d9-4401-b9a9-ea74e47f8c16',
//         Name: 'Updated Graph',
//         CreatedUtc: '2024-12-26T11:50:04.451739Z',
//         Data: {
//           graph: {},
//         },
//       }),
//       // Mock readNodes method
//       readNodes: jest.fn().mockResolvedValue([
//         {
//           GUID: '3fc519b1-5f78-4fc7-810b-c18fade549db',
//           GraphGUID: '01010101-0101-0101-0101-010101010101',
//           Name: 'My updated test node',
//           Data: {},
//           CreatedUtc: '2024-12-18T07:27:58.206132Z',
//         },
//         {
//           GUID: '6fe5823b-31ad-4431-9d3d-53d8b6f1ed4c',
//           GraphGUID: '01010101-0101-0101-0101-010101010101',
//           Name: 'test sas',
//           Data: {
//             Hello: 'world',
//           },
//           CreatedUtc: '2024-12-16T08:27:17.639147Z',
//         },
//       ]),
//       // Mock createNode method
//       createNode: jest.fn().mockResolvedValue({
//         GUID: 'b35234a5-e2d9-4401-b9a9-ea74e47f8a16',
//         GraphGUID: '01010101-0101-0101-0101-010101010101',
//         Name: 'New Node',
//         CreatedUtc: '2024-12-26T11:50:04.451739Z',
//         Data: {
//           graph: {},
//         },
//       }),
//       // Mock updateNode method
//       updateNode: jest.fn().mockResolvedValue({
//         GUID: 'b35234a5-e2d9-4401-b9a9-ea74e47f8a16',
//         GraphGUID: '01010101-0101-0101-0101-010101010101',
//         Name: 'New Updated Node',
//         CreatedUtc: '2024-12-26T11:50:04.451739Z',
//         Data: {
//           graph: {},
//         },
//       }),
//       // Mock deleteNode method
//       deleteNode: jest.fn().mockResolvedValue({
//         GUID: 'b35234a5-e2d9-4401-b9a9-ea74e47f8a16',
//         GraphGUID: '01010101-0101-0101-0101-010101010101',
//         Name: 'New Updated Node',
//         CreatedUtc: '2024-12-26T11:50:04.451739Z',
//         Data: {
//           graph: {},
//         },
//       }),
//     })),
//   };
// });

// jest.mock('./components/base/select/Select', () => ({
//   __esModule: true,
//   default: jest.fn((props) => (
//     <div data-testid="mocked-litegraph-select" {...props}>
//       Mocked LitegraphSelect
//     </div>
//   )),
// }));
// Mock rc-util scrollbar size logic
jest.mock('rc-util/lib/getScrollBarSize', () => ({
  __esModule: true,
  getTargetScrollBarSize: () => ({
    width: 0,
    height: 0,
  }),
}));

// eslint-disable-next-line react/display-name
jest.mock('@/components/base/select/Select', () => (props) => {
  return (
    <select
      data-testid={props['data-testid']}
      value={props.value}
      onChange={(e) => props.onChange(e.target.value)}
    >
      {props?.options?.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  );
});

jest.mock('jsoneditor-react', () => ({
  JsonEditor: ({ value, onChange }) => (
    <input
      data-testid="json-editor-textarea"
      value={value}
      onChange={(e) => onChange(e.target.value)}
    />
  ),
}));

window.matchMedia =
  window.matchMedia ||
  function () {
    return {
      matches: false,
      addListener: () => {},
      removeListener: () => {},
    };
  };

class BroadcastChannelMock {
  constructor(channelName) {
    this.name = channelName;
    this.onmessage = null;
  }

  postMessage(message) {
    if (this.onmessage) {
      this.onmessage({ data: message });
    }
  }

  close() {
    // No-op
  }
}

global.BroadcastChannel = BroadcastChannelMock;

global.TransformStream = class {
  constructor() {
    this.readable = {};
    this.writable = {};
  }
};
