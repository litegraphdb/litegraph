export const mockGraphGuid = '01010101-0101-0101-0101-010101010101';
export const graphData = {
    [mockGraphGuid]: {
        id: 1,
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'Sample Node',
        Data: {
            Hello: "World"
        },
        CreatedUtc: '2024-10-19T14:35:20.351Z'
    },
    '02020202-0202-0202-0202-020202020202': {
        id: 2,
        GUID: '02020202-0202-0202-0202-020202020202',
        GraphGUID: '02020202-0202-0202-0202-020202020202',
        Name: 'Sample Node',
        Data: {
            Hello: "World 1"
        },
        CreatedUtc: '2024-10-19T14:35:20.351Z'
    },
};
export const graphMockApiResponse = Object.values(graphData);

export const searchGraphData = {
    [mockGraphGuid]: {
        Graphs: [
            {
                GUID: "01010101-0101-0101-0101-010101010101",
                Name: "Custom GUID",
                CreatedUtc: "2024-10-19T09:03:07.575617Z",
                Data: {
                    Hello: "World"
                }
            }
        ]
    }
};
export const graphMockSearchApiResponse = Object.values(searchGraphData);

export const graphGEXFData = {

    [mockGraphGuid]: `<?xml version="1.0" encoding="utf-8"?>
<gexf xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xsi:schemaLocation="http://www.gexf.net/1.3 http://www.gexf.net/1.3/gexf.xsd" version="1.3" xmlns="http://www.gexf.net/1.3">
    <meta lastmodifieddate="2024-10-29T08:00:48.6315024Z">
        <creator>LiteGraph</creator>
        <description>Graph from LiteGraph https://github.com/jchristn/litegraph</description>
    </meta> 
    </gxef>
    `
};

export const graphGEXFMockApiResponse = Object.values(graphGEXFData);

export const graphJsonlData = `# litegraph-jsonl v1.0
{"Type":"Graph","Object":{"GUID":"01010101-0101-0101-0101-010101010101","Name":"Sample Graph"}}
{"Type":"Node","Object":{"GUID":"11111111-1111-1111-1111-111111111111","GraphGUID":"01010101-0101-0101-0101-010101010101","Name":"Node A"}}
{"Type":"Node","Object":{"GUID":"22222222-2222-2222-2222-222222222222","GraphGUID":"01010101-0101-0101-0101-010101010101","Name":"Node B"}}
{"Type":"Edge","Object":{"GUID":"33333333-3333-3333-3333-333333333333","GraphGUID":"01010101-0101-0101-0101-010101010101","From":"11111111-1111-1111-1111-111111111111","To":"22222222-2222-2222-2222-222222222222"}}
`;

export const subgraphExtractionRequest = {
    TenantGUID: 'default',
    GraphGUID: mockGraphGuid,
    StartNodeGUIDs: ['11111111-1111-1111-1111-111111111111'],
    MaxDepth: 2,
    Direction: 'Both',
    IncludeData: true,
    IncludeSubordinates: false,
};

export const graphImportResult = {
    Success: true,
    TenantGUID: 'default',
    GraphGUID: mockGraphGuid,
    GraphsCreated: 1,
    NodesCreated: 2,
    NodesUpdated: 0,
    NodesSkipped: 0,
    EdgesCreated: 1,
    EdgesUpdated: 0,
    EdgesSkipped: 0,
    LinesRead: 4,
    LinesIgnored: 1,
    Warnings: [],
    GuidMap: {},
};