export const mockGraphGuid = '01010101-0101-0101-0101-010101010101';
export const mockNodeGuid = '01010101-0101-0101-0101-010101010101';
export const toNodeGuid = '01010101-0101-0101-0101-010101010101';
export const fromNodeGuid = '01010101-0101-0101-0101-010101010101';
export const edgeData = {
  //Response for Get Edges from Node,get edges to node,get node edges, edges between nodes:
  [mockNodeGuid]: {
    GUID: '01010101-0101-0101-0101-010101010101',
    GraphGUID: '01010101-0101-0101-0101-010101010101',
    Name: 'Default Edge1',
    From: '3366752f-e634-4de9-9280-d94ea667bf23',
    To: 'cf935603-616a-4495-b238-e8cad096c714',
    Cost: 0,
    CreatedUtc: '2024-10-21T11:56:14.485466Z',
    LastUpdateUtc: '2024-10-21T11:56:14.485466Z',
    Data: {
      Hello: 'World3',
    },
  },
  '02020202-0202-0202-0202-020202020202': {
    GUID: '02020202-0202-0202-0202-020202020202',
    GraphGUID: '02020202-0202-0202-0202-020202020202',
    Name: 'My test edge',
    From: '3366752f-e634-4de9-9280-d94ea667bf23',
    To: 'cf935603-616a-4495-b238-e8cad096c714',
    Cost: 0,
    CreatedUtc: '2024-10-21T11:51:16.673555Z',
    LastUpdateUtc: '2024-10-21T11:51:16.673555Z',
    Data: {},
  },
};
export const edgeMockApiResponse = Object.values(edgeData);

export const nodeData = {
  //Response for get node neighbors,get node parents,get node children
  [mockNodeGuid]: {
    GUID: '01010101-0101-0101-0101-010101010101',
    GraphGUID: '01010101-0101-0101-0101-010101010101',
    Name: 'My test node 1',
    Data: {
      Hello: 'World 12',
    },
    CreatedUtc: '2024-10-21T06:12:51.477967Z',
    LastUpdateUtc: '2024-10-21T06:12:51.477967Z',
  },
  '02020202-0202-0202-0202-020202020202': {
    GUID: '02020202-0202-0202-0202-020202020202',
    GraphGUID: '02020202-0202-0202-0202-020202020202',
    Name: 'My test node 1',
    Data: {
      Hello: 'World 12',
    },
    CreatedUtc: '2024-10-21T06:12:51.477967Z',
    LastUpdateUtc: '2024-10-21T06:12:51.477967Z',
  },
};
export const nodeMockApiResponse = Object.values(nodeData);

export const routesData = {
  //get Routes
  [mockNodeGuid]: {
    Timestamp: {
      Start: '2024-10-23T05:17:19.540276Z',
      End: '2024-10-23T05:17:19.599676Z',
      TotalMs: 59.4,
      Messages: {},
    },
    Routes: [
      {
        TotalCost: 0,
        Edges: [
          {
            GUID: '01010101-0101-0101-0101-010101010101',
            GraphGUID: '01010101-0101-0101-0101-010101010101',
            Name: 'Default Edge1',
            From: '3366752f-e634-4de9-9280-d94ea667bf23',
            To: 'cf935603-616a-4495-b238-e8cad096c714',
            Cost: 0,
            CreatedUtc: '2024-10-21T11:56:14.485466Z',
            Data: {
              Hello: 'World3',
            },
          },
        ],
      },
      {
        TotalCost: 0,
        Edges: [
          {
            GUID: '02020202-0202-0202-0202-020202020202',
            GraphGUID: '02020202-0202-0202-0202-020202020202',
            Name: 'My test edge',
            From: '3366752f-e634-4de9-9280-d94ea667bf23',
            To: 'cf935603-616a-4495-b238-e8cad096c714',
            Cost: 0,
            CreatedUtc: '2024-10-21T11:51:16.673555Z',
            Data: {},
          },
        ],
      },
    ],
  },
};
export const routesMockApiResponse = Object.values(routesData);
