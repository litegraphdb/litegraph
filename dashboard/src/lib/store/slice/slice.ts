import { BaseQueryFn, EndpointBuilder } from '@reduxjs/toolkit/query';
import sdkSlice, { SdkBaseQueryArgs } from '../rtk/rtkSdkInstance';
import { SliceTags } from './types';
import {
  Graph,
  EnumerateResponse,
  EnumerateRequest,
  Node,
  NodeCreateRequest,
  Edge,
  EdgeCreateRequest,
  TagMetaData,
  TagMetaDataCreateRequest,
  LabelMetadata,
  LabelMetadataCreateRequest,
  VectorCreateRequest,
  VectorMetadata,
  CredentialMetadataCreateRequest,
  CredentialMetadata,
  TenantMetaDataCreateRequest,
  UserMetadata,
  UserMetadataCreateRequest,
  TenantMetaData,
  GraphCreateRequest,
  EnumerateAndSearchRequest,
  BackupMetaData,
  BackupMetaDataCreateRequest,
  Token,
  IncludeDataAndSubordinates,
  NodeEdgeSearchRequest,
  SearchResult,
  VectorIndexEnableRequest,
  VectorIndexEnableResponse,
  ReadSubGraphResponse,
} from 'litegraphdb/dist/types/types';
import { sdk } from '@/lib/sdk/litegraph.service';

const enhancedSdk = sdkSlice.enhanceEndpoints({
  addTagTypes: [
    SliceTags.GRAPH,
    SliceTags.USER,
    SliceTags.TENANT,
    SliceTags.NODE,
    SliceTags.EDGE,
    SliceTags.TAG,
    SliceTags.LABEL,
    SliceTags.VECTOR,
    SliceTags.CREDENTIAL,
    SliceTags.BACKUP,
    SliceTags.RESET,
  ],
});

const graphSlice = enhancedSdk.injectEndpoints({
  endpoints: (
    build: EndpointBuilder<BaseQueryFn<SdkBaseQueryArgs, unknown, unknown>, SliceTags, 'sdk'>
  ) => ({
    //region Graph
    //enumerate all graphs
    searchAndEnumerateGraph: build.query<EnumerateResponse<Graph>, EnumerateAndSearchRequest>({
      query: (request: EnumerateAndSearchRequest) => ({
        callback: () => sdk.Graph.enumerateAndSearch(request),
      }),
      providesTags: [SliceTags.GRAPH],
    }),
    //get all
    getAllGraphs: build.query<Graph[], void>({
      query: () => ({
        callback: () => sdk.Graph.readAll(),
      }),
      providesTags: [SliceTags.GRAPH],
    }),
    getGraphById: build.query<Graph, { graphId: string; request?: IncludeDataAndSubordinates }>({
      query: ({ graphId, request }) => ({
        callback: () => sdk.Graph.read(graphId, request),
      }),
      providesTags: [SliceTags.GRAPH],
    }),
    getGraphGexfContent: build.query<string, string>({
      query: (graphId: string) => ({
        callback: () => sdk.Graph.exportGexf(graphId),
      }),
    }),

    getGraphGexfContentById: build.mutation<string, { graphId: string }>({
      query: ({ graphId }: { graphId: string }) => ({
        callback: () => sdk.Graph.exportGexf(graphId),
      }),
    }),
    //Create api key
    createGraph: build.mutation<Graph, GraphCreateRequest>({
      query: (request: GraphCreateRequest) => ({
        callback: () => sdk.Graph.create(request),
      }),
      invalidatesTags: [SliceTags.GRAPH],
    }),
    //Delete api key
    deleteGraph: build.mutation<boolean, string>({
      query: (request: string) => ({
        callback: () => sdk.Graph.delete(request),
      }),
      invalidatesTags: [SliceTags.GRAPH],
    }),
    //Update api key
    updateGraph: build.mutation<Graph, Graph>({
      query: (request: Graph) => ({
        callback: () => sdk.Graph.update(request),
      }),
      invalidatesTags: [SliceTags.GRAPH],
    }),
    enableVectorIndex: build.mutation<
      boolean,
      { graphId: string; request: VectorIndexEnableRequest }
    >({
      query: ({ graphId, request }: { graphId: string; request: VectorIndexEnableRequest }) => ({
        callback: () => sdk.Graph.enableVectorIndex(graphId, request),
      }),
      invalidatesTags: [SliceTags.GRAPH],
    }),
    readVectorIndexConfiguration: build.query<VectorIndexEnableResponse, string>({
      query: (graphId: string) => ({
        callback: () => sdk.Graph.readVectorIndexConfig(graphId),
      }),
      providesTags: [SliceTags.GRAPH],
    }),
    readVectorIndexStatistics: build.query<any, string>({
      query: (graphId: string) => ({
        callback: () => sdk.Graph.readVectorIndexStats(graphId),
      }),
      providesTags: [SliceTags.GRAPH],
    }),
    rebuildVectorIndex: build.mutation<boolean, string>({
      query: (graphId: string) => ({
        callback: () => sdk.Graph.rebuildVectorIndex(graphId),
      }),
      invalidatesTags: [SliceTags.GRAPH],
    }),
    deleteVectorIndex: build.mutation<boolean, string>({
      query: (graphId: string) => ({
        callback: () => sdk.Graph.deleteVectorIndex(graphId),
      }),
      invalidatesTags: [SliceTags.GRAPH],
    }),

    //endregion

    //region Node
    searchNodes: build.mutation<SearchResult, NodeEdgeSearchRequest>({
      query: (request: NodeEdgeSearchRequest) => ({
        callback: () => sdk.Node.search(request),
      }),
    }),
    //enumerate all nodes
    enumerateAndSearchNode: build.query<
      EnumerateResponse<Node>,
      { graphId: string; request: EnumerateAndSearchRequest }
    >({
      query: ({ graphId, request }: { graphId: string; request: EnumerateAndSearchRequest }) => ({
        callback: () => sdk.Node.enumerateAndSearch(graphId, request),
      }),
      providesTags: [SliceTags.NODE, SliceTags.RESET],
    }),
    //get all nodes
    getAllNodes: build.query<Node[], { graphId: string }>({
      query: ({ graphId }: { graphId: string }) => ({
        callback: () => sdk.Node.readAll(graphId),
      }),
      providesTags: [SliceTags.NODE],
    }),
    getManyNodes: build.query<Node[], { graphId: string; nodeIds: string[] }>({
      query: ({ graphId, nodeIds }: { graphId: string; nodeIds: string[] }) => ({
        callback: () => sdk.Node.readMany(graphId, nodeIds),
      }),
      providesTags: [SliceTags.NODE],
    }),
    getNodeById: build.query<
      Node,
      { graphId: string; nodeId: string; request?: IncludeDataAndSubordinates }
    >({
      query: ({ graphId, nodeId, request }) => ({
        callback: () => sdk.Node.read(graphId, nodeId, request),
      }),
      providesTags: [SliceTags.NODE],
    }),
    //create node
    createNode: build.mutation<Node, NodeCreateRequest>({
      query: (node: NodeCreateRequest) => ({
        callback: () => sdk.Node.create(node),
      }),
      // Do not invalidate to avoid batch re-fetch after create
      invalidatesTags: [SliceTags.NODE],
    }),
    //update node
    updateNode: build.mutation<Node, Node>({
      query: (node: Node) => ({
        callback: () => sdk.Node.update(node),
      }),
      // Do not invalidate to avoid batch re-fetch after update
      invalidatesTags: [SliceTags.NODE],
    }),
    //delete node
    deleteNode: build.mutation<boolean, { graphId: string; nodeId: string }>({
      query: ({ graphId, nodeId }: { graphId: string; nodeId: string }) => ({
        callback: () => sdk.Node.delete(graphId, nodeId),
      }),
      invalidatesTags: [SliceTags.NODE],
    }),
    //endregion

    //region Edge
    //search edges
    searchEdges: build.mutation<SearchResult, NodeEdgeSearchRequest>({
      query: (request: NodeEdgeSearchRequest) => ({
        callback: () => sdk.Edge.search(request),
      }),
    }),
    //enumerate all edges
    enumerateAndSearchEdge: build.query<
      EnumerateResponse<Edge>,
      { graphId: string; request: EnumerateAndSearchRequest }
    >({
      query: ({ graphId, request }: { graphId: string; request: EnumerateAndSearchRequest }) => ({
        callback: () => sdk.Edge.enumerateAndSearch(graphId, request),
      }),
      providesTags: [SliceTags.EDGE, SliceTags.RESET],
    }),
    //get all edges
    getAllEdges: build.query<Edge[], { graphId: string }>({
      query: ({ graphId }: { graphId: string }) => ({
        callback: () => sdk.Edge.readAll(graphId),
      }),
      providesTags: [SliceTags.EDGE],
      transformResponse: (response: any) => {
        return typeof response === 'object' ? response : [];
      },
    }),
    getManyEdges: build.query<Edge[], { graphId: string; edgeIds: string[] }>({
      query: ({ graphId, edgeIds }: { graphId: string; edgeIds: string[] }) => ({
        callback: () => sdk.Edge.readMany(graphId, edgeIds),
      }),
      providesTags: [SliceTags.EDGE],
    }),
    getEdgeById: build.query<
      Edge,
      { graphId: string; edgeId: string; request?: IncludeDataAndSubordinates }
    >({
      query: ({
        graphId,
        edgeId,
        request,
      }: {
        graphId: string;
        edgeId: string;
        request?: IncludeDataAndSubordinates;
      }) => ({
        callback: () => sdk.Edge.read(graphId, edgeId, request),
      }),
      providesTags: [SliceTags.EDGE],
    }),
    //create edge
    createEdge: build.mutation<Edge, EdgeCreateRequest>({
      query: (edge: EdgeCreateRequest) => ({
        callback: () => sdk.Edge.create(edge),
      }),
      // Do not invalidate to avoid batch re-fetch after create
      invalidatesTags: [SliceTags.EDGE],
    }),
    //update edge
    updateEdge: build.mutation<Edge, Edge>({
      query: (edge: Edge) => ({
        callback: () => sdk.Edge.update(edge),
      }),
      // Do not invalidate to avoid batch re-fetch after update
      invalidatesTags: [SliceTags.EDGE],
    }),
    //delete edge
    deleteEdge: build.mutation<boolean, { graphId: string; edgeId: string }>({
      query: ({ graphId, edgeId }: { graphId: string; edgeId: string }) => ({
        callback: () => sdk.Edge.delete(graphId, edgeId),
      }),
      invalidatesTags: [SliceTags.EDGE],
    }),
    //endregion

    //region Tag
    //enumerate all tags
    enumerateAndSearchTag: build.query<EnumerateResponse<TagMetaData>, EnumerateAndSearchRequest>({
      query: (request: EnumerateAndSearchRequest) => ({
        callback: () => sdk.Tag.enumerateAndSearch(request),
      }),
      providesTags: [SliceTags.TAG],
    }),
    //get all tags
    getAllTags: build.query<TagMetaData[], void>({
      query: () => ({
        callback: () => sdk.Tag.readAll(),
      }),
      providesTags: [SliceTags.TAG],
    }),
    getTagById: build.query<TagMetaData, string>({
      query: (tagId: string) => ({
        callback: () => sdk.Tag.read(tagId),
      }),
      providesTags: [SliceTags.TAG],
    }),
    //create tag
    createTag: build.mutation<TagMetaData, TagMetaDataCreateRequest>({
      query: (tag: TagMetaDataCreateRequest) => ({
        callback: () => sdk.Tag.create(tag),
      }),
      invalidatesTags: [SliceTags.TAG],
    }),
    //update tag
    updateTag: build.mutation<TagMetaData, TagMetaData>({
      query: (tag: TagMetaData) => ({
        callback: () => sdk.Tag.update(tag),
      }),
      invalidatesTags: [SliceTags.TAG],
    }),
    //delete tag
    deleteTag: build.mutation<boolean, string>({
      query: (tagId: string) => ({
        callback: () => sdk.Tag.delete(tagId),
      }),
      invalidatesTags: [SliceTags.TAG],
    }),
    //endregion

    //region Label
    //enumerate all labels
    enumerateAndSearchLabel: build.query<
      EnumerateResponse<LabelMetadata>,
      EnumerateAndSearchRequest
    >({
      query: (request: EnumerateAndSearchRequest) => ({
        callback: () => sdk.Label.enumerateAndSearch(request),
      }),
      providesTags: [SliceTags.LABEL],
    }),
    //get all labels
    getAllLabels: build.query<LabelMetadata[], void>({
      query: () => ({
        callback: () => sdk.Label.readAll(),
      }),
      providesTags: [SliceTags.LABEL],
    }),
    getLabelById: build.query<LabelMetadata, string>({
      query: (labelId: string) => ({
        callback: () => sdk.Label.read(labelId),
      }),
      providesTags: [SliceTags.LABEL],
    }),
    //create label
    createLabel: build.mutation<LabelMetadata, LabelMetadataCreateRequest>({
      query: (label: LabelMetadataCreateRequest) => ({
        callback: () => sdk.Label.create(label),
      }),
      invalidatesTags: [SliceTags.LABEL],
    }),
    //update label
    updateLabel: build.mutation<LabelMetadata, LabelMetadata>({
      query: (label: LabelMetadata) => ({
        callback: () => sdk.Label.update(label),
      }),
      invalidatesTags: [SliceTags.LABEL],
    }),
    //delete label
    deleteLabel: build.mutation<boolean, string>({
      query: (labelId: string) => ({
        callback: () => sdk.Label.delete(labelId),
      }),
      invalidatesTags: [SliceTags.LABEL],
    }),
    //endregion

    //region Vector
    //enumerate all vectors
    enumerateAndSearchVector: build.query<
      EnumerateResponse<VectorMetadata>,
      EnumerateAndSearchRequest
    >({
      query: (request: EnumerateAndSearchRequest) => ({
        callback: () => sdk.Vector.enumerateAndSearch(request),
      }),
      providesTags: [SliceTags.VECTOR],
    }),
    //get all vectors
    getAllVectors: build.query<VectorMetadata[], void>({
      query: () => ({
        callback: () => sdk.Vector.readAll(),
      }),
      providesTags: [SliceTags.VECTOR],
    }),
    getVectorById: build.query<VectorMetadata, string>({
      query: (vectorId: string) => ({
        callback: () => sdk.Vector.read(vectorId),
      }),
      providesTags: [SliceTags.VECTOR],
    }),
    //create vector
    createVector: build.mutation<VectorMetadata, VectorCreateRequest>({
      query: (vector: VectorCreateRequest) => ({
        callback: () => sdk.Vector.create(vector),
      }),
      invalidatesTags: [SliceTags.VECTOR],
    }),
    //update vector
    updateVector: build.mutation<VectorMetadata, VectorMetadata>({
      query: (vector: VectorMetadata) => ({
        callback: () => sdk.Vector.update(vector),
      }),
      invalidatesTags: [SliceTags.VECTOR],
    }),
    //delete vector
    deleteVector: build.mutation<boolean, string>({
      query: (vectorId: string) => ({
        callback: () => sdk.Vector.delete(vectorId),
      }),
      invalidatesTags: [SliceTags.VECTOR],
    }),
    //endregion

    //region Credential
    //enumerate all credentials
    enumerateCredential: build.query<EnumerateResponse<CredentialMetadata>, EnumerateRequest>({
      query: (request: EnumerateRequest) => ({
        callback: () => sdk.Credential.enumerate(request),
      }),
      providesTags: [SliceTags.CREDENTIAL],
    }),
    //get all credentials
    getAllCredentials: build.query<CredentialMetadata[], void>({
      query: () => ({
        callback: () => sdk.Credential.readAll(),
      }),
      providesTags: [SliceTags.CREDENTIAL],
    }),
    getCredentialById: build.query<Credential, string>({
      query: (credentialId: string) => ({
        callback: () => sdk.Credential.read(credentialId),
      }),
      providesTags: [SliceTags.CREDENTIAL],
    }),
    //create credential
    createCredential: build.mutation<Credential, CredentialMetadataCreateRequest>({
      query: (credential: CredentialMetadataCreateRequest) => ({
        callback: () => sdk.Credential.create(credential),
      }),
      invalidatesTags: [SliceTags.CREDENTIAL],
    }),
    //update credential
    updateCredential: build.mutation<CredentialMetadata, CredentialMetadata>({
      query: (credential: CredentialMetadata) => ({
        callback: () => sdk.Credential.update(credential),
      }),
      invalidatesTags: [SliceTags.CREDENTIAL],
    }),
    //delete credential
    deleteCredential: build.mutation<boolean, string>({
      query: (credentialId: string) => ({
        callback: () => sdk.Credential.delete(credentialId),
      }),
      invalidatesTags: [SliceTags.CREDENTIAL],
    }),
    //endregion

    //region Tenant
    //enumerate all tenants
    enumerateTenant: build.query<EnumerateResponse<TenantMetaData>, EnumerateRequest>({
      query: (request: EnumerateRequest) => ({
        callback: () => sdk.Tenant.enumerate(request),
      }),
      providesTags: [SliceTags.TENANT],
    }),
    //get all tenants
    getAllTenants: build.query<TenantMetaData[], void>({
      query: () => ({
        callback: () => sdk.Tenant.readAll(),
      }),
      providesTags: [SliceTags.TENANT],
    }),
    getTenantById: build.query<TenantMetaData, string>({
      query: (tenantId: string) => ({
        callback: () => sdk.Tenant.read(tenantId),
      }),
      providesTags: [SliceTags.TENANT],
    }),
    //create tenant
    createTenant: build.mutation<TenantMetaData, TenantMetaDataCreateRequest>({
      query: (tenant: TenantMetaDataCreateRequest) => ({
        callback: () => sdk.Tenant.create(tenant),
      }),
      invalidatesTags: [SliceTags.TENANT],
    }),
    //update tenant
    updateTenant: build.mutation<TenantMetaData, TenantMetaData>({
      query: (tenant: TenantMetaData) => ({
        callback: () => sdk.Tenant.update(tenant),
      }),
      invalidatesTags: [SliceTags.TENANT],
    }),
    //delete tenant
    deleteTenant: build.mutation<boolean, string>({
      query: (tenantId: string) => ({
        callback: () => sdk.Tenant.delete(tenantId),
      }),
      invalidatesTags: [SliceTags.TENANT],
    }),
    //endregion

    //region User
    //enumerate all users
    enumerateUser: build.query<EnumerateResponse<UserMetadata>, EnumerateRequest>({
      query: (request: EnumerateRequest) => ({
        callback: () => sdk.User.enumerate(request),
      }),
      providesTags: [SliceTags.USER],
    }),
    //get all users
    getAllUsers: build.query<UserMetadata[], void>({
      query: () => ({
        callback: () => sdk.User.readAll(),
      }),
      providesTags: [SliceTags.USER],
    }),
    getUserById: build.query<UserMetadata, string>({
      query: (userId: string) => ({
        callback: () => sdk.User.read(userId),
      }),
      providesTags: [SliceTags.USER],
    }),
    //create user
    createUser: build.mutation<UserMetadata, UserMetadataCreateRequest>({
      query: (user: UserMetadataCreateRequest) => ({
        callback: () => sdk.User.create(user),
      }),
      invalidatesTags: [SliceTags.USER],
    }),
    //update user
    updateUser: build.mutation<UserMetadata, UserMetadata>({
      query: (user: UserMetadata) => ({
        callback: () => sdk.User.update(user),
      }),
      invalidatesTags: [SliceTags.USER],
    }),
    //delete user
    deleteUser: build.mutation<boolean, string>({
      query: (userId: string) => ({
        callback: () => sdk.User.delete(userId),
      }),
      invalidatesTags: [SliceTags.USER],
    }),
    //endregion
    //region Backup
    //read all backups
    readAllBackups: build.query<BackupMetaData[], void>({
      query: () => ({
        callback: () => sdk.Backup.readAll(),
      }),
      providesTags: [SliceTags.BACKUP],
    }),
    //read backup
    readBackup: build.mutation<BackupMetaData, string>({
      query: (fileName: string) => ({
        callback: () => sdk.Backup.read(fileName),
      }),
    }),
    //create backup
    createBackup: build.mutation<BackupMetaData, BackupMetaDataCreateRequest>({
      query: (backup: BackupMetaDataCreateRequest) => ({
        callback: () => sdk.Backup.create(backup),
      }),
      invalidatesTags: [SliceTags.BACKUP],
    }),
    //delete backup
    deleteBackup: build.mutation<boolean, string>({
      query: (fileName: string) => ({
        callback: () => sdk.Backup.delete(fileName),
      }),
      invalidatesTags: [SliceTags.BACKUP],
    }),

    //endregion
    //region Authentication
    getTenantsForEmail: build.mutation<TenantMetaData[], string>({
      query: (email: string) => ({
        callback: () => sdk.Authentication.getTenantsForEmail(email),
      }),
    }),
    generateToken: build.mutation<Token, { email: string; password: string; tenantId: string }>({
      query: ({
        email,
        password,
        tenantId,
      }: {
        email: string;
        password: string;
        tenantId: string;
      }) => ({
        callback: () => sdk.Authentication.generateToken(email, password, tenantId),
      }),
    }),
    getTokenDetails: build.mutation<Token, string>({
      query: (token: string) => ({
        callback: () => sdk.Authentication.getTokenDetails(token),
      }),
    }),
    getSubGraphs: build.mutation<
      ReadSubGraphResponse,
      {
        graphGuid: string;
        nodeGuid: string;
        options?: {
          maxDepth?: number;
          maxNodes?: number;
          maxEdges?: number;
          incldata?: boolean;
          inclsub?: boolean;
        };
      }
    >({
      query: ({ graphGuid, nodeGuid, options }) => ({
        callback: () => sdk.Graph.readSubGraph(graphGuid, nodeGuid, options),
      }),
    }),
  }),
});

export const {
  useSearchAndEnumerateGraphQuery,
  useGetGraphByIdQuery,
  useGetGraphGexfContentQuery,
  useCreateGraphMutation,
  useDeleteGraphMutation,
  useGetAllGraphsQuery,
  useUpdateGraphMutation,
  useEnumerateAndSearchNodeQuery,
  useGetNodeByIdQuery,
  useCreateNodeMutation,
  useDeleteNodeMutation,
  useGetAllNodesQuery,
  useUpdateNodeMutation,
  useEnumerateAndSearchEdgeQuery,
  useCreateEdgeMutation,
  useDeleteEdgeMutation,
  useGetAllEdgesQuery,
  useUpdateEdgeMutation,
  useEnumerateAndSearchTagQuery,
  useCreateTagMutation,
  useDeleteTagMutation,
  useGetAllTagsQuery,
  useUpdateTagMutation,
  useEnumerateAndSearchLabelQuery,
  useCreateLabelMutation,
  useDeleteLabelMutation,
  useGetAllLabelsQuery,
  useUpdateLabelMutation,
  useEnumerateAndSearchVectorQuery,
  useCreateVectorMutation,
  useDeleteVectorMutation,
  useGetAllVectorsQuery,
  useUpdateVectorMutation,
  useEnumerateCredentialQuery,
  useCreateCredentialMutation,
  useDeleteCredentialMutation,
  useGetAllCredentialsQuery,
  useUpdateCredentialMutation,
  useEnumerateTenantQuery,
  useCreateTenantMutation,
  useDeleteTenantMutation,
  useGetAllTenantsQuery,
  useUpdateTenantMutation,
  useEnumerateUserQuery,
  useCreateUserMutation,
  useDeleteUserMutation,
  useGetAllUsersQuery,
  useUpdateUserMutation,
  useGetTagByIdQuery,
  useGetLabelByIdQuery,
  useGetVectorByIdQuery,
  useGetCredentialByIdQuery,
  useGetTenantByIdQuery,
  useGetUserByIdQuery,
  useGetEdgeByIdQuery,
  useGetGraphGexfContentByIdMutation,
  useReadAllBackupsQuery,
  useReadBackupMutation,
  useCreateBackupMutation,
  useDeleteBackupMutation,
  useGetTenantsForEmailMutation,
  useGenerateTokenMutation,
  useGetTokenDetailsMutation,
  useGetManyNodesQuery,
  useGetManyEdgesQuery,
  useSearchNodesMutation,
  useSearchEdgesMutation,
  useEnableVectorIndexMutation,
  useReadVectorIndexConfigurationQuery,
  useReadVectorIndexStatisticsQuery,
  useRebuildVectorIndexMutation,
  useDeleteVectorIndexMutation,
  useGetSubGraphsMutation,
} = graphSlice;
