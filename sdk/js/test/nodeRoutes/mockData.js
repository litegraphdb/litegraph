export const mockGraphGuid = '01010101-0101-0101-0101-010101010101';
export const mockNodeGuid = '01010101-0101-0101-0101-010101010101';
export const mockNodeGuids = [
  '01010101-0101-0101-0101-010101010101',
  '02020202-0202-0202-0202-020202020202',
  '03030303-0303-0303-0303-030303030303',
];
export const mockEmptyNodeGuids = [];

export const nodeData = {
  [mockNodeGuid]: {
    id: 1,
    GUID: '01010101-0101-0101-0101-010101010101',
    GraphGUID: '01010101-0101-0101-0101-010101010101',
    Name: 'Custom GUID',
    Data: {
      Hello: 'World',
    },
    CreatedUtc: '2024-10-19T14:35:20.351Z',
    LastUpdateUtc: '2024-10-19T14:35:20.351Z',
  },
  '02020202-0202-0202-0202-020202020202': {
    id: 2,
    GUID: '02020202-0202-0202-0202-020202020202',
    GraphGUID: '02020202-0202-0202-0202-020202020202',
    Name: 'Custom GUID',
    Data: {
      Hello: 'World 1',
    },
    CreatedUtc: '2024-10-19T14:35:20.351Z',
    LastUpdateUtc: '2024-10-19T14:35:20.351Z',
  },
};
export const nodeMockApiResponse = Object.values(nodeData);

export const searchNodeData = {
  [mockNodeGuid]: {
    Nodes: [
      {
        GUID: '597f2ebd-4cca-4b72-88b1-10781d4cbf0c',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'My updated test node',
        Data: {
          Hello: 'World',
        },
        CreatedUtc: '2024-10-24T11:32:54.869919Z',
      },
    ],
  },
};
export const nodeMockSearchApiResponse = Object.values(searchNodeData);
