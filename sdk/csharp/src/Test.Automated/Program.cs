namespace Test.Automated
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;
	using LiteGraph.Sdk;

	internal static class Program
	{
		#region Private-Members

		private const string EndpointEnvVar = "LITEGRAPH_ENDPOINT";
		private const string TokenEnvVar = "LITEGRAPH_BEARER_TOKEN";
		private const string VerboseEnvVar = "LITEGRAPH_TEST_VERBOSE";

		private static LiteGraphSdk? _Sdk = null;
		private static Guid _TenantGuid = Guid.Empty;
		private static Guid _GraphGuid = Guid.Empty;
		private static Guid _UserGuid = Guid.Empty;
		private static Guid _CredentialGuid = Guid.Empty;
		private static Guid _SubgraphRootNodeGuid = Guid.Empty;
		private static string _TenantName = string.Empty;
		private static string _GraphName = string.Empty;
		private static string _UserEmail = string.Empty;
		private static string _CredentialName = string.Empty;
		private static string _CredentialBearerToken = string.Empty;
		private static Guid _EdgeGuidPrimary = Guid.Empty;
		private static Guid _EdgeGuidSecondary = Guid.Empty;
		private static Guid _EdgeNode1Guid = Guid.Empty;
		private static Guid _EdgeNode2Guid = Guid.Empty;
		private static Guid _EdgeNode3Guid = Guid.Empty;
		private static string _EdgeNamePrimary = string.Empty;
		private static string _EdgeNameSecondary = string.Empty;
		private static Guid _NodePrimaryGuid = Guid.Empty;
		private static Guid _NodeSecondaryGuid = Guid.Empty;
		private static Guid _NodeTertiaryGuid = Guid.Empty;
		private static string _NodePrimaryName = string.Empty;
		private static string _NodeSecondaryName = string.Empty;
		private static string _NodeTertiaryName = string.Empty;
		private static bool _NodeRelationshipsPrepared = false;
		private static Guid _LabelNodePrimaryGuid = Guid.Empty;
		private static Guid _LabelEdgePrimaryGuid = Guid.Empty;
		private static Guid _LabelGraphPrimaryGuid = Guid.Empty;
		private static string _LabelNodePrimaryName = string.Empty;
		private static string _LabelEdgePrimaryName = string.Empty;
		private static string _LabelGraphPrimaryName = string.Empty;
		private static Guid _TagNodePrimaryGuid = Guid.Empty;
		private static Guid _TagEdgePrimaryGuid = Guid.Empty;
		private static Guid _TagGraphPrimaryGuid = Guid.Empty;
		private static string _TagNodePrimaryKey = string.Empty;
		private static string _TagNodePrimaryValue = string.Empty;
		private static string _TagEdgePrimaryKey = string.Empty;
		private static string _TagEdgePrimaryValue = string.Empty;
		private static string _TagGraphPrimaryKey = string.Empty;
		private static string _TagGraphPrimaryValue = string.Empty;
		private static string _UserPassword = string.Empty;
		private static string _UserAuthToken = string.Empty;
		private static Guid _VectorNodePrimaryGuid = Guid.Empty;
		private static Guid _VectorNodeSecondaryGuid = Guid.Empty;
		private static Guid _VectorEdgePrimaryGuid = Guid.Empty;
		private static string _VectorNodePrimaryContent = string.Empty;
		private static string _VectorNodeSecondaryContent = string.Empty;
		private static string _VectorEdgePrimaryContent = string.Empty;
		private static string? _BackupFilename = null;
		private static bool _SubgraphPrepared = false;
		private static readonly List<Guid> _SubgraphNodeGuids = new List<Guid>();
		private static readonly List<SubgraphEdgeInfo> _SubgraphEdgeInfos = new List<SubgraphEdgeInfo>();
		private static readonly HashSet<Guid> _CreatedTenantGuids = new HashSet<Guid>();
		private static readonly List<TestResult> _TestResults = new List<TestResult>();

		private static string _Endpoint = Environment.GetEnvironmentVariable(EndpointEnvVar) ?? "http://localhost:8701";
		private static string _BearerToken = Environment.GetEnvironmentVariable(TokenEnvVar) ?? "litegraphadmin";
		private static bool _VerboseLogging = string.Equals(
			Environment.GetEnvironmentVariable(VerboseEnvVar),
			"true",
			StringComparison.OrdinalIgnoreCase);

		#endregion

		#region Delete-Tests

		private static async Task TestVectorDeleteMethods()
		{
			LiteGraphSdk sdk = RequireSdk();
			await EnsureVectorDependenciesAsync().ConfigureAwait(false);

			async Task<VectorMetadata> CreateVectorAsync(Guid? nodeGuid, Guid? edgeGuid, string contentSuffix)
			{
				VectorMetadata vector = BuildVectorMetadata(nodeGuid, edgeGuid, contentSuffix, new List<float> { 0.9f, 0.8f, 0.7f });
				VectorMetadata? created = await sdk.Vector.Create(vector).ConfigureAwait(false);
				AssertNotNull(created, "Vector create for delete methods");
				return created!;
			}

			// DeleteByGuid
			VectorMetadata single = await CreateVectorAsync(_NodePrimaryGuid, null, "vector-delete-single").ConfigureAwait(false);
			await sdk.Vector.DeleteByGuid(_TenantGuid, single.GUID).ConfigureAwait(false);
			bool exists = await sdk.Vector.ExistsByGuid(_TenantGuid, single.GUID).ConfigureAwait(false);
			AssertFalse(exists, "Vector deleted via DeleteByGuid");

			// DeleteMany
			List<VectorMetadata> many = new List<VectorMetadata>
			{
				BuildVectorMetadata(_NodeSecondaryGuid, null, "vector-delete-many-node", new List<float> { 0.1f, 0.4f, 0.7f }),
				BuildVectorMetadata(null, _EdgeGuidPrimary, "vector-delete-many-edge", new List<float> { 0.7f, 0.4f, 0.1f })
			};

			List<VectorMetadata>? createdMany = await sdk.Vector.CreateMany(_TenantGuid, many).ConfigureAwait(false);
			AssertNotNull(createdMany, "Vector create many for delete many");
			await sdk.Vector.DeleteMany(_TenantGuid, createdMany!.Select(v => v.GUID).ToList()).ConfigureAwait(false);
			foreach (VectorMetadata vector in createdMany)
			{
				bool vectorExists = await sdk.Vector.ExistsByGuid(_TenantGuid, vector.GUID).ConfigureAwait(false);
				AssertFalse(vectorExists, "Vector removed via DeleteMany");
			}

			// DeleteNodeVectors
			await CreateVectorAsync(_EdgeNode1Guid, null, "vector-node-delete").ConfigureAwait(false);
			await sdk.Vector.DeleteNodeVectors(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);
			List<VectorMetadata>? nodeVectors = await sdk.Vector.ReadManyNode(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);
			AssertTrue(nodeVectors == null || nodeVectors.Count == 0, "Node vectors deleted");

			// DeleteEdgeVectors
			await CreateVectorAsync(null, _EdgeGuidPrimary, "vector-edge-delete").ConfigureAwait(false);
			await sdk.Vector.DeleteEdgeVectors(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);
			List<VectorMetadata>? edgeVectors = await sdk.Vector.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);
			AssertTrue(edgeVectors == null || edgeVectors.Count == 0, "Edge vectors deleted");

			// DeleteGraphVectors
			VectorMetadata graphVector = BuildVectorMetadata(null, null, "vector-graph-delete", new List<float> { 0.5f, 0.4f, 0.3f });
			VectorMetadata? createdGraphVector = await sdk.Vector.Create(graphVector).ConfigureAwait(false);
			AssertNotNull(createdGraphVector, "Graph-level vector for DeleteGraphVectors");

			await sdk.Vector.DeleteGraphVectors(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			List<VectorMetadata>? graphVectors = await sdk.Vector.ReadManyGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(
				graphVectors == null ||
				graphVectors.All(v => v.NodeGUID.HasValue || v.EdgeGUID.HasValue),
				"Graph-level vectors deleted via DeleteGraphVectors");

			// DeleteAllInGraph
			await CreateVectorAsync(_NodePrimaryGuid, null, "vector-all-graph").ConfigureAwait(false);
			await sdk.Vector.DeleteAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			List<VectorMetadata>? allGraphVectors = await sdk.Vector.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(allGraphVectors == null || allGraphVectors.Count == 0, "Vectors deleted via DeleteAllInGraph");

			// DeleteAllInTenant
			await CreateVectorAsync(_NodePrimaryGuid, null, "vector-all-tenant").ConfigureAwait(false);
			await sdk.Vector.DeleteAllInTenant(_TenantGuid).ConfigureAwait(false);
			List<VectorMetadata>? tenantVectors = await sdk.Vector.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);
			AssertTrue(tenantVectors == null || tenantVectors.Count == 0, "Vectors deleted via DeleteAllInTenant");

			_VectorNodePrimaryGuid = Guid.Empty;
			_VectorNodeSecondaryGuid = Guid.Empty;
			_VectorEdgePrimaryGuid = Guid.Empty;
			_VectorNodePrimaryContent = string.Empty;
			_VectorNodeSecondaryContent = string.Empty;
			_VectorEdgePrimaryContent = string.Empty;
		}

		private static async Task TestTagDeleteMethods()
		{
			LiteGraphSdk sdk = RequireSdk();
			await EnsureTagTestDataAsync().ConfigureAwait(false);

			async Task<TagMetadata> CreateTagAsync(Guid? nodeGuid, Guid? edgeGuid, string keyPrefix)
			{
				TagMetadata tag = new TagMetadata
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					NodeGUID = nodeGuid,
					EdgeGUID = edgeGuid,
					Key = $"{keyPrefix}-{UniqueName("tag-chave")}",
					Value = UniqueName("tag-value")
				};

				TagMetadata? created = await sdk.Tag.Create(tag).ConfigureAwait(false);
				AssertNotNull(created, "Tag create for delete methods");
				return created!;
			}

			// DeleteByGuid
			TagMetadata single = await CreateTagAsync(_EdgeNode1Guid, null, "delete-node-tag").ConfigureAwait(false);
			await sdk.Tag.DeleteByGuid(_TenantGuid, single.GUID).ConfigureAwait(false);
			bool exists = await sdk.Tag.ExistsByGuid(_TenantGuid, single.GUID).ConfigureAwait(false);
			AssertFalse(exists, "Tag deleted via DeleteByGuid");

			// DeleteMany
			TagMetadata tag1 = await CreateTagAsync(_EdgeNode1Guid, null, "delete-many-node").ConfigureAwait(false);
			TagMetadata tag2 = await CreateTagAsync(null, _EdgeGuidPrimary, "delete-many-edge").ConfigureAwait(false);
			await sdk.Tag.DeleteMany(_TenantGuid, new List<Guid> { tag1.GUID, tag2.GUID }).ConfigureAwait(false);
			AssertFalse(await sdk.Tag.ExistsByGuid(_TenantGuid, tag1.GUID).ConfigureAwait(false), "Node tag removed via DeleteMany");
			AssertFalse(await sdk.Tag.ExistsByGuid(_TenantGuid, tag2.GUID).ConfigureAwait(false), "Edge tag removed via DeleteMany");

			// DeleteNodeTags
			await CreateTagAsync(_EdgeNode1Guid, null, "delete-node-tags").ConfigureAwait(false);
			await sdk.Tag.DeleteNodeTags(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);
			List<TagMetadata>? nodeTags = await sdk.Tag.ReadManyNode(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);
			AssertTrue(nodeTags == null || nodeTags.Count == 0, "Node tags deleted");

			// DeleteEdgeTags
			await CreateTagAsync(null, _EdgeGuidPrimary, "delete-edge-tags").ConfigureAwait(false);
			await sdk.Tag.DeleteEdgeTags(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);
			List<TagMetadata>? edgeTags = await sdk.Tag.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);
			AssertTrue(edgeTags == null || edgeTags.Count == 0, "Edge tags deleted");

			// DeleteGraphTags
			await CreateTagAsync(null, null, "delete-graph-tags").ConfigureAwait(false);
			await sdk.Tag.DeleteGraphTags(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			List<TagMetadata>? graphTags = await sdk.Tag.ReadManyGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(graphTags == null || graphTags.Count == 0, "Graph tags deleted via DeleteGraphTags");

			// DeleteAllInGraph
			await CreateTagAsync(_EdgeNode1Guid, null, "delete-all-graph").ConfigureAwait(false);
			await sdk.Tag.DeleteAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			List<TagMetadata>? tagsInGraph = await sdk.Tag.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(tagsInGraph == null || tagsInGraph.Count == 0, "Graph tags deleted via DeleteAllInGraph");

			// DeleteAllInTenant
			await CreateTagAsync(_EdgeNode1Guid, null, "delete-all-tenant").ConfigureAwait(false);
			await sdk.Tag.DeleteAllInTenant(_TenantGuid).ConfigureAwait(false);
			List<TagMetadata>? tagsInTenant = await sdk.Tag.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);
			AssertTrue(tagsInTenant == null || tagsInTenant.Count == 0, "Tenant tags deleted via DeleteAllInTenant");

			_TagNodePrimaryGuid = Guid.Empty;
			_TagEdgePrimaryGuid = Guid.Empty;
			_TagGraphPrimaryGuid = Guid.Empty;
			_TagNodePrimaryKey = string.Empty;
			_TagNodePrimaryValue = string.Empty;
			_TagEdgePrimaryKey = string.Empty;
			_TagEdgePrimaryValue = string.Empty;
			_TagGraphPrimaryKey = string.Empty;
			_TagGraphPrimaryValue = string.Empty;
		}

		private static async Task TestLabelDeleteMethods()
		{
			LiteGraphSdk sdk = RequireSdk();
			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);
			await EnsureEdgePrimaryAsync().ConfigureAwait(false);

			async Task<LabelMetadata> CreateLabelAsync(Guid? nodeGuid, Guid? edgeGuid, string value)
			{
				LabelMetadata label = new LabelMetadata
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					NodeGUID = nodeGuid,
					EdgeGUID = edgeGuid,
					Label = value
				};

				LabelMetadata? created = await sdk.Label.Create(label).ConfigureAwait(false);
				AssertNotNull(created, "Label create for delete methods");
				return created!;
			}

			// DeleteByGuid
			LabelMetadata single = await CreateLabelAsync(_EdgeNode1Guid, null, "label-delete-single").ConfigureAwait(false);
			await sdk.Label.DeleteByGuid(_TenantGuid, single.GUID).ConfigureAwait(false);
			LabelMetadata? readLabel = await sdk.Label.ReadByGuid(_TenantGuid, single.GUID).ConfigureAwait(false);
			AssertNull(readLabel, "Label deleted via DeleteByGuid");

			// DeleteMany
			LabelMetadata label1 = await CreateLabelAsync(_EdgeNode1Guid, null, "label-delete-many-node").ConfigureAwait(false);
			LabelMetadata label2 = await CreateLabelAsync(null, _EdgeGuidPrimary, "label-delete-many-edge").ConfigureAwait(false);
			await sdk.Label.DeleteMany(_TenantGuid, new List<Guid> { label1.GUID, label2.GUID }).ConfigureAwait(false);
			AssertNull(await sdk.Label.ReadByGuid(_TenantGuid, label1.GUID).ConfigureAwait(false), "Node label removed via DeleteMany");
			AssertNull(await sdk.Label.ReadByGuid(_TenantGuid, label2.GUID).ConfigureAwait(false), "Edge label removed via DeleteMany");

			// DeleteNodeLabels
			await CreateLabelAsync(_EdgeNode1Guid, null, "label-node-delete").ConfigureAwait(false);
			await sdk.Label.DeleteNodeLabels(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);
			List<LabelMetadata>? nodeLabels = await sdk.Label.ReadManyNode(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);
			AssertTrue(nodeLabels == null || nodeLabels.Count == 0, "Node labels deleted");

			// DeleteEdgeLabels
			await CreateLabelAsync(null, _EdgeGuidPrimary, "label-edge-delete").ConfigureAwait(false);
			await sdk.Label.DeleteEdgeLabels(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);
			List<LabelMetadata>? edgeLabels = await sdk.Label.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);
			AssertTrue(edgeLabels == null || edgeLabels.Count == 0, "Edge labels deleted");

			// DeleteGraphLabels
			await CreateLabelAsync(null, null, "label-graph-delete").ConfigureAwait(false);
			await sdk.Label.DeleteGraphLabels(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			List<LabelMetadata>? graphLabels = await sdk.Label.ReadManyGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(graphLabels == null || graphLabels.Count == 0, "Graph labels deleted via DeleteGraphLabels");

			// DeleteAllInGraph
			await CreateLabelAsync(_EdgeNode1Guid, null, "label-all-graph").ConfigureAwait(false);
			await sdk.Label.DeleteAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			List<LabelMetadata>? labelsInGraph = await sdk.Label.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(labelsInGraph == null || labelsInGraph.Count == 0, "Labels deleted via DeleteAllInGraph");

			// DeleteAllInTenant
			await CreateLabelAsync(_EdgeNode1Guid, null, "label-all-tenant").ConfigureAwait(false);
			await sdk.Label.DeleteAllInTenant(_TenantGuid).ConfigureAwait(false);
			List<LabelMetadata>? labelsInTenant = await sdk.Label.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);
			AssertTrue(labelsInTenant == null || labelsInTenant.Count == 0, "Labels deleted via DeleteAllInTenant");

			_LabelNodePrimaryGuid = Guid.Empty;
			_LabelEdgePrimaryGuid = Guid.Empty;
			_LabelGraphPrimaryGuid = Guid.Empty;
			_LabelNodePrimaryName = string.Empty;
			_LabelEdgePrimaryName = string.Empty;
			_LabelGraphPrimaryName = string.Empty;
		}

		private static async Task TestEdgeDeleteMethods()
		{
			LiteGraphSdk sdk = RequireSdk();
			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

			async Task<Edge> CreateEdgeAsync(Guid from, Guid to, string namePrefix)
			{
				Edge edge = new Edge
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					From = from,
					To = to,
					Name = $"{namePrefix}-{UniqueName("edge-delete")}",
					Cost = 1
				};

				Edge? created = await sdk.Edge.Create(edge).ConfigureAwait(false);
				AssertNotNull(created, "Edge create for delete methods");
				return created!;
			}

			// DeleteByGuid
			Edge single = await CreateEdgeAsync(_EdgeNode1Guid, _EdgeNode2Guid, "edge-delete-single").ConfigureAwait(false);
			await sdk.Edge.DeleteByGuid(_TenantGuid, _GraphGuid, single.GUID).ConfigureAwait(false);
			bool exists = await sdk.Edge.ExistsByGuid(_TenantGuid, _GraphGuid, single.GUID).ConfigureAwait(false);
			AssertFalse(exists, "Edge deleted via DeleteByGuid");

			// DeleteMany
			Edge edge1 = await CreateEdgeAsync(_EdgeNode2Guid, _EdgeNode3Guid, "edge-delete-many-1").ConfigureAwait(false);
			Edge edge2 = await CreateEdgeAsync(_EdgeNode3Guid, _EdgeNode1Guid, "edge-delete-many-2").ConfigureAwait(false);
			await sdk.Edge.DeleteMany(_TenantGuid, _GraphGuid, new List<Guid> { edge1.GUID, edge2.GUID }).ConfigureAwait(false);
			AssertFalse(await sdk.Edge.ExistsByGuid(_TenantGuid, _GraphGuid, edge1.GUID).ConfigureAwait(false), "Edge 1 removed via DeleteMany");
			AssertFalse(await sdk.Edge.ExistsByGuid(_TenantGuid, _GraphGuid, edge2.GUID).ConfigureAwait(false), "Edge 2 removed via DeleteMany");

			// DeleteNodeEdges
			await CreateEdgeAsync(_EdgeNode1Guid, _EdgeNode2Guid, "edge-node-delete").ConfigureAwait(false);
			await sdk.Edge.DeleteNodeEdges(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);
			List<Edge>? nodeEdges = await sdk.Edge.ReadEdgesFromNode(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);
			AssertTrue(nodeEdges == null || nodeEdges.Count == 0, "Node edges deleted");

			// DeleteNodeEdgesMany
			await CreateEdgeAsync(_EdgeNode2Guid, _EdgeNode3Guid, "edge-node-many-1").ConfigureAwait(false);
			await CreateEdgeAsync(_EdgeNode3Guid, _EdgeNode1Guid, "edge-node-many-2").ConfigureAwait(false);
			await sdk.Edge.DeleteNodeEdgesMany(_TenantGuid, _GraphGuid, new List<Guid> { _EdgeNode2Guid, _EdgeNode3Guid }).ConfigureAwait(false);
			List<Edge>? fromNode2 = await sdk.Edge.ReadEdgesFromNode(_TenantGuid, _GraphGuid, _EdgeNode2Guid).ConfigureAwait(false);
			List<Edge>? fromNode3 = await sdk.Edge.ReadEdgesFromNode(_TenantGuid, _GraphGuid, _EdgeNode3Guid).ConfigureAwait(false);
			AssertTrue((fromNode2 == null || fromNode2.Count == 0) && (fromNode3 == null || fromNode3.Count == 0), "Node edges deleted via DeleteNodeEdgesMany");

			// DeleteAllInGraph
			await CreateEdgeAsync(_EdgeNode1Guid, _EdgeNode2Guid, "edge-all-graph").ConfigureAwait(false);
			await sdk.Edge.DeleteAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			List<Edge>? graphEdges = await sdk.Edge.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(graphEdges == null || graphEdges.Count == 0, "Edges deleted via DeleteAllInGraph");

			// DeleteAllInTenant
			await CreateEdgeAsync(_EdgeNode1Guid, _EdgeNode2Guid, "edge-all-tenant").ConfigureAwait(false);
			await sdk.Edge.DeleteAllInTenant(_TenantGuid).ConfigureAwait(false);
			List<Edge>? tenantEdges = await sdk.Edge.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);
			AssertTrue(tenantEdges == null || tenantEdges.Count == 0, "Edges deleted via DeleteAllInTenant");

			_EdgeGuidPrimary = Guid.Empty;
			_EdgeGuidSecondary = Guid.Empty;
			_EdgeNode1Guid = Guid.Empty;
			_EdgeNode2Guid = Guid.Empty;
			_EdgeNode3Guid = Guid.Empty;
			_EdgeNamePrimary = string.Empty;
			_EdgeNameSecondary = string.Empty;
		}

		private static async Task TestNodeDeleteMethods()
		{
			LiteGraphSdk sdk = RequireSdk();
			await EnsureGraphExistsAsync().ConfigureAwait(false);

			async Task<Node> CreateNodeAsync(string namePrefix)
			{
				Node node = new Node
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					Name = $"{namePrefix}-{UniqueName("node-delete")}",
					Data = new { description = "node delete helper" }
				};

				Node? created = await sdk.Node.Create(node).ConfigureAwait(false);
				AssertNotNull(created, "Node create for delete methods");
				return created!;
			}

			// DeleteByGuid
			Node single = await CreateNodeAsync("node-delete-single").ConfigureAwait(false);
			await sdk.Node.DeleteByGuid(_TenantGuid, _GraphGuid, single.GUID).ConfigureAwait(false);
			bool exists = await sdk.Node.ExistsByGuid(_TenantGuid, _GraphGuid, single.GUID).ConfigureAwait(false);
			AssertFalse(exists, "Node deleted via DeleteByGuid");

			// DeleteMany
			Node node1 = await CreateNodeAsync("node-delete-many-1").ConfigureAwait(false);
			Node node2 = await CreateNodeAsync("node-delete-many-2").ConfigureAwait(false);
			await sdk.Node.DeleteMany(_TenantGuid, _GraphGuid, new List<Guid> { node1.GUID, node2.GUID }).ConfigureAwait(false);
			AssertFalse(await sdk.Node.ExistsByGuid(_TenantGuid, _GraphGuid, node1.GUID).ConfigureAwait(false), "Node1 removed via DeleteMany");
			AssertFalse(await sdk.Node.ExistsByGuid(_TenantGuid, _GraphGuid, node2.GUID).ConfigureAwait(false), "Node2 removed via DeleteMany");

			// DeleteAllInGraph
			await CreateNodeAsync("node-delete-all-graph").ConfigureAwait(false);
			await sdk.Node.DeleteAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			List<Node>? graphNodes = await sdk.Node.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(graphNodes == null || graphNodes.Count == 0, "Nodes deleted via DeleteAllInGraph");

			// DeleteAllInTenant
			await CreateNodeAsync("node-delete-all-tenant").ConfigureAwait(false);
			await sdk.Node.DeleteAllInTenant(_TenantGuid).ConfigureAwait(false);
			List<Node>? tenantNodes = await sdk.Node.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);
			AssertTrue(tenantNodes == null || tenantNodes.Count == 0, "Nodes deleted via DeleteAllInTenant");

			_NodePrimaryGuid = Guid.Empty;
			_NodeSecondaryGuid = Guid.Empty;
			_NodeTertiaryGuid = Guid.Empty;
			_NodePrimaryName = string.Empty;
			_NodeSecondaryName = string.Empty;
			_NodeTertiaryName = string.Empty;
			_NodeRelationshipsPrepared = false;
		}

		private static async Task TestGraphDeleteMethods()
		{
			LiteGraphSdk sdk = RequireSdk();
			await EnsureGraphExistsAsync().ConfigureAwait(false);

			// DeleteByGuid (temporary graph)
			Graph tempGraph = new Graph
			{
				TenantGUID = _TenantGuid,
				Name = UniqueName("sdk-graph-delete-temp")
			};

			Graph? createdTemp = await sdk.Graph.Create(tempGraph).ConfigureAwait(false);
			AssertNotNull(createdTemp, "Graph create for DeleteByGuid");
			await sdk.Graph.DeleteByGuid(_TenantGuid, createdTemp!.GUID, true).ConfigureAwait(false);

			await sdk.Graph.DeleteAllInTenant(_TenantGuid).ConfigureAwait(false);
			List<Graph>? remainingGraphs = await sdk.Graph.ReadMany(_TenantGuid).ConfigureAwait(false);
			AssertTrue(remainingGraphs == null || remainingGraphs.Count == 0, "All graphs deleted in tenant");

			_GraphGuid = Guid.Empty;
			_GraphName = string.Empty;
			_SubgraphPrepared = false;
			_SubgraphNodeGuids.Clear();
			_SubgraphEdgeInfos.Clear();
		}

		private static async Task TestTenantDeleteMethods()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for delete methods");
			LiteGraphSdk sdk = RequireSdk();

			await sdk.Tenant.DeleteByGuid(_TenantGuid, true).ConfigureAwait(false);

			bool exists = await sdk.Tenant.ExistsByGuid(_TenantGuid).ConfigureAwait(false);
			AssertFalse(exists, "Tenant deleted via DeleteByGuid");

			_TenantGuid = Guid.Empty;
			_TenantName = string.Empty;
			_CreatedTenantGuids.Clear();
		}

		private static async Task EnsureGraphExistsAsync()
		{
			if (_TenantGuid == Guid.Empty)
			{
				throw new InvalidOperationException("Tenant must exist for graph operations.");
			}

			if (_GraphGuid != Guid.Empty)
			{
				return;
			}

			LiteGraphSdk sdk = RequireSdk();
			Graph graph = new Graph
			{
				TenantGUID = _TenantGuid,
				Name = UniqueName("sdk-graph-recreated"),
				Data = new { description = "recreated graph" }
			};

			Graph? created = await sdk.Graph.Create(graph).ConfigureAwait(false);
			AssertNotNull(created, "Graph recreate for dependencies");

			_GraphGuid = created!.GUID;
			_GraphName = created.Name ?? string.Empty;
		}

		#endregion



		#region Entry-Point

		public static async Task<int> Main(string[] args)
		{
			try
			{
				ApplyArgs(args);
				PrintBanner();

				using LiteGraphSdk sdk = new LiteGraphSdk(_Endpoint, _BearerToken);
				if (_VerboseLogging)
				{
					sdk.Logger = (severity, message) => Console.WriteLine($"[{severity}] {message}");
				}

				_Sdk = sdk;

				await RunAllTests().ConfigureAwait(false);
				bool allPassed = _TestResults.All(r => r.Passed);
				PrintSummary();

				if (allPassed)
				{
					await CleanupTestDataAsync().ConfigureAwait(false);
				}
				else
				{
					Console.WriteLine("Skipping cleanup because one or more tests failed. Resources remain for investigation.");
				}

				return allPassed ? 0 : 1;
			}
			catch (Exception ex)
			{
				Console.WriteLine("");
				Console.WriteLine("FATAL ERROR: " + ex.Message);
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine("");
				return 1;
			}
		}

		#endregion

		#region Test-Orchestration

		private static async Task RunAllTests()
		{
			Console.WriteLine("Running automated SDK tests...");
			Console.WriteLine("");

			// Tenant tests (must run first to supply context for other modules)
			await RunTest("Tenant.Create", TestTenantCreate).ConfigureAwait(false);
			await RunTest("Tenant.ReadByGuid", TestTenantReadByGuid).ConfigureAwait(false);
			await RunTest("Tenant.ExistsByGuid", TestTenantExistsByGuid).ConfigureAwait(false);
			await RunTest("Tenant.Update", TestTenantUpdate).ConfigureAwait(false);
			await RunTest("Tenant.ReadMany", TestTenantReadMany).ConfigureAwait(false);
			await RunTest("Tenant.ReadByGuids", TestTenantReadByGuids).ConfigureAwait(false);
			await RunTest("Tenant.Enumerate", TestTenantEnumerate).ConfigureAwait(false);
			await RunTest("Tenant.GetStatistics", TestTenantGetStatistics).ConfigureAwait(false);
			await RunTest("Tenant.GetStatisticsAll", TestTenantGetStatisticsAll).ConfigureAwait(false);

			// Graph tests (require tenant to exist)
			await RunTest("Graph.Create", TestGraphCreate).ConfigureAwait(false);
			await RunTest("Graph.ReadByGuid", TestGraphReadByGuid).ConfigureAwait(false);
			await RunTest("Graph.ExistsByGuid", TestGraphExistsByGuid).ConfigureAwait(false);
			await RunTest("Graph.Update", TestGraphUpdate).ConfigureAwait(false);
			await RunTest("Graph.ReadAllInTenant", TestGraphReadAllInTenant).ConfigureAwait(false);
			await RunTest("Graph.ReadMany", TestGraphReadMany).ConfigureAwait(false);
			await RunTest("Graph.ReadByGuids", TestGraphReadByGuids).ConfigureAwait(false);
			await RunTest("Graph.ReadFirst", TestGraphReadFirst).ConfigureAwait(false);
			await RunTest("Graph.Enumerate", TestGraphEnumerate).ConfigureAwait(false);
			await RunTest("Graph.Search", TestGraphSearch).ConfigureAwait(false);
			await RunTest("Graph.GetStatistics", TestGraphGetStatistics).ConfigureAwait(false);
			await RunTest("Graph.GetStatisticsAll", TestGraphGetStatisticsAll).ConfigureAwait(false);
			await RunTest("Graph.GetSubgraph", TestGraphGetSubgraph).ConfigureAwait(false);
			await RunTest("Graph.GetSubgraphStatistics", TestGraphGetSubgraphStatistics).ConfigureAwait(false);
			await RunTest("Graph.ExportGraphToGexf", TestGraphExportGraphToGexf).ConfigureAwait(false);

			// Node tests
			await RunTest("Node.Create", TestNodeCreate).ConfigureAwait(false);
			await RunTest("Node.CreateMany", TestNodeCreateMany).ConfigureAwait(false);
			await RunTest("Node.ReadByGuid", TestNodeReadByGuid).ConfigureAwait(false);
			await RunTest("Node.ExistsByGuid", TestNodeExistsByGuid).ConfigureAwait(false);
			await RunTest("Node.Update", TestNodeUpdate).ConfigureAwait(false);
			await RunTest("Node.ReadMany", TestNodeReadMany).ConfigureAwait(false);
			await RunTest("Node.ReadByGuids", TestNodeReadByGuids).ConfigureAwait(false);
			await RunTest("Node.ReadParents", TestNodeReadParents).ConfigureAwait(false);
			await RunTest("Node.ReadChildren", TestNodeReadChildren).ConfigureAwait(false);
			await RunTest("Node.ReadNeighbors", TestNodeReadNeighbors).ConfigureAwait(false);
			await RunTest("Node.ReadRoutes", TestNodeReadRoutes).ConfigureAwait(false);
			await RunTest("Node.ReadAllInTenant", TestNodeReadAllInTenant).ConfigureAwait(false);
			await RunTest("Node.ReadAllInGraph", TestNodeReadAllInGraph).ConfigureAwait(false);
			await RunTest("Node.ReadMostConnected", TestNodeReadMostConnected).ConfigureAwait(false);
			await RunTest("Node.ReadLeastConnected", TestNodeReadLeastConnected).ConfigureAwait(false);
			await RunTest("Node.Search", TestNodeSearch).ConfigureAwait(false);
			await RunTest("Node.ReadFirst", TestNodeReadFirst).ConfigureAwait(false);
			await RunTest("Node.Enumerate", TestNodeEnumerate).ConfigureAwait(false);

			// Edge tests
			await RunTest("Edge.Create", TestEdgeCreate).ConfigureAwait(false);
			await RunTest("Edge.CreateMany", TestEdgeCreateMany).ConfigureAwait(false);
			await RunTest("Edge.ReadByGuid", TestEdgeReadByGuid).ConfigureAwait(false);
			await RunTest("Edge.ExistsByGuid", TestEdgeExistsByGuid).ConfigureAwait(false);
			await RunTest("Edge.Update", TestEdgeUpdate).ConfigureAwait(false);
			await RunTest("Edge.ReadMany", TestEdgeReadMany).ConfigureAwait(false);
			await RunTest("Edge.ReadByGuids", TestEdgeReadByGuids).ConfigureAwait(false);
			await RunTest("Edge.ReadNodeEdges", TestEdgeReadNodeEdges).ConfigureAwait(false);
			await RunTest("Edge.ReadEdgesFromNode", TestEdgeReadEdgesFromNode).ConfigureAwait(false);
			await RunTest("Edge.ReadEdgesToNode", TestEdgeReadEdgesToNode).ConfigureAwait(false);
			await RunTest("Edge.ReadEdgesBetweenNodes", TestEdgeReadEdgesBetweenNodes).ConfigureAwait(false);
			await RunTest("Edge.ReadAllInTenant", TestEdgeReadAllInTenant).ConfigureAwait(false);
			await RunTest("Edge.ReadAllInGraph", TestEdgeReadAllInGraph).ConfigureAwait(false);
			await RunTest("Edge.ReadFirst", TestEdgeReadFirst).ConfigureAwait(false);
			await RunTest("Edge.Enumerate", TestEdgeEnumerate).ConfigureAwait(false);
			await RunTest("Edge.Search", TestEdgeSearch).ConfigureAwait(false);

			// Label tests
			await RunTest("Label.Create", TestLabelCreate).ConfigureAwait(false);
			await RunTest("Label.CreateMany", TestLabelCreateMany).ConfigureAwait(false);
			await RunTest("Label.ReadByGuid", TestLabelReadByGuid).ConfigureAwait(false);
			await RunTest("Label.ExistsByGuid", TestLabelExistsByGuid).ConfigureAwait(false);
			await RunTest("Label.Update", TestLabelUpdate).ConfigureAwait(false);
			await RunTest("Label.ReadMany", TestLabelReadMany).ConfigureAwait(false);
			await RunTest("Label.ReadByGuids", TestLabelReadByGuids).ConfigureAwait(false);
			await RunTest("Label.ReadAllInTenant", TestLabelReadAllInTenant).ConfigureAwait(false);
			await RunTest("Label.ReadAllInGraph", TestLabelReadAllInGraph).ConfigureAwait(false);
			await RunTest("Label.ReadManyGraph", TestLabelReadManyGraph).ConfigureAwait(false);
			await RunTest("Label.ReadManyNode", TestLabelReadManyNode).ConfigureAwait(false);
			await RunTest("Label.ReadManyEdge", TestLabelReadManyEdge).ConfigureAwait(false);
			await RunTest("Label.Enumerate", TestLabelEnumerate).ConfigureAwait(false);

			// Tag tests
			await RunTest("Tag.Create", TestTagCreate).ConfigureAwait(false);
			await RunTest("Tag.CreateMany", TestTagCreateMany).ConfigureAwait(false);
			await RunTest("Tag.ReadByGuid", TestTagReadByGuid).ConfigureAwait(false);
			await RunTest("Tag.ExistsByGuid", TestTagExistsByGuid).ConfigureAwait(false);
			await RunTest("Tag.Update", TestTagUpdate).ConfigureAwait(false);
			await RunTest("Tag.ReadMany", TestTagReadMany).ConfigureAwait(false);
			await RunTest("Tag.ReadByGuids", TestTagReadByGuids).ConfigureAwait(false);
			await RunTest("Tag.ReadAllInTenant", TestTagReadAllInTenant).ConfigureAwait(false);
			await RunTest("Tag.ReadAllInGraph", TestTagReadAllInGraph).ConfigureAwait(false);
			await RunTest("Tag.ReadManyGraph", TestTagReadManyGraph).ConfigureAwait(false);
			await RunTest("Tag.ReadManyNode", TestTagReadManyNode).ConfigureAwait(false);
			await RunTest("Tag.ReadManyEdge", TestTagReadManyEdge).ConfigureAwait(false);
			await RunTest("Tag.Enumerate", TestTagEnumerate).ConfigureAwait(false);

			// Vector tests
			await RunTest("Vector.Create", TestVectorCreate).ConfigureAwait(false);
			await RunTest("Vector.CreateMany", TestVectorCreateMany).ConfigureAwait(false);
			await RunTest("Vector.ReadByGuid", TestVectorReadByGuid).ConfigureAwait(false);
			await RunTest("Vector.ExistsByGuid", TestVectorExistsByGuid).ConfigureAwait(false);
			await RunTest("Vector.Update", TestVectorUpdate).ConfigureAwait(false);
			await RunTest("Vector.ReadMany", TestVectorReadMany).ConfigureAwait(false);
			await RunTest("Vector.ReadByGuids", TestVectorReadByGuids).ConfigureAwait(false);
			await RunTest("Vector.ReadAllInTenant", TestVectorReadAllInTenant).ConfigureAwait(false);
			await RunTest("Vector.ReadAllInGraph", TestVectorReadAllInGraph).ConfigureAwait(false);
			await RunTest("Vector.ReadManyGraph", TestVectorReadManyGraph).ConfigureAwait(false);
			await RunTest("Vector.ReadManyNode", TestVectorReadManyNode).ConfigureAwait(false);
			await RunTest("Vector.ReadManyEdge", TestVectorReadManyEdge).ConfigureAwait(false);
			await RunTest("Vector.Enumerate", TestVectorEnumerate).ConfigureAwait(false);
			await RunTest("Vector.Search", TestVectorSearch).ConfigureAwait(false);

			// User tests (require tenant)
			await RunTest("User.Create", TestUserCreate).ConfigureAwait(false);
			await RunTest("User.ReadByGuid", TestUserReadByGuid).ConfigureAwait(false);
			await RunTest("User.ExistsByGuid", TestUserExistsByGuid).ConfigureAwait(false);
			await RunTest("User.Update", TestUserUpdate).ConfigureAwait(false);
			await RunTest("User.ReadMany", TestUserReadMany).ConfigureAwait(false);
			await RunTest("User.ReadByGuids", TestUserReadByGuids).ConfigureAwait(false);
			await RunTest("User.Enumerate", TestUserEnumerate).ConfigureAwait(false);

			// User authentication tests
			await RunTest("UserAuth.GetTenantsForEmail", TestUserAuthGetTenantsForEmail).ConfigureAwait(false);
			await RunTest("UserAuth.GenerateToken", TestUserAuthGenerateToken).ConfigureAwait(false);
			await RunTest("UserAuth.GetTokenDetails", TestUserAuthGetTokenDetails).ConfigureAwait(false);

			// Credential tests (require tenant and user)
			await RunTest("Credential.Create", TestCredentialCreate).ConfigureAwait(false);
			await RunTest("Credential.ReadByGuid", TestCredentialReadByGuid).ConfigureAwait(false);
			await RunTest("Credential.ExistsByGuid", TestCredentialExistsByGuid).ConfigureAwait(false);
			await RunTest("Credential.Update", TestCredentialUpdate).ConfigureAwait(false);
			await RunTest("Credential.ReadMany", TestCredentialReadMany).ConfigureAwait(false);
			await RunTest("Credential.ReadByGuids", TestCredentialReadByGuids).ConfigureAwait(false);
			await RunTest("Credential.ReadByBearerToken", TestCredentialReadByBearerToken).ConfigureAwait(false);
			await RunTest("Credential.Enumerate", TestCredentialEnumerate).ConfigureAwait(false);

			// Admin tests
			await RunTest("Admin.Backup", TestAdminBackup).ConfigureAwait(false);
			await RunTest("Admin.ListBackups", TestAdminListBackups).ConfigureAwait(false);
			await RunTest("Admin.ReadBackup", TestAdminReadBackup).ConfigureAwait(false);
			await RunTest("Admin.BackupExists", TestAdminBackupExists).ConfigureAwait(false);
			await RunTest("Admin.DeleteBackup", TestAdminDeleteBackup).ConfigureAwait(false);
			await RunTest("Admin.FlushDatabase", TestAdminFlushDatabase).ConfigureAwait(false);

			// Batch tests
			await RunTest("Batch.Existence", TestBatchExistence).ConfigureAwait(false);

			// Delete tests (run in dependency order)
			await RunTest("Vector.DeleteMethods", TestVectorDeleteMethods).ConfigureAwait(false);
			await RunTest("Tag.DeleteMethods", TestTagDeleteMethods).ConfigureAwait(false);
			await RunTest("Label.DeleteMethods", TestLabelDeleteMethods).ConfigureAwait(false);
			await RunTest("Edge.DeleteMethods", TestEdgeDeleteMethods).ConfigureAwait(false);
			await RunTest("Node.DeleteMethods", TestNodeDeleteMethods).ConfigureAwait(false);
			await RunTest("Graph.DeleteMethods", TestGraphDeleteMethods).ConfigureAwait(false);
			await RunTest("Tenant.DeleteMethods", TestTenantDeleteMethods).ConfigureAwait(false);
		}

		private static async Task RunTest(string name, Func<Task> testFunc)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			bool passed = false;
			string? error = null;

			try
			{
				await testFunc().ConfigureAwait(false);
				passed = true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
			}

			stopwatch.Stop();

			_TestResults.Add(new TestResult
			{
				Name = name,
				Passed = passed,
				RuntimeMs = stopwatch.ElapsedMilliseconds,
				ErrorMessage = error
			});

			string status = passed ? "PASS" : "FAIL";
			Console.WriteLine($"[{status}] {name,-35} {stopwatch.ElapsedMilliseconds,6}ms");
			if (!passed && !string.IsNullOrEmpty(error))
			{
				Console.WriteLine($"       Error: {error}");
			}
		}

		#endregion

		#region Tenant-Tests

		private static async Task TestTenantCreate()
		{
			LiteGraphSdk sdk = RequireSdk();

			TenantMetadata tenant = new TenantMetadata
			{
				Name = UniqueName("sdk-tenant")
			};

			TenantMetadata? created = await sdk.Tenant.Create(tenant).ConfigureAwait(false);

			AssertNotNull(created, "Tenant create result");
			AssertNotEmpty(created!.GUID, "Tenant GUID");
			AssertEqual(tenant.Name, created.Name, "Tenant name");

			_TenantGuid = created.GUID;
			_TenantName = created.Name ?? string.Empty;
			_CreatedTenantGuids.Add(_TenantGuid);
		}

		private static async Task TestTenantReadByGuid()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestTenantReadByGuid");

			LiteGraphSdk sdk = RequireSdk();
			TenantMetadata? tenant = await sdk.Tenant.ReadByGuid(_TenantGuid).ConfigureAwait(false);

			AssertNotNull(tenant, "Tenant read result");
			AssertEqual(_TenantGuid, tenant!.GUID, "Tenant GUID match");
			AssertEqual(_TenantName, tenant.Name, "Tenant name match");
		}

		private static async Task TestTenantExistsByGuid()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestTenantExistsByGuid");

			LiteGraphSdk sdk = RequireSdk();
			bool exists = await sdk.Tenant.ExistsByGuid(_TenantGuid).ConfigureAwait(false);
			AssertTrue(exists, "Tenant should exist");

			bool nonExistent = await sdk.Tenant.ExistsByGuid(Guid.NewGuid()).ConfigureAwait(false);
			AssertFalse(nonExistent, "Random tenant should not exist");
		}

		private static async Task TestTenantUpdate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestTenantUpdate");

			LiteGraphSdk sdk = RequireSdk();
			TenantMetadata? tenant = await sdk.Tenant.ReadByGuid(_TenantGuid).ConfigureAwait(false);
			AssertNotNull(tenant, "Tenant to update");

			string updatedName = UniqueName("sdk-tenant-updated");
			tenant!.Name = updatedName;

			TenantMetadata? updated = await sdk.Tenant.Update(tenant).ConfigureAwait(false);
			AssertNotNull(updated, "Updated tenant");
			AssertEqual(updatedName, updated!.Name, "Updated tenant name");

			_TenantName = updated.Name ?? string.Empty;
		}

		private static async Task TestTenantReadMany()
		{
			LiteGraphSdk sdk = RequireSdk();
			List<TenantMetadata>? tenants = await sdk.Tenant.ReadMany().ConfigureAwait(false);

			AssertNotNull(tenants, "Tenant list");
			AssertTrue(tenants!.Count > 0, "Tenant list count");
			AssertTrue(tenants.Any(t => t.GUID == _TenantGuid), "Tenant list contains created tenant");
		}

		private static async Task TestTenantReadByGuids()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestTenantReadByGuids");

			LiteGraphSdk sdk = RequireSdk();
			List<TenantMetadata>? tenants = await sdk.Tenant.ReadByGuids(new List<Guid> { _TenantGuid }).ConfigureAwait(false);

			AssertNotNull(tenants, "Tenant list by GUIDs");
			AssertEqual(1, tenants!.Count, "Tenant list count");
			AssertEqual(_TenantGuid, tenants[0].GUID, "Tenant GUID from list");
		}

		private static async Task TestTenantEnumerate()
		{
			LiteGraphSdk sdk = RequireSdk();

			EnumerationRequest request = new EnumerationRequest
			{
				MaxResults = 5
			};

			EnumerationResult<TenantMetadata>? result = await sdk.Tenant.Enumerate(request).ConfigureAwait(false);

			AssertNotNull(result, "Tenant enumeration result");
			AssertTrue(result!.Objects.Count > 0, "Enumerated tenants count");
		}

		private static async Task TestTenantGetStatistics()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestTenantGetStatistics");

			LiteGraphSdk sdk = RequireSdk();
			TenantStatistics? stats = await sdk.Tenant.GetStatistics(_TenantGuid).ConfigureAwait(false);
			AssertNotNull(stats, "Tenant statistics");
		}

		private static async Task TestTenantGetStatisticsAll()
		{
			LiteGraphSdk sdk = RequireSdk();
			Dictionary<Guid, TenantStatistics>? stats = await sdk.Tenant.GetStatistics().ConfigureAwait(false);

			AssertNotNull(stats, "All tenant statistics");
			AssertTrue(stats!.ContainsKey(_TenantGuid), "Statistics contains tenant entry");
		}

		#endregion

		#region Graph-Tests

		private static async Task TestGraphCreate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphCreate");

			LiteGraphSdk sdk = RequireSdk();

			Graph graph = new Graph
			{
				TenantGUID = _TenantGuid,
				Name = UniqueName("sdk-graph"),
				Data = new { description = "sdk automated test graph" }
			};

			Graph? created = await sdk.Graph.Create(graph).ConfigureAwait(false);

			AssertNotNull(created, "Graph create result");
			AssertNotEmpty(created!.GUID, "Graph GUID");
			AssertEqual(graph.Name, created.Name, "Graph name");

			_GraphGuid = created.GUID;
			_GraphName = created.Name ?? string.Empty;
		}

		private static async Task TestGraphReadByGuid()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphReadByGuid");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphReadByGuid");

			LiteGraphSdk sdk = RequireSdk();
			Graph? graph = await sdk.Graph.ReadByGuid(_TenantGuid, _GraphGuid, includeData: true).ConfigureAwait(false);

			AssertNotNull(graph, "Graph read result");
			AssertEqual(_GraphGuid, graph!.GUID, "Graph GUID match");
			AssertEqual(_GraphName, graph.Name, "Graph name match");
			AssertNotNull(graph.Data, "Graph data");
		}

		private static async Task TestGraphExistsByGuid()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphExistsByGuid");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphExistsByGuid");

			LiteGraphSdk sdk = RequireSdk();
			bool exists = await sdk.Graph.ExistsByGuid(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertTrue(exists, "Graph should exist");
		}

		private static async Task TestGraphUpdate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphUpdate");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphUpdate");

			LiteGraphSdk sdk = RequireSdk();
			Graph? graph = await sdk.Graph.ReadByGuid(_TenantGuid, _GraphGuid).ConfigureAwait(false);
			AssertNotNull(graph, "Graph to update");

			string updatedName = UniqueName("sdk-graph-updated");
			graph!.Name = updatedName;
			graph.Data = new { description = "updated graph data" };

			Graph? updated = await sdk.Graph.Update(graph).ConfigureAwait(false);
			AssertNotNull(updated, "Updated graph");
			AssertEqual(updatedName, updated!.Name, "Updated graph name");

			_GraphName = updated.Name ?? string.Empty;
		}

		private static async Task TestGraphReadAllInTenant()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphReadAllInTenant");

			LiteGraphSdk sdk = RequireSdk();
			List<Graph>? graphs = await sdk.Graph.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);

			AssertNotNull(graphs, "Graphs read all result");
			AssertTrue(graphs!.Count > 0, "Graphs count");
			AssertTrue(graphs.Any(g => g.GUID == _GraphGuid), "Graphs include created graph");
		}

		private static async Task TestGraphReadMany()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphReadMany");

			LiteGraphSdk sdk = RequireSdk();
			List<Graph>? graphs = await sdk.Graph.ReadMany(_TenantGuid).ConfigureAwait(false);

			AssertNotNull(graphs, "Graphs read many result");
			AssertTrue(graphs!.Count > 0, "Graphs count");
		}

		private static async Task TestGraphReadByGuids()
		{
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphReadByGuids");
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphReadByGuids");

			LiteGraphSdk sdk = RequireSdk();
			List<Graph>? graphs = await sdk.Graph.ReadByGuids(
				_TenantGuid,
				new List<Guid> { _GraphGuid },
				includeData: true).ConfigureAwait(false);

			AssertNotNull(graphs, "Graphs read by GUIDs");
			AssertEqual(1, graphs!.Count, "Graphs count");
			AssertEqual(_GraphGuid, graphs[0].GUID, "Graph GUID from list");
		}

		private static async Task TestGraphReadFirst()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphReadFirst");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphReadFirst");

			LiteGraphSdk sdk = RequireSdk();

			SearchRequest request = new SearchRequest
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = _GraphName
			};

			Graph? graph = await sdk.Graph.ReadFirst(request).ConfigureAwait(false);
			AssertNotNull(graph, "Graph read first result");
			AssertEqual(_GraphGuid, graph!.GUID, "Graph GUID from ReadFirst");
		}

		private static async Task TestGraphEnumerate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphEnumerate");

			LiteGraphSdk sdk = RequireSdk();

			EnumerationRequest request = new EnumerationRequest
			{
				TenantGUID = _TenantGuid,
				MaxResults = 5,
				IncludeData = true
			};

			EnumerationResult<Graph>? result = await sdk.Graph.Enumerate(request).ConfigureAwait(false);

			AssertNotNull(result, "Graph enumeration result");
			AssertTrue(result!.Objects.Count > 0, "Graph enumeration count");
		}

		private static async Task TestGraphGetStatistics()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphGetStatistics");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphGetStatistics");

			LiteGraphSdk sdk = RequireSdk();
			GraphStatistics? stats = await sdk.Graph.GetStatistics(_TenantGuid, _GraphGuid).ConfigureAwait(false);

			AssertNotNull(stats, "Graph statistics");
		}

		private static async Task TestGraphGetStatisticsAll()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphGetStatisticsAll");

			LiteGraphSdk sdk = RequireSdk();
			Dictionary<Guid, GraphStatistics>? stats = await sdk.Graph.GetStatistics(_TenantGuid).ConfigureAwait(false);

			AssertNotNull(stats, "Graph statistics collection");
			AssertTrue(stats!.ContainsKey(_GraphGuid), "Graph statistics contains graph entry");
		}

		private static async Task TestGraphSearch()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphSearch");
			AssertFalse(string.IsNullOrEmpty(_GraphName), "Graph name for TestGraphSearch");

			LiteGraphSdk sdk = RequireSdk();

			SearchRequest request = new SearchRequest
			{
				TenantGUID = _TenantGuid,
				Name = _GraphName
			};

			SearchResult? result = await sdk.Graph.Search(request).ConfigureAwait(false);

			AssertNotNull(result, "Graph search result");
			AssertNotNull(result!.Graphs, "Graph search graphs");
			AssertTrue(result.Graphs!.Any(g => g.GUID == _GraphGuid), "Graph search should include target graph");
		}

		private static async Task TestGraphGetSubgraph()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphGetSubgraph");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphGetSubgraph");

			Guid nodeGuid = await EnsureSubgraphScenarioAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			SearchResult? result = await sdk.Graph.GetSubgraph(
				_TenantGuid,
				_GraphGuid,
				nodeGuid,
				maxDepth: 1,
				maxNodes: 5,
				maxEdges: 5,
				includeData: true,
				includeSubordinates: false).ConfigureAwait(false);

			AssertNotNull(result, "Subgraph result");
		}

		private static async Task TestGraphGetSubgraphStatistics()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphGetSubgraphStatistics");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphGetSubgraphStatistics");

			Guid nodeGuid = await EnsureSubgraphScenarioAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			GraphStatistics? stats = await sdk.Graph.GetSubgraphStatistics(
				_TenantGuid,
				_GraphGuid,
				nodeGuid,
				maxDepth: 1,
				maxNodes: 5,
				maxEdges: 5).ConfigureAwait(false);

			AssertNotNull(stats, "Subgraph statistics");
		}

		private static async Task TestGraphExportGraphToGexf()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestGraphExportGraphToGexf");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestGraphExportGraphToGexf");

			await EnsureSubgraphScenarioAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			string? gexf = await sdk.Graph.ExportGraphToGexf(
				_TenantGuid,
				_GraphGuid,
				includeData: true,
				includeSubordinates: false).ConfigureAwait(false);

			AssertFalse(string.IsNullOrWhiteSpace(gexf), "GEXF output generated");
			AssertTrue(gexf!.Contains("<gexf", StringComparison.OrdinalIgnoreCase), "GEXF output contains tag");
		}

		#endregion

		#region Edge-Tests

		private static async Task TestEdgeCreate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestEdgeCreate");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestEdgeCreate");

			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			string edgeName = UniqueName("sdk-edge");

			Edge edge = new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = _EdgeNode1Guid,
				To = _EdgeNode2Guid,
				Name = edgeName,
				Cost = 1,
				Data = new { description = "primary edge" }
			};

			Edge? created = await sdk.Edge.Create(edge).ConfigureAwait(false);

			AssertNotNull(created, "Edge create result");
			AssertNotEmpty(created!.GUID, "Edge GUID");
			AssertEqual(edgeName, created.Name, "Edge name");

			_EdgeGuidPrimary = created.GUID;
			_EdgeNamePrimary = created.Name ?? string.Empty;
		}

		private static async Task TestEdgeCreateMany()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestEdgeCreateMany");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestEdgeCreateMany");

			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();

			List<Edge> edges = new List<Edge>
			{
				new Edge
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					From = _EdgeNode2Guid,
					To = _EdgeNode3Guid,
					Name = UniqueName("sdk-edge-secondary"),
					Cost = 2,
					Data = new { description = "secondary edge" }
				},
				new Edge
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					From = _EdgeNode3Guid,
					To = _EdgeNode1Guid,
					Name = UniqueName("sdk-edge-tertiary"),
					Cost = 3,
					Data = new { description = "tertiary edge" }
				}
			};

			List<Edge>? created = await sdk.Edge.CreateMany(_TenantGuid, _GraphGuid, edges).ConfigureAwait(false);

			AssertNotNull(created, "Edge create many result");
			AssertEqual(2, created!.Count, "Edge create many count");

			_EdgeGuidSecondary = created[0].GUID;
			_EdgeNameSecondary = created[0].Name ?? string.Empty;
		}

		private static async Task TestEdgeReadByGuid()
		{
			AssertNotEmpty(_EdgeGuidPrimary, "Edge GUID for TestEdgeReadByGuid");

			LiteGraphSdk sdk = RequireSdk();
			Edge? edge = await sdk.Edge.ReadByGuid(_TenantGuid, _GraphGuid, _EdgeGuidPrimary, includeData: true, includeSubordinates: false).ConfigureAwait(false);

			AssertNotNull(edge, "Edge read result");
			AssertEqual(_EdgeGuidPrimary, edge!.GUID, "Edge GUID match");
		}

		private static async Task TestEdgeExistsByGuid()
		{
			AssertNotEmpty(_EdgeGuidPrimary, "Edge GUID for TestEdgeExistsByGuid");

			LiteGraphSdk sdk = RequireSdk();
			bool exists = await sdk.Edge.ExistsByGuid(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);
			AssertTrue(exists, "Edge should exist");

			bool missing = await sdk.Edge.ExistsByGuid(_TenantGuid, _GraphGuid, Guid.NewGuid()).ConfigureAwait(false);
			AssertFalse(missing, "Random edge should not exist");
		}

		private static async Task TestEdgeUpdate()
		{
			AssertNotEmpty(_EdgeGuidPrimary, "Edge GUID for TestEdgeUpdate");

			LiteGraphSdk sdk = RequireSdk();
			Edge? edge = await sdk.Edge.ReadByGuid(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);
			AssertNotNull(edge, "Edge to update");

			string updatedName = UniqueName("sdk-edge-updated");
			edge!.Name = updatedName;
			edge.Cost = 5;
			edge.Data = new { description = "updated edge" };

			Edge? updated = await sdk.Edge.Update(edge).ConfigureAwait(false);

			AssertNotNull(updated, "Edge update result");
			AssertEqual(updatedName, updated!.Name, "Updated edge name");

			_EdgeNamePrimary = updated.Name ?? string.Empty;
		}

		private static async Task TestEdgeReadMany()
		{
			LiteGraphSdk sdk = RequireSdk();
			List<Edge>? edges = await sdk.Edge.ReadMany(_TenantGuid, _GraphGuid).ConfigureAwait(false);

			AssertNotNull(edges, "Edge list");
			AssertTrue(edges!.Count > 0, "Edge list count");
		}

		private static async Task TestEdgeReadByGuids()
		{
			AssertNotEmpty(_EdgeGuidPrimary, "Edge primary GUID for TestEdgeReadByGuids");
			AssertNotEmpty(_EdgeGuidSecondary, "Edge secondary GUID for TestEdgeReadByGuids");

			LiteGraphSdk sdk = RequireSdk();
			List<Edge>? edges = await sdk.Edge.ReadByGuids(_TenantGuid, _GraphGuid, new List<Guid> { _EdgeGuidPrimary, _EdgeGuidSecondary }).ConfigureAwait(false);

			AssertNotNull(edges, "Edges by GUIDs");
			AssertEqual(2, edges!.Count, "Edges by GUID count");
		}

		private static async Task TestEdgeReadNodeEdges()
		{
			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			List<Edge>? edges = await sdk.Edge.ReadNodeEdges(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);

			AssertNotNull(edges, "Node edges");
			AssertTrue(edges!.Count > 0, "Node edges count");
		}

		private static async Task TestEdgeReadEdgesFromNode()
		{
			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			List<Edge>? edges = await sdk.Edge.ReadEdgesFromNode(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);

			AssertNotNull(edges, "Edges from node");
			AssertTrue(edges!.Any(e => e.From == _EdgeNode1Guid), "Edges from node contain expected edge");
		}

		private static async Task TestEdgeReadEdgesToNode()
		{
			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			List<Edge>? edges = await sdk.Edge.ReadEdgesToNode(_TenantGuid, _GraphGuid, _EdgeNode2Guid).ConfigureAwait(false);

			AssertNotNull(edges, "Edges to node");
			AssertTrue(edges!.Any(e => e.To == _EdgeNode2Guid), "Edges to node contain expected edge");
		}

		private static async Task TestEdgeReadEdgesBetweenNodes()
		{
			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			List<Edge>? edges = await sdk.Edge.ReadEdgesBetweenNodes(_TenantGuid, _GraphGuid, _EdgeNode1Guid, _EdgeNode2Guid).ConfigureAwait(false);

			AssertNotNull(edges, "Edges between nodes");
			AssertTrue(edges!.Count > 0, "Edges between nodes count");
		}

		private static async Task TestEdgeReadAllInTenant()
		{
			LiteGraphSdk sdk = RequireSdk();
			List<Edge>? edges = await sdk.Edge.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);

			AssertNotNull(edges, "Edges in tenant");
			AssertTrue(edges!.Count > 0, "Edges in tenant count");
		}

		private static async Task TestEdgeReadAllInGraph()
		{
			LiteGraphSdk sdk = RequireSdk();
			List<Edge>? edges = await sdk.Edge.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);

			AssertNotNull(edges, "Edges in graph");
			AssertTrue(edges!.Count > 0, "Edges in graph count");
		}

		private static async Task TestEdgeReadFirst()
		{
			AssertNotEmpty(_EdgeGuidPrimary, "Edge GUID for TestEdgeReadFirst");

			LiteGraphSdk sdk = RequireSdk();
			SearchRequest request = new SearchRequest
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = _EdgeNamePrimary
			};

			Edge? edge = await sdk.Edge.ReadFirst(request).ConfigureAwait(false);

			AssertNotNull(edge, "Edge read first result");
			AssertEqual(_EdgeGuidPrimary, edge!.GUID, "Edge read first GUID");
		}

		private static async Task TestEdgeEnumerate()
		{
			EnumerationRequest request = new EnumerationRequest
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				MaxResults = 5,
				IncludeData = true
			};

			LiteGraphSdk sdk = RequireSdk();
			EnumerationResult<Edge>? result = await sdk.Edge.Enumerate(request).ConfigureAwait(false);

			AssertNotNull(result, "Edge enumeration result");
			AssertTrue(result!.Objects.Count > 0, "Edge enumeration count");
		}

		private static async Task TestEdgeSearch()
		{
			AssertFalse(string.IsNullOrEmpty(_EdgeNamePrimary), "Edge name for TestEdgeSearch");

			SearchRequest request = new SearchRequest
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = _EdgeNamePrimary
			};

			LiteGraphSdk sdk = RequireSdk();
			SearchResult? result = await sdk.Edge.Search(request).ConfigureAwait(false);

			AssertNotNull(result, "Edge search result");
			AssertNotNull(result!.Edges, "Edge search edges");
			AssertTrue(result.Edges!.Any(e => e.GUID == _EdgeGuidPrimary), "Edge search contains target edge");
		}

		#endregion

		#region User-Tests

		private static async Task TestUserCreate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestUserCreate");

			LiteGraphSdk sdk = RequireSdk();
			UserMaster user = new UserMaster
			{
				TenantGUID = _TenantGuid,
				Email = UniqueEmail(),
				Password = "P@ssw0rd!123",
				FirstName = "SDK",
				LastName = "Automated"
			};

			UserMaster? created = await sdk.User.Create(user).ConfigureAwait(false);

			AssertNotNull(created, "User create result");
			AssertNotEmpty(created!.GUID, "User GUID");
			AssertEqual(user.Email, created.Email, "User email");

			_UserGuid = created.GUID;
			_UserEmail = created.Email ?? string.Empty;
			_UserPassword = user.Password;
		}

		private static async Task TestUserReadByGuid()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestUserReadByGuid");
			AssertNotEmpty(_UserGuid, "User GUID for TestUserReadByGuid");

			LiteGraphSdk sdk = RequireSdk();
			UserMaster? user = await sdk.User.ReadByGuid(_TenantGuid, _UserGuid).ConfigureAwait(false);

			AssertNotNull(user, "User read result");
			AssertEqual(_UserGuid, user!.GUID, "User GUID match");
			AssertEqual(_UserEmail, user.Email, "User email match");
		}

		private static async Task TestUserExistsByGuid()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestUserExistsByGuid");
			AssertNotEmpty(_UserGuid, "User GUID for TestUserExistsByGuid");

			LiteGraphSdk sdk = RequireSdk();
			bool exists = await sdk.User.ExistsByGuid(_TenantGuid, _UserGuid).ConfigureAwait(false);
			AssertTrue(exists, "User should exist");

			bool nonExistent = await sdk.User.ExistsByGuid(_TenantGuid, Guid.NewGuid()).ConfigureAwait(false);
			AssertFalse(nonExistent, "Random user should not exist");
		}

		private static async Task TestUserUpdate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestUserUpdate");
			AssertNotEmpty(_UserGuid, "User GUID for TestUserUpdate");

			LiteGraphSdk sdk = RequireSdk();
			UserMaster? user = await sdk.User.ReadByGuid(_TenantGuid, _UserGuid).ConfigureAwait(false);
			AssertNotNull(user, "User to update");

			user!.FirstName = "SDK-Updated";
			user.LastName = "Automated";

			UserMaster? updated = await sdk.User.Update(user).ConfigureAwait(false);
			AssertNotNull(updated, "Updated user");
			AssertEqual("SDK-Updated", updated!.FirstName, "Updated first name");
		}

		private static async Task TestUserReadMany()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestUserReadMany");

			LiteGraphSdk sdk = RequireSdk();
			List<UserMaster>? users = await sdk.User.ReadMany(_TenantGuid).ConfigureAwait(false);

			AssertNotNull(users, "User list");
			AssertTrue(users!.Count > 0, "User list count");
			AssertTrue(users.Any(u => u.GUID == _UserGuid), "User list contains created user");
		}

		private static async Task TestUserReadByGuids()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestUserReadByGuids");
			AssertNotEmpty(_UserGuid, "User GUID for TestUserReadByGuids");

			LiteGraphSdk sdk = RequireSdk();
			List<UserMaster>? users = await sdk.User.ReadByGuids(_TenantGuid, new List<Guid> { _UserGuid }).ConfigureAwait(false);

			AssertNotNull(users, "Users by GUIDs");
			AssertEqual(1, users!.Count, "User list count");
			AssertEqual(_UserGuid, users[0].GUID, "User GUID from list");
		}

		private static async Task TestUserEnumerate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestUserEnumerate");

			LiteGraphSdk sdk = RequireSdk();

			EnumerationRequest request = new EnumerationRequest
			{
				TenantGUID = _TenantGuid,
				MaxResults = 5
			};

			EnumerationResult<UserMaster>? result = await sdk.User.Enumerate(request).ConfigureAwait(false);

			AssertNotNull(result, "User enumeration result");
			AssertTrue(result!.Objects.Count > 0, "User enumeration count");
		}

		#endregion

		#region UserAuthentication-Tests

		private static async Task TestUserAuthGetTenantsForEmail()
		{
			AssertFalse(string.IsNullOrEmpty(_UserEmail), "User email for auth tests");
			AssertFalse(string.IsNullOrEmpty(_UserPassword), "User password for auth tests");

			using LiteGraphSdk authSdk = new LiteGraphSdk(_UserEmail, _UserPassword, _TenantGuid, _Endpoint, _BearerToken);
			List<TenantMetadata>? tenants = await authSdk.UserAuthentication.GetTenantsForEmail().ConfigureAwait(false);

			AssertNotNull(tenants, "Auth tenants result");
			AssertTrue(tenants!.Any(t => t.GUID == _TenantGuid), "Auth tenants contains current tenant");
		}

		private static async Task TestUserAuthGenerateToken()
		{
			AssertFalse(string.IsNullOrEmpty(_UserEmail), "User email for auth token");
			AssertFalse(string.IsNullOrEmpty(_UserPassword), "User password for auth token");

			using LiteGraphSdk authSdk = new LiteGraphSdk(_UserEmail, _UserPassword, _TenantGuid, _Endpoint, _BearerToken);
			AuthenticationToken? token = await authSdk.UserAuthentication.GenerateToken().ConfigureAwait(false);

			AssertNotNull(token, "Generated token");
			AssertFalse(string.IsNullOrEmpty(token!.Token), "Token string");

			_UserAuthToken = token.Token;
		}

		private static async Task TestUserAuthGetTokenDetails()
		{
			await EnsureUserAuthTokenAsync().ConfigureAwait(false);

			using LiteGraphSdk authSdk = new LiteGraphSdk(_UserEmail, _UserPassword, _TenantGuid, _Endpoint, _BearerToken);
			AuthenticationToken? details = await authSdk.UserAuthentication.GetTokenDetails(_UserAuthToken).ConfigureAwait(false);

			AssertNotNull(details, "Token details");
			AssertEqual(_TenantGuid, details!.TenantGUID, "Token details tenant");
		}

		#endregion

		#region Credential-Tests

		private static async Task TestCredentialCreate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestCredentialCreate");
			AssertNotEmpty(_UserGuid, "User GUID for TestCredentialCreate");

			LiteGraphSdk sdk = RequireSdk();
			Credential credential = new Credential
			{
				TenantGUID = _TenantGuid,
				UserGUID = _UserGuid,
				Name = UniqueName("sdk-credential")
			};

			Credential? created = await sdk.Credential.Create(credential).ConfigureAwait(false);

			AssertNotNull(created, "Credential create result");
			AssertNotEmpty(created!.GUID, "Credential GUID");
			AssertEqual(credential.Name, created.Name, "Credential name");

			_CredentialGuid = created.GUID;
			_CredentialName = created.Name ?? string.Empty;
			_CredentialBearerToken = created.BearerToken ?? string.Empty;
		}

		private static async Task TestCredentialReadByGuid()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestCredentialReadByGuid");
			AssertNotEmpty(_CredentialGuid, "Credential GUID for TestCredentialReadByGuid");

			LiteGraphSdk sdk = RequireSdk();
			Credential? credential = await sdk.Credential.ReadByGuid(_TenantGuid, _CredentialGuid).ConfigureAwait(false);

			AssertNotNull(credential, "Credential read result");
			AssertEqual(_CredentialGuid, credential!.GUID, "Credential GUID match");
		}

		private static async Task TestCredentialExistsByGuid()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestCredentialExistsByGuid");
			AssertNotEmpty(_CredentialGuid, "Credential GUID for TestCredentialExistsByGuid");

			LiteGraphSdk sdk = RequireSdk();
			bool exists = await sdk.Credential.ExistsByGuid(_TenantGuid, _CredentialGuid).ConfigureAwait(false);
			AssertTrue(exists, "Credential should exist");
		}

		private static async Task TestCredentialUpdate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestCredentialUpdate");
			AssertNotEmpty(_CredentialGuid, "Credential GUID for TestCredentialUpdate");

			LiteGraphSdk sdk = RequireSdk();
			Credential? credential = await sdk.Credential.ReadByGuid(_TenantGuid, _CredentialGuid).ConfigureAwait(false);
			AssertNotNull(credential, "Credential to update");

			string updatedName = UniqueName("sdk-credential-updated");
			credential!.Name = updatedName;

			Credential? updated = await sdk.Credential.Update(credential).ConfigureAwait(false);
			AssertNotNull(updated, "Updated credential");
			AssertEqual(updatedName, updated!.Name, "Updated credential name");

			_CredentialName = updated.Name ?? string.Empty;
		}

		private static async Task TestCredentialReadMany()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestCredentialReadMany");

			LiteGraphSdk sdk = RequireSdk();
			List<Credential>? credentials = await sdk.Credential.ReadMany(_TenantGuid).ConfigureAwait(false);

			AssertNotNull(credentials, "Credential list");
			AssertTrue(credentials!.Count > 0, "Credential list count");
			AssertTrue(credentials.Any(c => c.GUID == _CredentialGuid), "Credential list contains created credential");
		}

		private static async Task TestCredentialReadByGuids()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestCredentialReadByGuids");
			AssertNotEmpty(_CredentialGuid, "Credential GUID for TestCredentialReadByGuids");

			LiteGraphSdk sdk = RequireSdk();
			List<Credential>? credentials = await sdk.Credential.ReadByGuids(_TenantGuid, new List<Guid> { _CredentialGuid }).ConfigureAwait(false);

			AssertNotNull(credentials, "Credentials by GUIDs");
			AssertEqual(1, credentials!.Count, "Credentials count");
			AssertEqual(_CredentialGuid, credentials[0].GUID, "Credential GUID from list");
		}

		private static async Task TestCredentialReadByBearerToken()
		{
			if (string.IsNullOrEmpty(_CredentialBearerToken))
			{
				throw new InvalidOperationException("Credential bearer token is not available");
			}

			LiteGraphSdk sdk = RequireSdk();
			Credential? credential = await sdk.Credential.ReadByBearerToken(_CredentialBearerToken).ConfigureAwait(false);

			AssertNotNull(credential, "Credential read by bearer token");
			AssertEqual(_CredentialGuid, credential!.GUID, "Credential GUID via bearer token");
		}

		private static async Task TestCredentialEnumerate()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestCredentialEnumerate");

			LiteGraphSdk sdk = RequireSdk();

			EnumerationRequest request = new EnumerationRequest
			{
				TenantGUID = _TenantGuid,
				MaxResults = 5
			};

			EnumerationResult<Credential>? result = await sdk.Credential.Enumerate(request).ConfigureAwait(false);

			AssertNotNull(result, "Credential enumeration result");
			AssertTrue(result!.Objects.Count > 0, "Credential enumeration count");
		}

		#endregion

		#region Admin-Tests

		private static async Task TestAdminBackup()
		{
			string filename = await EnsureBackupAsync(forceNew: true).ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			bool exists = await sdk.Admin.BackupExists(filename).ConfigureAwait(false);
			AssertTrue(exists, "Backup should exist after creation");
		}

		private static async Task TestAdminListBackups()
		{
			string filename = await EnsureBackupAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			List<BackupFile>? backups = await sdk.Admin.ListBackups().ConfigureAwait(false);

			AssertNotNull(backups, "Backup listing");
			AssertTrue(backups!.Any(b => string.Equals(b.Filename, filename, StringComparison.OrdinalIgnoreCase)), "Backup list contains created file");
		}

		private static async Task TestAdminReadBackup()
		{
			string filename = await EnsureBackupAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			BackupFile? backup = await sdk.Admin.ReadBackup(filename).ConfigureAwait(false);

			AssertNotNull(backup, "Backup read result");
			AssertEqual(filename, backup!.Filename, "Backup filename");
			AssertTrue(backup.Length >= 0, "Backup length");
			AssertNotNull(backup.Data, "Backup data");
		}

		private static async Task TestAdminBackupExists()
		{
			string filename = await EnsureBackupAsync().ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			bool exists = await sdk.Admin.BackupExists(filename).ConfigureAwait(false);
			AssertTrue(exists, "Existing backup should be reported");

			string missingName = $"missing-backup-{Guid.NewGuid():N}.bak";
			bool missing = await sdk.Admin.BackupExists(missingName).ConfigureAwait(false);
			AssertFalse(missing, "Random backup should not exist");
		}

		private static async Task TestAdminDeleteBackup()
		{
			string filename = await EnsureBackupAsync(forceNew: true).ConfigureAwait(false);

			LiteGraphSdk sdk = RequireSdk();
			await sdk.Admin.DeleteBackup(filename).ConfigureAwait(false);

			bool exists = await sdk.Admin.BackupExists(filename).ConfigureAwait(false);
			AssertFalse(exists, "Backup should be deleted");

			if (string.Equals(_BackupFilename, filename, StringComparison.OrdinalIgnoreCase))
			{
				_BackupFilename = null;
			}
		}

		private static async Task TestAdminFlushDatabase()
		{
			LiteGraphSdk sdk = RequireSdk();
			await sdk.Admin.FlushDatabase().ConfigureAwait(false);
		}

		#endregion

		#region Batch-Tests

		private static async Task TestBatchExistence()
		{
			AssertNotEmpty(_TenantGuid, "Tenant GUID for TestBatchExistence");
			AssertNotEmpty(_GraphGuid, "Graph GUID for TestBatchExistence");

			await EnsureSubgraphScenarioAsync().ConfigureAwait(false);

			if (_SubgraphNodeGuids.Count < 6 || _SubgraphEdgeInfos.Count < 1)
			{
				throw new InvalidOperationException("Subgraph scenario not initialized correctly for batch tests.");
			}

			Guid existingNode = _SubgraphNodeGuids[0];
			Guid missingNode = Guid.NewGuid();

			SubgraphEdgeInfo existingEdge = _SubgraphEdgeInfos[0];
			Guid missingEdge = Guid.NewGuid();

			Guid missingEdgeBetweenFrom = _SubgraphNodeGuids[4];
			Guid missingEdgeBetweenTo = _SubgraphNodeGuids[5];

			ExistenceRequest request = new ExistenceRequest
			{
				Nodes = new List<Guid> { existingNode, missingNode },
				Edges = new List<Guid> { existingEdge.EdgeGuid, missingEdge },
				EdgesBetween = new List<EdgeBetween>
				{
					new EdgeBetween { From = existingEdge.From, To = existingEdge.To },
					new EdgeBetween { From = missingEdgeBetweenFrom, To = missingEdgeBetweenTo }
				}
			};

			LiteGraphSdk sdk = RequireSdk();
			ExistenceResult? result = await sdk.Batch.Existence(_TenantGuid, _GraphGuid, request).ConfigureAwait(false);

			AssertNotNull(result, "Batch existence result");

			AssertTrue(result!.ExistingNodes != null && result.ExistingNodes.Contains(existingNode), "Existing node reported");
			AssertTrue(result.MissingNodes != null && result.MissingNodes.Contains(missingNode), "Missing node reported");

			AssertTrue(result.ExistingEdges != null && result.ExistingEdges.Contains(existingEdge.EdgeGuid), "Existing edge reported");
			AssertTrue(result.MissingEdges != null && result.MissingEdges.Contains(missingEdge), "Missing edge reported");

			bool hasExistingBetween = result.ExistingEdgesBetween != null &&
				result.ExistingEdgesBetween.Any(e => e.From == existingEdge.From && e.To == existingEdge.To);
			AssertTrue(hasExistingBetween, "Existing edge-between reported");

			bool hasMissingBetween = result.MissingEdgesBetween != null &&
				result.MissingEdgesBetween.Any(e => e.From == missingEdgeBetweenFrom && e.To == missingEdgeBetweenTo);
			AssertTrue(hasMissingBetween, "Missing edge-between reported");
		}

        #endregion

        #region Tag-Tests

        private static async Task TestTagCreate()
        {
            await EnsureEdgeTestNodesAsync().ConfigureAwait(false);
            await EnsureEdgePrimaryAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();

            TagMetadata tag = new TagMetadata
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                NodeGUID = _EdgeNode1Guid,
                Key = "sdk-tag-node",
                Value = UniqueName("tag-node-value")
            };

            TagMetadata? created = await sdk.Tag.Create(tag).ConfigureAwait(false);

            AssertNotNull(created, "Tag create result");
            AssertNotEmpty(created!.GUID, "Tag GUID");
            AssertEqual(tag.Key, created.Key, "Tag key");

            _TagNodePrimaryGuid = created.GUID;
            _TagNodePrimaryKey = created.Key ?? string.Empty;
            _TagNodePrimaryValue = created.Value ?? string.Empty;
        }

        private static async Task TestTagCreateMany()
        {
            await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();

            List<TagMetadata> tags = new List<TagMetadata>
            {
                new TagMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    EdgeGUID = _EdgeGuidPrimary,
                    Key = "sdk-tag-edge",
                    Value = UniqueName("tag-edge-value")
                },
                new TagMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Key = "sdk-tag-graph",
                    Value = UniqueName("tag-graph-value")
                }
            };

            List<TagMetadata>? created = await sdk.Tag.CreateMany(_TenantGuid, tags).ConfigureAwait(false);

            AssertNotNull(created, "Tag create many result");
            AssertEqual(2, created!.Count, "Tag create many count");

            foreach (TagMetadata createdTag in created)
            {
                if (createdTag.EdgeGUID.HasValue && _TagEdgePrimaryGuid == Guid.Empty)
                {
                    _TagEdgePrimaryGuid = createdTag.GUID;
                    _TagEdgePrimaryKey = createdTag.Key ?? string.Empty;
                    _TagEdgePrimaryValue = createdTag.Value ?? string.Empty;
                }
                else if (!createdTag.NodeGUID.HasValue && !createdTag.EdgeGUID.HasValue && _TagGraphPrimaryGuid == Guid.Empty)
                {
                    _TagGraphPrimaryGuid = createdTag.GUID;
                    _TagGraphPrimaryKey = createdTag.Key ?? string.Empty;
                    _TagGraphPrimaryValue = createdTag.Value ?? string.Empty;
                }
            }

            if (_TagEdgePrimaryGuid == Guid.Empty)
            {
                TagMetadata edgeTag = new TagMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    EdgeGUID = _EdgeGuidPrimary,
                    Key = "sdk-tag-edge-fallback",
                    Value = UniqueName("tag-edge-fallback")
                };

                TagMetadata? createdEdgeTag = await sdk.Tag.Create(edgeTag).ConfigureAwait(false);
                AssertNotNull(createdEdgeTag, "Fallback edge tag create");

                _TagEdgePrimaryGuid = createdEdgeTag!.GUID;
                _TagEdgePrimaryKey = createdEdgeTag.Key ?? string.Empty;
                _TagEdgePrimaryValue = createdEdgeTag.Value ?? string.Empty;
            }

            if (_TagGraphPrimaryGuid == Guid.Empty)
            {
                TagMetadata graphTag = new TagMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Key = "sdk-tag-graph-fallback",
                    Value = UniqueName("tag-graph-fallback")
                };

                TagMetadata? createdGraphTag = await sdk.Tag.Create(graphTag).ConfigureAwait(false);
                AssertNotNull(createdGraphTag, "Fallback graph tag create");

                _TagGraphPrimaryGuid = createdGraphTag!.GUID;
                _TagGraphPrimaryKey = createdGraphTag.Key ?? string.Empty;
                _TagGraphPrimaryValue = createdGraphTag.Value ?? string.Empty;
            }
        }

        private static async Task TestTagReadByGuid()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            TagMetadata? tag = await sdk.Tag.ReadByGuid(_TenantGuid, _TagNodePrimaryGuid).ConfigureAwait(false);

            AssertNotNull(tag, "Tag read result");
            AssertEqual(_TagNodePrimaryGuid, tag!.GUID, "Tag GUID match");
        }

        private static async Task TestTagExistsByGuid()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            bool exists = await sdk.Tag.ExistsByGuid(_TenantGuid, _TagNodePrimaryGuid).ConfigureAwait(false);
            AssertTrue(exists, "Tag should exist");

            bool missing = await sdk.Tag.ExistsByGuid(_TenantGuid, Guid.NewGuid()).ConfigureAwait(false);
            AssertFalse(missing, "Random tag should not exist");
        }

        private static async Task TestTagUpdate()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            TagMetadata? tag = await sdk.Tag.ReadByGuid(_TenantGuid, _TagNodePrimaryGuid).ConfigureAwait(false);
            AssertNotNull(tag, "Tag to update");

            string updatedValue = UniqueName("tag-node-value-updated");
            tag!.Value = updatedValue;

            TagMetadata? updated = await sdk.Tag.Update(tag).ConfigureAwait(false);

            AssertNotNull(updated, "Tag update result");
            AssertEqual(updatedValue, updated!.Value, "Updated tag value");

            _TagNodePrimaryValue = updated.Value ?? string.Empty;
        }

        private static async Task TestTagReadMany()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<TagMetadata>? tags = await sdk.Tag.ReadMany(_TenantGuid, _GraphGuid, _EdgeNode1Guid, null).ConfigureAwait(false);

            AssertNotNull(tags, "Tag read many");
            AssertTrue(tags!.Count > 0, "Tag read many count");
        }

        private static async Task TestTagReadByGuids()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<TagMetadata>? tags = await sdk.Tag.ReadByGuids(_TenantGuid, new List<Guid> { _TagNodePrimaryGuid, _TagEdgePrimaryGuid }).ConfigureAwait(false);

            AssertNotNull(tags, "Tags by GUIDs");
            AssertEqual(2, tags!.Count, "Tags by GUID count");
        }

        private static async Task TestTagReadAllInTenant()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<TagMetadata>? tags = await sdk.Tag.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);

            AssertNotNull(tags, "Tags in tenant");
            AssertTrue(tags!.Count > 0, "Tags in tenant count");
        }

        private static async Task TestTagReadAllInGraph()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<TagMetadata>? tags = await sdk.Tag.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(tags, "Tags in graph");
            AssertTrue(tags!.Count > 0, "Tags in graph count");
        }

        private static async Task TestTagReadManyGraph()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<TagMetadata>? tags = await sdk.Tag.ReadManyGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(tags, "Tags read many graph");
            AssertTrue(tags!.Any(t =>
                t.GraphGUID == _GraphGuid &&
                !t.NodeGUID.HasValue &&
                !t.EdgeGUID.HasValue &&
                string.Equals(t.Key, _TagGraphPrimaryKey, StringComparison.OrdinalIgnoreCase)), "Tags read many graph contains graph tag");
        }

        private static async Task TestTagReadManyNode()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<TagMetadata>? tags = await sdk.Tag.ReadManyNode(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);

            AssertNotNull(tags, "Tags read many node");
            AssertTrue(tags!.Any(t => t.GUID == _TagNodePrimaryGuid), "Tags read many node contains node tag");
        }

        private static async Task TestTagReadManyEdge()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<TagMetadata>? tags = await sdk.Tag.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);

            AssertNotNull(tags, "Tags read many edge");
            AssertTrue(tags!.Any(t =>
                t.EdgeGUID == _EdgeGuidPrimary &&
                string.Equals(t.Key, _TagEdgePrimaryKey, StringComparison.OrdinalIgnoreCase)), "Tags read many edge contains edge tag");
        }

        private static async Task TestTagEnumerate()
        {
            await EnsureTagTestDataAsync().ConfigureAwait(false);

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 5
            };

            LiteGraphSdk sdk = RequireSdk();
            EnumerationResult<TagMetadata>? result = await sdk.Tag.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Tag enumeration result");
            AssertTrue(result!.Objects.Count > 0, "Tag enumeration count");
        }

        #endregion

        #region Vector-Tests

        private static async Task TestVectorCreate()
        {
            await EnsureVectorDependenciesAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            VectorMetadata vector = BuildVectorMetadata(
                _NodePrimaryGuid,
                null,
                "primary node vector",
                new List<float> { 0.1f, 0.2f, 0.3f });

            VectorMetadata? created = await sdk.Vector.Create(vector).ConfigureAwait(false);

            AssertNotNull(created, "Vector create result");
            AssertNotEmpty(created!.GUID, "Vector GUID");

            _VectorNodePrimaryGuid = created.GUID;
            _VectorNodePrimaryContent = created.Content ?? string.Empty;
        }

        private static async Task TestVectorCreateMany()
        {
            await EnsureVectorDependenciesAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();

            List<VectorMetadata> vectors = new List<VectorMetadata>
            {
                BuildVectorMetadata(
                    _NodeSecondaryGuid,
                    null,
                    "secondary node vector",
                    new List<float> { 0.2f, 0.3f, 0.4f }),
                BuildVectorMetadata(
                    null,
                    _EdgeGuidPrimary,
                    "primary edge vector",
                    new List<float> { 0.4f, 0.3f, 0.2f })
            };

            List<VectorMetadata>? created = await sdk.Vector.CreateMany(_TenantGuid, vectors).ConfigureAwait(false);

            AssertNotNull(created, "Vector create many result");
            AssertEqual(2, created!.Count, "Vector create many count");

            _VectorNodeSecondaryGuid = created[0].GUID;
            _VectorNodeSecondaryContent = created[0].Content ?? string.Empty;

            _VectorEdgePrimaryGuid = created[1].GUID;
            _VectorEdgePrimaryContent = created[1].Content ?? string.Empty;
        }

        private static async Task TestVectorReadByGuid()
        {
            await EnsureVectorNodePrimaryAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            VectorMetadata? vector = await sdk.Vector.ReadByGuid(_TenantGuid, _VectorNodePrimaryGuid).ConfigureAwait(false);

            AssertNotNull(vector, "Vector read result");
            AssertEqual(_VectorNodePrimaryGuid, vector!.GUID, "Vector GUID match");
        }

        private static async Task TestVectorExistsByGuid()
        {
            await EnsureVectorNodePrimaryAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            bool exists = await sdk.Vector.ExistsByGuid(_TenantGuid, _VectorNodePrimaryGuid).ConfigureAwait(false);
            AssertTrue(exists, "Vector should exist");

            bool missing = await sdk.Vector.ExistsByGuid(_TenantGuid, Guid.NewGuid()).ConfigureAwait(false);
            AssertFalse(missing, "Random vector should not exist");
        }

        private static async Task TestVectorUpdate()
        {
            await EnsureVectorNodePrimaryAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            VectorMetadata? vector = await sdk.Vector.ReadByGuid(_TenantGuid, _VectorNodePrimaryGuid).ConfigureAwait(false);
            AssertNotNull(vector, "Vector to update");

            vector!.Content = "updated vector content";
            vector.Vectors = new List<float> { 0.3f, 0.2f, 0.1f };

            VectorMetadata? updated = await sdk.Vector.Update(vector).ConfigureAwait(false);

            AssertNotNull(updated, "Vector update result");
            AssertEqual("updated vector content", updated!.Content, "Updated vector content");

            _VectorNodePrimaryContent = updated.Content ?? string.Empty;
        }

        private static async Task TestVectorReadMany()
        {
            await EnsureVectorDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<VectorMetadata>? vectors = await sdk.Vector.ReadMany(_TenantGuid).ConfigureAwait(false);

            AssertNotNull(vectors, "Vector list");
            AssertTrue(vectors!.Count > 0, "Vector list count");
        }

        private static async Task TestVectorReadByGuids()
        {
            await EnsureVectorDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<VectorMetadata>? vectors = await sdk.Vector.ReadByGuids(_TenantGuid, new List<Guid> { _VectorNodePrimaryGuid, _VectorEdgePrimaryGuid }).ConfigureAwait(false);

            AssertNotNull(vectors, "Vectors by GUIDs");
            AssertEqual(2, vectors!.Count, "Vectors by GUID count");
        }

        private static async Task TestVectorReadAllInTenant()
        {
            await EnsureVectorDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<VectorMetadata>? vectors = await sdk.Vector.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);

            AssertNotNull(vectors, "Vectors in tenant");
            AssertTrue(vectors!.Count > 0, "Vectors in tenant count");
        }

        private static async Task TestVectorReadAllInGraph()
        {
            await EnsureVectorDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<VectorMetadata>? vectors = await sdk.Vector.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(vectors, "Vectors in graph");
            AssertTrue(vectors!.Count > 0, "Vectors in graph count");
        }

        private static async Task TestVectorReadManyGraph()
        {
            await EnsureVectorDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<VectorMetadata>? vectors = await sdk.Vector.ReadManyGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(vectors, "Vectors read many graph");
            AssertTrue(vectors!.Count > 0, "Vectors read many graph count");
        }

        private static async Task TestVectorReadManyNode()
        {
            await EnsureVectorNodePrimaryAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<VectorMetadata>? vectors = await sdk.Vector.ReadManyNode(_TenantGuid, _GraphGuid, _NodePrimaryGuid).ConfigureAwait(false);

            AssertNotNull(vectors, "Vectors read many node");
            AssertTrue(vectors!.Any(v => v.GUID == _VectorNodePrimaryGuid), "Vectors read many node contains primary vector");
        }

        private static async Task TestVectorReadManyEdge()
        {
            await EnsureVectorEdgePrimaryAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<VectorMetadata>? vectors = await sdk.Vector.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);

            AssertNotNull(vectors, "Vectors read many edge");
        }

        private static async Task TestVectorEnumerate()
        {
            await EnsureVectorDataAsync().ConfigureAwait(false);

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 5
            };

            LiteGraphSdk sdk = RequireSdk();
            EnumerationResult<VectorMetadata>? result = await sdk.Vector.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Vector enumeration result");
            AssertTrue(result!.Objects.Count > 0, "Vector enumeration count");
        }

        private static async Task TestVectorSearch()
        {
            await EnsureVectorDataAsync().ConfigureAwait(false);

            VectorSearchRequest request = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                TopK = 5,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f },
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity
            };

            LiteGraphSdk sdk = RequireSdk();
            List<VectorSearchResult>? results = await sdk.Vector.SearchVectors(_TenantGuid, _GraphGuid, request).ConfigureAwait(false);

            AssertNotNull(results, "Vector search results");
            AssertTrue(results!.Count >= 0, "Vector search results returned");
        }

        #endregion

        #region Node-Tests

        private static async Task TestNodeCreate()
        {
            AssertNotEmpty(_TenantGuid, "Tenant GUID for TestNodeCreate");
            AssertNotEmpty(_GraphGuid, "Graph GUID for TestNodeCreate");

            LiteGraphSdk sdk = RequireSdk();

            Node node = new Node
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Name = UniqueName("sdk-node-primary"),
                Data = new { description = "primary node" }
            };

            Node? created = await sdk.Node.Create(node).ConfigureAwait(false);

            AssertNotNull(created, "Node create result");
            AssertNotEmpty(created!.GUID, "Node GUID");
            AssertEqual(node.Name, created.Name, "Node name");

            _NodePrimaryGuid = created.GUID;
            _NodePrimaryName = created.Name ?? string.Empty;
            _NodeRelationshipsPrepared = false;
        }

        private static async Task TestNodeCreateMany()
        {
            AssertNotEmpty(_TenantGuid, "Tenant GUID for TestNodeCreateMany");
            AssertNotEmpty(_GraphGuid, "Graph GUID for TestNodeCreateMany");

            LiteGraphSdk sdk = RequireSdk();

            List<Node> nodes = new List<Node>
            {
                new Node
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Name = UniqueName("sdk-node-secondary"),
                    Data = new { description = "secondary node" }
                },
                new Node
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Name = UniqueName("sdk-node-tertiary"),
                    Data = new { description = "tertiary node" }
                }
            };

            List<Node>? created = await sdk.Node.CreateMany(_TenantGuid, _GraphGuid, nodes).ConfigureAwait(false);

            AssertNotNull(created, "Node create many result");
            AssertEqual(2, created!.Count, "Node create many count");

            _NodeSecondaryGuid = created[0].GUID;
            _NodeSecondaryName = created[0].Name ?? string.Empty;
            _NodeTertiaryGuid = created[1].GUID;
            _NodeTertiaryName = created[1].Name ?? string.Empty;
            _NodeRelationshipsPrepared = false;
        }

        private static async Task TestNodeReadByGuid()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            Node? node = await sdk.Node.ReadByGuid(_TenantGuid, _GraphGuid, _NodePrimaryGuid, includeData: true, includeSubordinates: false).ConfigureAwait(false);

            AssertNotNull(node, "Node read result");
            AssertEqual(_NodePrimaryGuid, node!.GUID, "Node GUID match");
        }

        private static async Task TestNodeExistsByGuid()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            bool exists = await sdk.Node.ExistsByGuid(_TenantGuid, _GraphGuid, _NodePrimaryGuid).ConfigureAwait(false);
            AssertTrue(exists, "Node should exist");

            bool missing = await sdk.Node.ExistsByGuid(_TenantGuid, _GraphGuid, Guid.NewGuid()).ConfigureAwait(false);
            AssertFalse(missing, "Random node should not exist");
        }

        private static async Task TestNodeUpdate()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            Node? node = await sdk.Node.ReadByGuid(_TenantGuid, _GraphGuid, _NodePrimaryGuid).ConfigureAwait(false);
            AssertNotNull(node, "Node to update");

            string updatedName = UniqueName("sdk-node-primary-updated");
            node!.Name = updatedName;
            node.Data = new { description = "updated node" };

            Node? updated = await sdk.Node.Update(node).ConfigureAwait(false);

            AssertNotNull(updated, "Node update result");
            AssertEqual(updatedName, updated!.Name, "Updated node name");

            _NodePrimaryName = updated.Name ?? string.Empty;
        }

        private static async Task TestNodeReadMany()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? nodes = await sdk.Node.ReadMany(_TenantGuid, _GraphGuid, includeData: true).ConfigureAwait(false);

            AssertNotNull(nodes, "Node list");
            AssertTrue(nodes!.Count > 0, "Node list count");
        }

        private static async Task TestNodeReadByGuids()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? nodes = await sdk.Node.ReadByGuids(_TenantGuid, _GraphGuid, new List<Guid> { _NodePrimaryGuid, _NodeSecondaryGuid }, includeData: true).ConfigureAwait(false);

            AssertNotNull(nodes, "Nodes by GUIDs");
            AssertEqual(2, nodes!.Count, "Nodes by GUID count");
        }

        private static async Task TestNodeReadParents()
        {
            await EnsureNodeRelationshipsAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? parents = await sdk.Node.ReadParents(_TenantGuid, _GraphGuid, _NodeSecondaryGuid).ConfigureAwait(false);

            AssertNotNull(parents, "Node parents");
            AssertTrue(parents!.Any(n => n.GUID == _NodePrimaryGuid), "Parents contain primary node");
        }

        private static async Task TestNodeReadChildren()
        {
            await EnsureNodeRelationshipsAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? children = await sdk.Node.ReadChildren(_TenantGuid, _GraphGuid, _NodePrimaryGuid).ConfigureAwait(false);

            AssertNotNull(children, "Node children");
            AssertTrue(children!.Any(n => n.GUID == _NodeSecondaryGuid), "Children contain secondary node");
        }

        private static async Task TestNodeReadNeighbors()
        {
            await EnsureNodeRelationshipsAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? neighbors = await sdk.Node.ReadNeighbors(_TenantGuid, _GraphGuid, _NodePrimaryGuid).ConfigureAwait(false);

            AssertNotNull(neighbors, "Node neighbors");
            AssertTrue(neighbors!.Count > 0, "Node neighbors count");
        }

        private static async Task TestNodeReadRoutes()
        {
            await EnsureNodeRelationshipsAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            RouteResponse? routes = await sdk.Node.ReadRoutes(
                SearchTypeEnum.DepthFirstSearch,
                _TenantGuid,
                _GraphGuid,
                _NodePrimaryGuid,
                _NodeTertiaryGuid).ConfigureAwait(false);

            AssertNotNull(routes, "Node routes");
            AssertTrue(routes!.Routes != null && routes.Routes.Count > 0, "Node routes count");
        }

        private static async Task TestNodeReadAllInTenant()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? nodes = await sdk.Node.ReadAllInTenant(_TenantGuid, includeData: true).ConfigureAwait(false);

            AssertNotNull(nodes, "Nodes in tenant");
            AssertTrue(nodes!.Count > 0, "Nodes in tenant count");
        }

        private static async Task TestNodeReadAllInGraph()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? nodes = await sdk.Node.ReadAllInGraph(_TenantGuid, _GraphGuid, includeData: true).ConfigureAwait(false);

            AssertNotNull(nodes, "Nodes in graph");
            AssertTrue(nodes!.Count > 0, "Nodes in graph count");
        }

        private static async Task TestNodeReadMostConnected()
        {
            await EnsureNodeRelationshipsAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? nodes = await sdk.Node.ReadMostConnected(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(nodes, "Most connected nodes");
            AssertTrue(nodes!.Count > 0, "Most connected nodes count");
        }

        private static async Task TestNodeReadLeastConnected()
        {
            await EnsureNodeRelationshipsAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            List<Node>? nodes = await sdk.Node.ReadLeastConnected(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(nodes, "Least connected nodes");
            AssertTrue(nodes!.Count > 0, "Least connected nodes count");
        }

        private static async Task TestNodeSearch()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            SearchRequest request = new SearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Name = _NodePrimaryName
            };

            LiteGraphSdk sdk = RequireSdk();
            SearchResult? result = await sdk.Node.Search(request).ConfigureAwait(false);

            AssertNotNull(result, "Node search result");
            AssertNotNull(result!.Nodes, "Node search nodes");
            AssertTrue(result.Nodes!.Any(n => n.GUID == _NodePrimaryGuid), "Node search contains target");
        }

        private static async Task TestNodeReadFirst()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            SearchRequest request = new SearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Name = _NodePrimaryName
            };

            LiteGraphSdk sdk = RequireSdk();
            Node? node = await sdk.Node.ReadFirst(request).ConfigureAwait(false);

            AssertNotNull(node, "Node read first");
            AssertEqual(_NodePrimaryGuid, node!.GUID, "Node read first GUID");
        }

        private static async Task TestNodeEnumerate()
        {
            await EnsureNodeTestDataAsync().ConfigureAwait(false);

            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 5
            };

            LiteGraphSdk sdk = RequireSdk();
            EnumerationResult<Node>? result = await sdk.Node.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Node enumeration result");
            AssertTrue(result!.Objects.Count > 0, "Node enumeration count");
        }

        #endregion

        #region Label-Tests

        private static async Task TestLabelCreate()
        {
            await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

            LiteGraphSdk sdk = RequireSdk();
            string labelValue = UniqueName("sdk-label-node");

            LabelMetadata label = new LabelMetadata
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                NodeGUID = _EdgeNode1Guid,
                Label = labelValue
            };

            LabelMetadata? created = await sdk.Label.Create(label).ConfigureAwait(false);

            AssertNotNull(created, "Label create result");
            AssertNotEmpty(created!.GUID, "Label GUID");
            AssertEqual(labelValue, created.Label, "Label value");

            _LabelNodePrimaryGuid = created.GUID;
            _LabelNodePrimaryName = created.Label ?? string.Empty;
        }

        private static async Task TestLabelCreateMany()
        {
            AssertNotEmpty(_EdgeGuidPrimary, "Edge GUID for TestLabelCreateMany");

            LiteGraphSdk sdk = RequireSdk();

            List<LabelMetadata> labels = new List<LabelMetadata>
            {
                new LabelMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    EdgeGUID = _EdgeGuidPrimary,
                    Label = UniqueName("sdk-label-edge")
                },
                new LabelMetadata
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Label = UniqueName("sdk-label-graph")
                }
            };

            List<LabelMetadata>? created = await sdk.Label.CreateMany(_TenantGuid, labels).ConfigureAwait(false);

            AssertNotNull(created, "Label create many result");
            AssertEqual(2, created!.Count, "Label create many count");

            _LabelEdgePrimaryGuid = created[0].GUID;
            _LabelEdgePrimaryName = created[0].Label ?? string.Empty;

            _LabelGraphPrimaryGuid = created[1].GUID;
            _LabelGraphPrimaryName = created[1].Label ?? string.Empty;
        }

        private static async Task TestLabelReadByGuid()
        {
            AssertNotEmpty(_LabelNodePrimaryGuid, "Label GUID for TestLabelReadByGuid");

            LiteGraphSdk sdk = RequireSdk();
            LabelMetadata? label = await sdk.Label.ReadByGuid(_TenantGuid, _LabelNodePrimaryGuid).ConfigureAwait(false);

            AssertNotNull(label, "Label read result");
            AssertEqual(_LabelNodePrimaryGuid, label!.GUID, "Label GUID match");
        }

        private static async Task TestLabelExistsByGuid()
        {
            AssertNotEmpty(_LabelNodePrimaryGuid, "Label GUID for TestLabelExistsByGuid");

            LiteGraphSdk sdk = RequireSdk();
            bool exists = await sdk.Label.ExistsByGuid(_TenantGuid, _LabelNodePrimaryGuid).ConfigureAwait(false);
            AssertTrue(exists, "Label should exist");

            bool missing = await sdk.Label.ExistsByGuid(_TenantGuid, Guid.NewGuid()).ConfigureAwait(false);
            AssertFalse(missing, "Random label should not exist");
        }

        private static async Task TestLabelUpdate()
        {
            AssertNotEmpty(_LabelNodePrimaryGuid, "Label GUID for TestLabelUpdate");

            LiteGraphSdk sdk = RequireSdk();
            LabelMetadata? label = await sdk.Label.ReadByGuid(_TenantGuid, _LabelNodePrimaryGuid).ConfigureAwait(false);
            AssertNotNull(label, "Label to update");

            string updatedValue = UniqueName("sdk-label-node-updated");
            label!.Label = updatedValue;

            LabelMetadata? updated = await sdk.Label.Update(label).ConfigureAwait(false);

            AssertNotNull(updated, "Label update result");
            AssertEqual(updatedValue, updated!.Label, "Updated label value");

            _LabelNodePrimaryName = updated.Label ?? string.Empty;
        }

        private static async Task TestLabelReadMany()
        {
            LiteGraphSdk sdk = RequireSdk();
            List<LabelMetadata>? labels = await sdk.Label.ReadMany(_TenantGuid).ConfigureAwait(false);

            AssertNotNull(labels, "Label list");
            AssertTrue(labels!.Count > 0, "Label list count");
        }

        private static async Task TestLabelReadByGuids()
        {
            AssertNotEmpty(_LabelNodePrimaryGuid, "Label node GUID for TestLabelReadByGuids");
            AssertNotEmpty(_LabelEdgePrimaryGuid, "Label edge GUID for TestLabelReadByGuids");

            LiteGraphSdk sdk = RequireSdk();
            List<LabelMetadata>? labels = await sdk.Label.ReadByGuids(_TenantGuid, new List<Guid> { _LabelNodePrimaryGuid, _LabelEdgePrimaryGuid }).ConfigureAwait(false);

            AssertNotNull(labels, "Labels by GUIDs");
            AssertEqual(2, labels!.Count, "Labels by GUID count");
        }

        private static async Task TestLabelReadAllInTenant()
        {
            LiteGraphSdk sdk = RequireSdk();
            List<LabelMetadata>? labels = await sdk.Label.ReadAllInTenant(_TenantGuid).ConfigureAwait(false);

            AssertNotNull(labels, "Labels in tenant");
            AssertTrue(labels!.Count > 0, "Labels in tenant count");
        }

        private static async Task TestLabelReadAllInGraph()
        {
            LiteGraphSdk sdk = RequireSdk();
            List<LabelMetadata>? labels = await sdk.Label.ReadAllInGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(labels, "Labels in graph");
            AssertTrue(labels!.Count > 0, "Labels in graph count");
        }

        private static async Task TestLabelReadManyGraph()
        {
            LiteGraphSdk sdk = RequireSdk();
            List<LabelMetadata>? labels = await sdk.Label.ReadManyGraph(_TenantGuid, _GraphGuid).ConfigureAwait(false);

            AssertNotNull(labels, "Labels read many graph");
            AssertTrue(labels!.Count > 0, "Labels read many graph count");
        }

        private static async Task TestLabelReadManyNode()
        {
            AssertNotEmpty(_EdgeNode1Guid, "Node GUID for TestLabelReadManyNode");

            LiteGraphSdk sdk = RequireSdk();
            List<LabelMetadata>? labels = await sdk.Label.ReadManyNode(_TenantGuid, _GraphGuid, _EdgeNode1Guid).ConfigureAwait(false);

            AssertNotNull(labels, "Labels read many node");
            AssertTrue(labels!.Any(l => l.GUID == _LabelNodePrimaryGuid), "Labels read many node contains target");
        }

        private static async Task TestLabelReadManyEdge()
        {
            AssertNotEmpty(_EdgeGuidPrimary, "Edge GUID for TestLabelReadManyEdge");

            LiteGraphSdk sdk = RequireSdk();
            List<LabelMetadata>? labels = await sdk.Label.ReadManyEdge(_TenantGuid, _GraphGuid, _EdgeGuidPrimary).ConfigureAwait(false);

            AssertNotNull(labels, "Labels read many edge");
        }

        private static async Task TestLabelEnumerate()
        {
            EnumerationRequest request = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                MaxResults = 5
            };

            LiteGraphSdk sdk = RequireSdk();
            EnumerationResult<LabelMetadata>? result = await sdk.Label.Enumerate(request).ConfigureAwait(false);

            AssertNotNull(result, "Label enumeration result");
            AssertTrue(result!.Objects.Count > 0, "Label enumeration count");
        }

        #endregion

        #region Helpers

        private static void ApplyArgs(string[] args)
		{
			if (args == null || args.Length == 0) return;

			foreach (string raw in args)
			{
				if (string.IsNullOrWhiteSpace(raw)) continue;
				string arg = raw.Trim();

				if (arg.StartsWith("--endpoint=", StringComparison.OrdinalIgnoreCase))
				{
					_Endpoint = arg.Substring("--endpoint=".Length);
				}
				else if (arg.StartsWith("--token=", StringComparison.OrdinalIgnoreCase))
				{
					_BearerToken = arg.Substring("--token=".Length);
				}
				else if (arg.Equals("--verbose", StringComparison.OrdinalIgnoreCase))
				{
					_VerboseLogging = true;
				}
			}
		}

		private static void PrintBanner()
		{
			Console.WriteLine("==============================================");
			Console.WriteLine("  LiteGraph SDK Automated Tests");
			Console.WriteLine("==============================================");
			Console.WriteLine($"Endpoint     : {_Endpoint}");
			Console.WriteLine($"Bearer Token : {MaskToken(_BearerToken)}");
			Console.WriteLine($"Verbose Logs : {_VerboseLogging}");
			Console.WriteLine("");
		}

		private static void PrintSummary()
		{
			Console.WriteLine("");
			Console.WriteLine("==============================================");
			Console.WriteLine("  Test Summary");
			Console.WriteLine("==============================================");

			foreach (TestResult result in _TestResults)
			{
				string status = result.Passed ? "PASS" : "FAIL";
				Console.WriteLine($"[{status}] {result.Name,-35} {result.RuntimeMs,6}ms");
			}

			int passed = _TestResults.Count(r => r.Passed);
			int failed = _TestResults.Count - passed;

			Console.WriteLine("----------------------------------------------");
			Console.WriteLine($"Total : {_TestResults.Count}");
			Console.WriteLine($"Pass  : {passed}");
			Console.WriteLine($"Fail  : {failed}");
			Console.WriteLine("==============================================");

			if (failed > 0)
			{
				Console.WriteLine("Failed Tests:");
				foreach (TestResult result in _TestResults.Where(r => !r.Passed))
				{
					Console.WriteLine($"  - {result.Name}: {result.ErrorMessage}");
				}
			}

			Console.WriteLine("");
		}

		private static LiteGraphSdk RequireSdk()
		{
			return _Sdk ?? throw new InvalidOperationException("SDK is not initialized");
		}

		private static string UniqueName(string prefix)
		{
			return $"{prefix}-{Guid.NewGuid():N}";
		}

		private static string UniqueEmail()
		{
			return $"sdk.user.{Guid.NewGuid():N}@example.com";
		}

		private static string MaskToken(string token)
		{
			if (string.IsNullOrEmpty(token)) return "(empty)";
			if (token.Length <= 6) return new string('*', token.Length);

			return token.Substring(0, 3) + new string('*', token.Length - 6) + token.Substring(token.Length - 3);
		}

		private static async Task<string> EnsureBackupAsync(bool forceNew = false)
		{
			LiteGraphSdk sdk = RequireSdk();

			if (!forceNew && !string.IsNullOrEmpty(_BackupFilename))
			{
				bool exists = await sdk.Admin.BackupExists(_BackupFilename).ConfigureAwait(false);
				if (exists)
				{
					return _BackupFilename;
				}
			}

			string filename = $"sdk-backup-{Guid.NewGuid():N}.bak";
			await sdk.Admin.Backup(filename).ConfigureAwait(false);
			_BackupFilename = filename;
			return filename;
		}

		private static async Task<Guid> EnsureSubgraphScenarioAsync()
		{
			if (_SubgraphPrepared && _SubgraphRootNodeGuid != Guid.Empty)
			{
				return _SubgraphRootNodeGuid;
			}

			AssertNotEmpty(_TenantGuid, "Tenant GUID for EnsureSubgraphScenarioAsync");
			AssertNotEmpty(_GraphGuid, "Graph GUID for EnsureSubgraphScenarioAsync");

			LiteGraphSdk sdk = RequireSdk();

			_SubgraphNodeGuids.Clear();
			_SubgraphEdgeInfos.Clear();

			// Build node hierarchy
			Node nodeA = await sdk.Node.Create(new Node
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = "Node A (Root)",
				Data = new { Type = "Root", Level = 0 }
			}).ConfigureAwait(false);
			_SubgraphNodeGuids.Add(nodeA.GUID);

			Node nodeB = await sdk.Node.Create(new Node
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = "Node B (Layer 1)",
				Data = new { Type = "Layer1", Level = 1 }
			}).ConfigureAwait(false);
			_SubgraphNodeGuids.Add(nodeB.GUID);

			Node nodeC = await sdk.Node.Create(new Node
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = "Node C (Layer 1)",
				Data = new { Type = "Layer1", Level = 1 }
			}).ConfigureAwait(false);
			_SubgraphNodeGuids.Add(nodeC.GUID);

			Node nodeD = await sdk.Node.Create(new Node
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = "Node D (Layer 2)",
				Data = new { Type = "Layer2", Level = 2 }
			}).ConfigureAwait(false);
			_SubgraphNodeGuids.Add(nodeD.GUID);

			Node nodeE = await sdk.Node.Create(new Node
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = "Node E (Layer 2)",
				Data = new { Type = "Layer2", Level = 2 }
			}).ConfigureAwait(false);
			_SubgraphNodeGuids.Add(nodeE.GUID);

			Node nodeF = await sdk.Node.Create(new Node
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = "Node F (Layer 2)",
				Data = new { Type = "Layer2", Level = 2 }
			}).ConfigureAwait(false);
			_SubgraphNodeGuids.Add(nodeF.GUID);

			Node nodeG = await sdk.Node.Create(new Node
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				Name = "Node G (Layer 3)",
				Data = new { Type = "Layer3", Level = 3 }
			}).ConfigureAwait(false);
			_SubgraphNodeGuids.Add(nodeG.GUID);

			// Connect nodes with edges (including a back edge for traversal coverage)
			Edge edgeAB = await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = nodeA.GUID,
				To = nodeB.GUID,
				Name = "A -> B",
				Cost = 1
			}).ConfigureAwait(false);
			_SubgraphEdgeInfos.Add(new SubgraphEdgeInfo(edgeAB.GUID, nodeA.GUID, nodeB.GUID));

			Edge edgeAC = await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = nodeA.GUID,
				To = nodeC.GUID,
				Name = "A -> C",
				Cost = 1
			}).ConfigureAwait(false);
			_SubgraphEdgeInfos.Add(new SubgraphEdgeInfo(edgeAC.GUID, nodeA.GUID, nodeC.GUID));

			Edge edgeBD = await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = nodeB.GUID,
				To = nodeD.GUID,
				Name = "B -> D",
				Cost = 1
			}).ConfigureAwait(false);
			_SubgraphEdgeInfos.Add(new SubgraphEdgeInfo(edgeBD.GUID, nodeB.GUID, nodeD.GUID));

			Edge edgeBE = await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = nodeB.GUID,
				To = nodeE.GUID,
				Name = "B -> E",
				Cost = 1
			}).ConfigureAwait(false);
			_SubgraphEdgeInfos.Add(new SubgraphEdgeInfo(edgeBE.GUID, nodeB.GUID, nodeE.GUID));

			Edge edgeCF = await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = nodeC.GUID,
				To = nodeF.GUID,
				Name = "C -> F",
				Cost = 1
			}).ConfigureAwait(false);
			_SubgraphEdgeInfos.Add(new SubgraphEdgeInfo(edgeCF.GUID, nodeC.GUID, nodeF.GUID));

			Edge edgeDG = await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = nodeD.GUID,
				To = nodeG.GUID,
				Name = "D -> G",
				Cost = 1
			}).ConfigureAwait(false);
			_SubgraphEdgeInfos.Add(new SubgraphEdgeInfo(edgeDG.GUID, nodeD.GUID, nodeG.GUID));

			Edge edgeCA = await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = nodeC.GUID,
				To = nodeA.GUID,
				Name = "C -> A (back edge)",
				Cost = 1
			}).ConfigureAwait(false);
			_SubgraphEdgeInfos.Add(new SubgraphEdgeInfo(edgeCA.GUID, nodeC.GUID, nodeA.GUID));

			// Labels for nodes and a primary edge
			await sdk.Label.Create(new LabelMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeA.GUID,
				Label = "root-node"
			}).ConfigureAwait(false);

			await sdk.Label.Create(new LabelMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeA.GUID,
				Label = "level-0"
			}).ConfigureAwait(false);

			await sdk.Label.Create(new LabelMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeB.GUID,
				Label = "layer-1"
			}).ConfigureAwait(false);

			await sdk.Label.Create(new LabelMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeC.GUID,
				Label = "layer-1"
			}).ConfigureAwait(false);

			await sdk.Label.Create(new LabelMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				EdgeGUID = edgeAB.GUID,
				Label = "primary-edge"
			}).ConfigureAwait(false);

			// Tags for nodes and an edge
			await sdk.Tag.Create(new TagMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeA.GUID,
				Key = "type",
				Value = "root"
			}).ConfigureAwait(false);

			await sdk.Tag.Create(new TagMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeA.GUID,
				Key = "category",
				Value = "start"
			}).ConfigureAwait(false);

			await sdk.Tag.Create(new TagMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeB.GUID,
				Key = "type",
				Value = "child"
			}).ConfigureAwait(false);

			await sdk.Tag.Create(new TagMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				EdgeGUID = edgeAC.GUID,
				Key = "weight",
				Value = "1"
			}).ConfigureAwait(false);

			// Vectors for nodes and an edge
			await sdk.Vector.Create(new VectorMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeA.GUID,
				Model = "test-model",
				Dimensionality = 3,
				Content = "root node embedding",
				Vectors = new List<float> { 0.1f, 0.2f, 0.3f }
			}).ConfigureAwait(false);

			await sdk.Vector.Create(new VectorMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeB.GUID,
				Model = "test-model",
				Dimensionality = 3,
				Content = "child node embedding",
				Vectors = new List<float> { 0.4f, 0.5f, 0.6f }
			}).ConfigureAwait(false);

			await sdk.Vector.Create(new VectorMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				EdgeGUID = edgeBD.GUID,
				Model = "test-model",
				Dimensionality = 3,
				Content = "edge embedding",
				Vectors = new List<float> { 0.7f, 0.8f, 0.9f }
			}).ConfigureAwait(false);

			_SubgraphRootNodeGuid = nodeA.GUID;
			_SubgraphPrepared = true;
			return _SubgraphRootNodeGuid;
		}

		private static async Task EnsureNodeTestDataAsync()
		{
			LiteGraphSdk sdk = RequireSdk();

			if (_NodePrimaryGuid == Guid.Empty)
			{
				Node node = new Node
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					Name = UniqueName("sdk-node-primary-helper"),
					Data = new { description = "helper primary node" }
				};

				Node? created = await sdk.Node.Create(node).ConfigureAwait(false);
				AssertNotNull(created, "Helper node create result");

				_NodePrimaryGuid = created!.GUID;
				_NodePrimaryName = created.Name ?? string.Empty;
				_NodeRelationshipsPrepared = false;
			}

			List<Node> nodesToCreate = new List<Node>();
			if (_NodeSecondaryGuid == Guid.Empty)
			{
				nodesToCreate.Add(new Node
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					Name = UniqueName("sdk-node-secondary-helper"),
					Data = new { description = "helper secondary node" }
				});
			}

			if (_NodeTertiaryGuid == Guid.Empty)
			{
				nodesToCreate.Add(new Node
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					Name = UniqueName("sdk-node-tertiary-helper"),
					Data = new { description = "helper tertiary node" }
				});
			}

			if (nodesToCreate.Count > 0)
			{
				List<Node>? created = await sdk.Node.CreateMany(_TenantGuid, _GraphGuid, nodesToCreate).ConfigureAwait(false);
				AssertNotNull(created, "Helper node create many result");

				foreach (Node createdNode in created!)
				{
					if (_NodeSecondaryGuid == Guid.Empty)
					{
						_NodeSecondaryGuid = createdNode.GUID;
						_NodeSecondaryName = createdNode.Name ?? string.Empty;
					}
					else if (_NodeTertiaryGuid == Guid.Empty)
					{
						_NodeTertiaryGuid = createdNode.GUID;
						_NodeTertiaryName = createdNode.Name ?? string.Empty;
					}
				}

				_NodeRelationshipsPrepared = false;
			}
		}

		private static async Task EnsureNodeRelationshipsAsync()
		{
			await EnsureNodeTestDataAsync().ConfigureAwait(false);
			if (_NodeRelationshipsPrepared) return;

			LiteGraphSdk sdk = RequireSdk();

			await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = _NodePrimaryGuid,
				To = _NodeSecondaryGuid,
				Name = UniqueName("node-edge-primary-secondary"),
				Cost = 1
			}).ConfigureAwait(false);

			await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = _NodeSecondaryGuid,
				To = _NodeTertiaryGuid,
				Name = UniqueName("node-edge-secondary-tertiary"),
				Cost = 1
			}).ConfigureAwait(false);

			await sdk.Edge.Create(new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = _NodePrimaryGuid,
				To = _NodeTertiaryGuid,
				Name = UniqueName("node-edge-primary-tertiary"),
				Cost = 1
			}).ConfigureAwait(false);

			_NodeRelationshipsPrepared = true;
		}

		private static async Task EnsureVectorDependenciesAsync()
		{
			await EnsureNodeTestDataAsync().ConfigureAwait(false);
			await EnsureEdgePrimaryAsync().ConfigureAwait(false);
		}

		private static async Task EnsureVectorNodePrimaryAsync()
		{
			if (_VectorNodePrimaryGuid != Guid.Empty) return;

			await EnsureVectorDependenciesAsync().ConfigureAwait(false);
			LiteGraphSdk sdk = RequireSdk();

			VectorMetadata vector = BuildVectorMetadata(_NodePrimaryGuid, null, "helper node primary vector", new List<float> { 0.1f, 0.2f, 0.3f });
			VectorMetadata? created = await sdk.Vector.Create(vector).ConfigureAwait(false);

			AssertNotNull(created, "Helper node vector create result");
			_VectorNodePrimaryGuid = created!.GUID;
			_VectorNodePrimaryContent = created.Content ?? string.Empty;
		}

		private static async Task EnsureVectorNodeSecondaryAsync()
		{
			if (_VectorNodeSecondaryGuid != Guid.Empty) return;

			await EnsureVectorDependenciesAsync().ConfigureAwait(false);
			LiteGraphSdk sdk = RequireSdk();

			VectorMetadata vector = BuildVectorMetadata(_NodeSecondaryGuid, null, "helper node secondary vector", new List<float> { 0.2f, 0.3f, 0.4f });
			VectorMetadata? created = await sdk.Vector.Create(vector).ConfigureAwait(false);

			AssertNotNull(created, "Helper node secondary vector create result");
			_VectorNodeSecondaryGuid = created!.GUID;
			_VectorNodeSecondaryContent = created.Content ?? string.Empty;
		}

		private static async Task EnsureVectorEdgePrimaryAsync()
		{
			if (_VectorEdgePrimaryGuid != Guid.Empty) return;

			await EnsureEdgePrimaryAsync().ConfigureAwait(false);
			LiteGraphSdk sdk = RequireSdk();

			VectorMetadata vector = BuildVectorMetadata(null, _EdgeGuidPrimary, "helper edge vector", new List<float> { 0.3f, 0.2f, 0.1f });
			VectorMetadata? created = await sdk.Vector.Create(vector).ConfigureAwait(false);

			AssertNotNull(created, "Helper edge vector create result");
			_VectorEdgePrimaryGuid = created!.GUID;
			_VectorEdgePrimaryContent = created.Content ?? string.Empty;
		}

		private static async Task EnsureVectorDataAsync()
		{
			await EnsureVectorDependenciesAsync().ConfigureAwait(false);
			await EnsureVectorNodePrimaryAsync().ConfigureAwait(false);
			await EnsureVectorNodeSecondaryAsync().ConfigureAwait(false);
			await EnsureVectorEdgePrimaryAsync().ConfigureAwait(false);
		}

		private static VectorMetadata BuildVectorMetadata(Guid? nodeGuid, Guid? edgeGuid, string content, List<float> vectors)
		{
			return new VectorMetadata
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				NodeGUID = nodeGuid,
				EdgeGUID = edgeGuid,
				Model = "sdk-vector-model",
				Dimensionality = vectors?.Count ?? 0,
				Content = content,
				Vectors = vectors
			};
		}

		private static async Task EnsureUserAuthTokenAsync()
		{
			if (!string.IsNullOrEmpty(_UserAuthToken)) return;

			AssertFalse(string.IsNullOrEmpty(_UserEmail), "User email for auth token ensure");
			AssertFalse(string.IsNullOrEmpty(_UserPassword), "User password for auth token ensure");

			using LiteGraphSdk authSdk = new LiteGraphSdk(_UserEmail, _UserPassword, _TenantGuid, _Endpoint, _BearerToken);
			AuthenticationToken? token = await authSdk.UserAuthentication.GenerateToken().ConfigureAwait(false);
			AssertNotNull(token, "Helper auth token");

			_UserAuthToken = token!.Token ?? string.Empty;
		}

		private static async Task EnsureTagTestDataAsync()
		{
			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);
			await EnsureEdgePrimaryAsync().ConfigureAwait(false);
			
			LiteGraphSdk sdk = RequireSdk();

			if (_TagNodePrimaryGuid == Guid.Empty)
			{
				TagMetadata tag = new TagMetadata
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					NodeGUID = _EdgeNode1Guid,
					Key = "node-tag",
					Value = UniqueName("node-tag-value")
				};

				TagMetadata? created = await sdk.Tag.Create(tag).ConfigureAwait(false);
				AssertNotNull(created, "Helper node tag create result");

				_TagNodePrimaryGuid = created!.GUID;
				_TagNodePrimaryKey = created.Key ?? string.Empty;
				_TagNodePrimaryValue = created.Value ?? string.Empty;
			}

			if (_TagEdgePrimaryGuid == Guid.Empty && _EdgeGuidPrimary != Guid.Empty)
			{
				TagMetadata tag = new TagMetadata
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					EdgeGUID = _EdgeGuidPrimary,
					Key = "edge-tag",
					Value = UniqueName("edge-tag-value")
				};

				TagMetadata? created = await sdk.Tag.Create(tag).ConfigureAwait(false);
				AssertNotNull(created, "Helper edge tag create result");

				_TagEdgePrimaryGuid = created!.GUID;
				_TagEdgePrimaryKey = created.Key ?? string.Empty;
				_TagEdgePrimaryValue = created.Value ?? string.Empty;
			}

			if (_TagGraphPrimaryGuid == Guid.Empty)
			{
				TagMetadata tag = new TagMetadata
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					Key = "graph-tag",
					Value = UniqueName("graph-tag-value")
				};

				TagMetadata? created = await sdk.Tag.Create(tag).ConfigureAwait(false);
				AssertNotNull(created, "Helper graph tag create result");

				_TagGraphPrimaryGuid = created!.GUID;
				_TagGraphPrimaryKey = created.Key ?? string.Empty;
				_TagGraphPrimaryValue = created.Value ?? string.Empty;
			}
		}

		private static async Task EnsureEdgeTestNodesAsync()
		{
			if (_EdgeNode1Guid != Guid.Empty && _EdgeNode2Guid != Guid.Empty && _EdgeNode3Guid != Guid.Empty)
			{
				return;
			}

			AssertNotEmpty(_TenantGuid, "Tenant GUID for EnsureEdgeTestNodesAsync");
			AssertNotEmpty(_GraphGuid, "Graph GUID for EnsureEdgeTestNodesAsync");

			LiteGraphSdk sdk = RequireSdk();

			if (_EdgeNode1Guid == Guid.Empty)
			{
				Node node = await sdk.Node.Create(new Node
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					Name = UniqueName("edge-node-1"),
					Data = new { type = "edge-test", role = "source" }
				}).ConfigureAwait(false);
				_EdgeNode1Guid = node.GUID;
			}

			if (_EdgeNode2Guid == Guid.Empty)
			{
				Node node = await sdk.Node.Create(new Node
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					Name = UniqueName("edge-node-2"),
					Data = new { type = "edge-test", role = "target" }
				}).ConfigureAwait(false);
				_EdgeNode2Guid = node.GUID;
			}

			if (_EdgeNode3Guid == Guid.Empty)
			{
				Node node = await sdk.Node.Create(new Node
				{
					TenantGUID = _TenantGuid,
					GraphGUID = _GraphGuid,
					Name = UniqueName("edge-node-3"),
					Data = new { type = "edge-test", role = "aux" }
				}).ConfigureAwait(false);
				_EdgeNode3Guid = node.GUID;
			}
		}

		private static async Task EnsureEdgePrimaryAsync()
		{
			await EnsureEdgeTestNodesAsync().ConfigureAwait(false);

			if (_EdgeGuidPrimary != Guid.Empty)
			{
				return;
			}

			LiteGraphSdk sdk = RequireSdk();
			string edgeName = UniqueName("sdk-edge-helper");

			Edge edge = new Edge
			{
				TenantGUID = _TenantGuid,
				GraphGUID = _GraphGuid,
				From = _EdgeNode1Guid,
				To = _EdgeNode2Guid,
				Name = edgeName,
				Cost = 1,
				Data = new { description = "helper edge" }
			};

			Edge? created = await sdk.Edge.Create(edge).ConfigureAwait(false);
			AssertNotNull(created, "Helper edge create result");

			_EdgeGuidPrimary = created!.GUID;
			_EdgeNamePrimary = created.Name ?? string.Empty;
		}

		private static async Task CleanupTestDataAsync()
		{
			try
			{
				LiteGraphSdk sdk = RequireSdk();

				if (!string.IsNullOrEmpty(_BackupFilename))
				{
					try
					{
						if (await sdk.Admin.BackupExists(_BackupFilename).ConfigureAwait(false))
						{
							await sdk.Admin.DeleteBackup(_BackupFilename).ConfigureAwait(false);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Warning: Backup cleanup failed for '{_BackupFilename}': {ex.Message}");
					}
					finally
					{
						_BackupFilename = null;
					}
				}

				foreach (Guid tenantId in _CreatedTenantGuids.ToList())
				{
					try
					{
						await sdk.Tenant.DeleteByGuid(tenantId, true).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Warning: Failed to delete tenant {tenantId}: {ex.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Warning: Cleanup encountered an error: {ex.Message}");
			}
			finally
			{
				ResetTestState();
			}
		}

		private static void ResetTestState()
		{
			_TenantGuid = Guid.Empty;
			_GraphGuid = Guid.Empty;
			_UserGuid = Guid.Empty;
			_CredentialGuid = Guid.Empty;
			_SubgraphRootNodeGuid = Guid.Empty;
			_TenantName = string.Empty;
			_GraphName = string.Empty;
			_UserEmail = string.Empty;
			_CredentialName = string.Empty;
			_CredentialBearerToken = string.Empty;
			_BackupFilename = null;
			_SubgraphPrepared = false;
			_SubgraphNodeGuids.Clear();
			_SubgraphEdgeInfos.Clear();
			_EdgeGuidPrimary = Guid.Empty;
			_EdgeGuidSecondary = Guid.Empty;
			_EdgeNode1Guid = Guid.Empty;
			_EdgeNode2Guid = Guid.Empty;
			_EdgeNode3Guid = Guid.Empty;
			_EdgeNamePrimary = string.Empty;
			_EdgeNameSecondary = string.Empty;
			_NodePrimaryGuid = Guid.Empty;
			_NodeSecondaryGuid = Guid.Empty;
			_NodeTertiaryGuid = Guid.Empty;
			_NodePrimaryName = string.Empty;
			_NodeSecondaryName = string.Empty;
			_NodeTertiaryName = string.Empty;
			_NodeRelationshipsPrepared = false;
			_CreatedTenantGuids.Clear();
			_LabelNodePrimaryGuid = Guid.Empty;
			_LabelEdgePrimaryGuid = Guid.Empty;
			_LabelGraphPrimaryGuid = Guid.Empty;
			_LabelNodePrimaryName = string.Empty;
			_LabelEdgePrimaryName = string.Empty;
			_LabelGraphPrimaryName = string.Empty;
			_TagNodePrimaryGuid = Guid.Empty;
			_TagEdgePrimaryGuid = Guid.Empty;
			_TagGraphPrimaryGuid = Guid.Empty;
			_TagNodePrimaryKey = string.Empty;
			_TagNodePrimaryValue = string.Empty;
			_TagEdgePrimaryKey = string.Empty;
			_TagEdgePrimaryValue = string.Empty;
			_TagGraphPrimaryKey = string.Empty;
			_TagGraphPrimaryValue = string.Empty;
			_UserPassword = string.Empty;
			_UserAuthToken = string.Empty;
			_VectorNodePrimaryGuid = Guid.Empty;
			_VectorNodeSecondaryGuid = Guid.Empty;
			_VectorEdgePrimaryGuid = Guid.Empty;
			_VectorNodePrimaryContent = string.Empty;
			_VectorNodeSecondaryContent = string.Empty;
			_VectorEdgePrimaryContent = string.Empty;
		}

		private static void AssertNotNull<T>(T? value, string message) where T : class
		{
			if (value == null)
			{
				throw new InvalidOperationException($"Assertion failed: {message} (value is null)");
			}
		}

		private static void AssertNotEmpty(Guid value, string message)
		{
			if (value == Guid.Empty)
			{
				throw new InvalidOperationException($"Assertion failed: {message} (GUID is empty)");
			}
		}

		private static void AssertNull<T>(T? value, string message) where T : class
		{
			if (value != null)
			{
				throw new InvalidOperationException($"Assertion failed: {message} (expected null)");
			}
		}

		private static void AssertTrue(bool condition, string message)
		{
			if (!condition)
			{
				throw new InvalidOperationException($"Assertion failed: {message}");
			}
		}

		private static void AssertFalse(bool condition, string message)
		{
			if (condition)
			{
				throw new InvalidOperationException($"Assertion failed: {message}");
			}
		}

		private static void AssertEqual<T>(T expected, T actual, string message)
		{
			if (!EqualityComparer<T>.Default.Equals(expected, actual))
			{
				throw new InvalidOperationException($"Assertion failed: {message} (expected '{expected}', actual '{actual}')");
			}
		}

		#endregion
	}

	internal sealed class TestResult
	{
		public string Name { get; set; } = string.Empty;
		public bool Passed { get; set; }
		public long RuntimeMs { get; set; }
		public string? ErrorMessage { get; set; }
	}

	internal sealed class SubgraphEdgeInfo
	{
		public Guid EdgeGuid { get; set; }
		public Guid From { get; set; }
		public Guid To { get; set; }

		public SubgraphEdgeInfo(Guid edgeGuid, Guid from, Guid to)
		{
			EdgeGuid = edgeGuid;
			From = from;
			To = to;
		}
	}
}

