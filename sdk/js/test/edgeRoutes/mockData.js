export const mockGraphGuid = '01010101-0101-0101-0101-010101010101';
export const mockEdgeGuid = '01010101-0101-0101-0101-010101010101';
export const mockEdgeGuids = [
  '01010101-0101-0101-0101-010101010101',
  '02020202-0202-0202-0202-020202020202',
  '03030303-0303-0303-0303-030303030303',
];
export const mockEmptyEdgeGuids = [];
export const edgeData = {
  [mockEdgeGuid]: {
    id: 1,
    GUID: '01010101-0101-0101-0101-010101010101',
    GraphGUID: '01010101-0101-0101-0101-010101010101',
    Name: 'My test edge',
    From: '2b1520be-d285-4f22-8c74-f296047162b9',
    To: '784cfa37-fb06-4f81-b10d-f1167dfe2b22',
    Cost: 10,
    Data: {
      Hello: 'World',
    },
    CreatedUtc: '2024-07-01 15:43:06.991834',
    LastUpdateUtc: '2024-07-01 15:43:06.991834',
  },
  '02020202-0202-0202-0202-020202020202': {
    id: 2,
    GUID: '02020202-0202-0202-0202-020202020202',
    GraphGUID: '02020202-0202-0202-0202-020202020202',
    Name: 'My test edge',
    From: '2b1520be-d285-4f22-8c74-f296047162b9',
    To: '784cfa37-fb06-4f81-b10d-f1167dfe2b22',
    Cost: 10,
    Data: {
      Hello: 'World 1',
    },
    CreatedUtc: '2024-07-01 15:43:06.991834',
    LastUpdateUtc: '2024-07-01 15:43:06.991834',
  },
};
export const edgeMockApiResponse = Object.values(edgeData);

export const searchEdgeData = {
  [mockEdgeGuid]: {
    Edges: [
      {
        GUID: '3ae42458-22a1-42c5-a41a-97e90cb7d7db',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'My test edge12',
        From: 'd0a3c463-d1b3-42ce-9403-023c8aad216b',
        To: '597f2ebd-4cca-4b72-88b1-10781d4cbf0c',
        Cost: 0,
        CreatedUtc: '2024-10-28T07:33:14.382414Z',
        Data: {
          Hello: 'World',
        },
      },
    ],
  },
};
export const edgeMockSearchApiResponse = Object.values(searchEdgeData);
