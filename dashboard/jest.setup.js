import '@testing-library/jest-dom';
import { Table } from 'antd';
import { LiteGraphSdk } from 'litegraphdb';

// Mock the antd React 19 patch since tests mock antd without unstableSetRender
jest.mock('@ant-design/v5-patch-for-react-19', () => ({}));

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
