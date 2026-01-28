namespace Test.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using ExpressionTree;
    using GetSomeInput;
    using LiteGraph.Sdk;

    public static class Program
    {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603 // Possible null reference return.

        private static bool _Debug = true;
        private static bool _RunForever = true;
        private static string _Endpoint = "http://localhost:8701";
        private static string _BearerToken = "litegraphadmin";
        private static Guid _Tenant = Guid.Parse("00000000-0000-0000-0000-000000000000");
        private static Guid _Graph = Guid.Parse("00000000-0000-0000-0000-000000000000");
        private static LiteGraphSdk _Sdk = null;

        public static void Main(string[] args)
        {
            _Sdk = new LiteGraphSdk(
                Inputty.GetString("Endpoint     :", _Endpoint, false),
                Inputty.GetString("Bearer token :", _BearerToken, false));

            _Tenant = Inputty.GetGuid("Tenant GUID  :", _Tenant);

            if (_Debug) _Sdk.Logger = Logger;

            while (_RunForever)
            {
                string userInput = Inputty.GetString("Command [?/help]:", null, false);

                switch (userInput)
                {
                    case "?":
                        Menu();
                        break;
                    case "q":
                        _RunForever = false;
                        break;
                    case "cls":
                        Console.Clear();
                        break;

                    case "conn":
                        ValidateConnectivity();
                        break;

                    case "backup":
                        BackupDatabase();
                        break;

                    case "flush":
                        FlushDatabase();
                        break;

                    case "tenant":
                        SetTenant();
                        break;
                    case "graph":
                        SetGraph();
                        break;

                    case "tenant exists":
                        TenantExists();
                        break;
                    case "tenant create":
                        TenantCreate();
                        break;
                    case "tenant update":
                        TenantUpdate();
                        break;
                    case "tenant all":
                        TenantAll();
                        break;
                    case "tenant read":
                        TenantRead();
                        break;
                    case "tenant enum":
                        TenantEnumerate();
                        break;
                    case "tenant stats":
                        TenantStats();
                        break;
                    case "tenant delete":
                        TenantDelete();
                        break;

                    case "user exists":
                        UserExists();
                        break;
                    case "user create":
                        UserCreate();
                        break;
                    case "user update":
                        UserUpdate();
                        break;
                    case "user all":
                        UserAll();
                        break;
                    case "user read":
                        UserRead();
                        break;
                    case "user enum":
                        UserEnumerate();
                        break;
                    case "user delete":
                        UserDelete();
                        break;

                    case "cred exists":
                        CredentialExists();
                        break;
                    case "cred create":
                        CredentialCreate();
                        break;
                    case "cred update":
                        CredentialUpdate();
                        break;
                    case "cred all":
                        CredentialAll();
                        break;
                    case "cred read":
                        CredentialRead();
                        break;
                    case "cred enum":
                        CredentialEnumerate();
                        break;
                    case "cred delete":
                        CredentialDelete();
                        break;
                    case "cred read bearer":
                        CredentialReadByBearerToken();
                        break;
                    case "cred delete all":
                        CredentialDeleteAllInTenant();
                        break;
                    case "cred delete user":
                        CredentialDeleteByUser();
                        break;

                    case "label exists":
                        LabelExists();
                        break;
                    case "label create":
                        LabelCreate();
                        break;
                    case "label update":
                        LabelUpdate();
                        break;
                    case "label all":
                        LabelAll();
                        break;
                    case "label read":
                        LabelRead();
                        break;
                    case "label enum":
                        LabelEnumerate();
                        break;
                    case "label delete":
                        LabelDelete();
                        break;
                    case "label all tenant":
                        LabelReadAllInTenant();
                        break;
                    case "label all graph":
                        LabelReadAllInGraph();
                        break;
                    case "label graph":
                        LabelReadManyGraph();
                        break;
                    case "label node":
                        LabelReadManyNode();
                        break;
                    case "label edge":
                        LabelReadManyEdge();
                        break;
                    case "label delete all tenant":
                        LabelDeleteAllInTenant();
                        break;
                    case "label delete all graph":
                        LabelDeleteAllInGraph();
                        break;
                    case "label delete graph":
                        LabelDeleteGraphLabels();
                        break;
                    case "label delete node":
                        LabelDeleteNodeLabels();
                        break;
                    case "label delete edge":
                        LabelDeleteEdgeLabels();
                        break;

                    case "tag exists":
                        TagExists();
                        break;
                    case "tag create":
                        TagCreate();
                        break;
                    case "tag update":
                        TagUpdate();
                        break;
                    case "tag all":
                        TagAll();
                        break;
                    case "tag read":
                        TagRead();
                        break;
                    case "tag enum":
                        TagEnumerate();
                        break;
                    case "tag delete":
                        TagDelete();
                        break;

                    case "tag read all in tenant":
                        TagReadAllInTenant();
                        break;
                    case "tag read all in graph":
                        TagReadAllInGraph();
                        break;
                    case "tag read many graph":
                        TagReadManyGraph();
                        break;
                    case "tag read many node":
                        TagReadManyNode();
                        break;
                    case "tag read many edge":
                        TagReadManyEdge();
                        break;
                    case "tag delete all in tenant":
                        TagDeleteAllInTenant();
                        break;
                    case "tag delete all in graph":
                        TagDeleteAllInGraph();
                        break;
                    case "tag delete graph tags":
                        TagDeleteGraphTags();
                        break;
                    case "tag delete node tags":
                        TagDeleteNodeTags();
                        break;
                    case "tag delete edge tags":
                        TagDeleteEdgeTags();
                        break;

                    case "vector exists":
                        VectorExists();
                        break;
                    case "vector create":
                        VectorCreate();
                        break;
                    case "vector update":
                        VectorUpdate();
                        break;
                    case "vector all":
                        VectorAll();
                        break;
                    case "vector read":
                        VectorRead();
                        break;
                    case "vector enum":
                        VectorEnumerate();
                        break;
                    case "vector delete":
                        VectorDelete();
                        break;

                    case "vector read all in tenant":
                        VectorReadAllInTenant();
                        break;
                    case "vector read all in graph":
                        VectorReadAllInGraph();
                        break;
                    case "vector read many graph":
                        VectorReadManyGraph();
                        break;
                    case "vector read many node":
                        VectorReadManyNode();
                        break;
                    case "vector read many edge":
                        VectorReadManyEdge();
                        break;
                    case "vector delete all in tenant":
                        VectorDeleteAllInTenant();
                        break;
                    case "vector delete all in graph":
                        VectorDeleteAllInGraph();
                        break;
                    case "vector delete graph vectors":
                        VectorDeleteGraphVectors();
                        break;
                    case "vector delete node vectors":
                        VectorDeleteNodeVectors();
                        break;
                    case "vector delete edge vectors":
                        VectorDeleteEdgeVectors();
                        break;

                    case "graph exists":
                        GraphExists();
                        break;
                    case "graph create":
                        GraphCreate();
                        break;
                    case "graph update":
                        GraphUpdate();
                        break;
                    case "graph all":
                        GraphAll();
                        break;
                    case "graph read":
                        GraphRead();
                        break;
                    case "graph enum":
                        GraphEnumerate();
                        break;
                    case "graph stats":
                        GraphStats();
                        break;
                    case "graph delete":
                        GraphDelete();
                        break;
                    case "graph search":
                        GraphSearch();
                        break;
                    case "graph subgraph":
                        GraphSubgraph();
                        break;
                    case "graph subgraph stats":
                        GraphSubgraphStats();
                        break;
                    case "test subgraph":
                        TestSubgraph();
                        break;
                    case "test subgraph stats":
                        TestSubgraphStats();
                        break;
                    case "graph enable index":
                        GraphEnableVectorIndex();
                        break;
                    case "graph rebuild index":
                        GraphRebuildVectorIndex();
                        break;
                    case "graph delete index":
                        GraphDeleteVectorIndex();
                        break;
                    case "graph read index config":
                        GraphReadVectorIndexConfig();
                        break;
                    case "graph index stats":
                        GraphVectorIndexStats();
                        break;
                    case "graph read all in tenant":
                        GraphReadAllInTenant();
                        break;
                    case "graph delete all in tenant":
                        GraphDeleteAllInTenant();
                        break;

                    case "node exists":
                        NodeExists();
                        break;
                    case "node create":
                        NodeCreate();
                        break;
                    case "node update":
                        NodeUpdate();
                        break;
                    case "node all":
                        NodeAll();
                        break;
                    case "node read":
                        NodeRead();
                        break;
                    case "node enum":
                        NodeEnumerate();
                        break;
                    case "node delete":
                        NodeDelete();
                        break;
                    case "node search":
                        NodeSearch();
                        break;

                    case "node edges":
                        NodeEdges();
                        break;
                    case "node parents":
                        NodeParents();
                        break;
                    case "node children":
                        NodeChildren();
                        break;
                    case "node all tenant":
                        NodeReadAllInTenant();
                        break;
                    case "node all graph":
                        NodeReadAllInGraph();
                        break;
                    case "node most connected":
                        NodeReadMostConnected();
                        break;
                    case "node least connected":
                        NodeReadLeastConnected();
                        break;
                    case "node delete all tenant":
                        NodeDeleteAllInTenant();
                        break;

                    case "edge exists":
                        EdgeExists();
                        break;
                    case "edge create":
                        EdgeCreate();
                        break;
                    case "edge update":
                        EdgeUpdate();
                        break;
                    case "edge all":
                        EdgeAll();
                        break;
                    case "edge read":
                        EdgeRead();
                        break;
                    case "edge enum":
                        EdgeEnumerate();
                        break;
                    case "edge delete":
                        EdgeDelete();
                        break;
                    case "edge search":
                        EdgeSearch();
                        break;

                    case "edges from":
                        EdgesFrom();
                        break;
                    case "edges to":
                        EdgesTo();
                        break;
                    case "edges between":
                        EdgesBetween();
                        break;
                    case "edge all tenant":
                        EdgeReadAllInTenant();
                        break;
                    case "edge all graph":
                        EdgeReadAllInGraph();
                        break;
                    case "edge delete all tenant":
                        EdgeDeleteAllInTenant();
                        break;
                    case "edge delete node":
                        EdgeDeleteNodeEdges();
                        break;
                    case "edge delete nodes":
                        EdgeDeleteNodeEdgesMany();
                        break;

                    case "route":
                        Route();
                        break;

                    case "vsearch":
                        VectorSearch();
                        break;

                    case "auth tenants":
                        AuthGetTenantsForEmail();
                        break;
                    case "auth generate token":
                        AuthGenerateToken();
                        break;
                    case "auth token details":
                        AuthGetTokenDetails();
                        break;
                }

                Console.WriteLine("");
            }
        }

        private static void Menu()
        {
            Console.WriteLine("");
            Console.WriteLine("Available commands:");
            Console.WriteLine("  ?                            help, this menu");
            Console.WriteLine("  q                            quit");
            Console.WriteLine("  cls                          clear the screen");
            Console.WriteLine("  conn                         validate connectivity");
            Console.WriteLine("  backup                       create a database backup");
            Console.WriteLine("  tenant                       set the tenant GUID (currently " + _Tenant + ")");
            Console.WriteLine("  graph                        set the graph GUID (currently " + _Graph + ")");
            Console.WriteLine("");
            Console.WriteLine("Administrative commands (requires administrative bearer token):");
            Console.WriteLine("  Tenants                    : tenant [create|update|all|read|enum|stats|delete|exists]");
            Console.WriteLine("  Users                      : user [create|update|all|read|enum|delete|exists]");
            Console.WriteLine("  Credentials                : cred [create|update|all|read|enum|delete|exists|read bearer|delete all|delete user]");
            Console.WriteLine("  Labels                     : label [create|update|all|read|enum|delete|exists|all tenant|all graph|graph|node|edge|delete all tenant|delete all graph|delete graph|delete node|delete edge]");
            Console.WriteLine("  Tags                       : tag [create|update|all|read|enum|delete|exists]");
            Console.WriteLine("  Vectors                    : vector [create|update|all|read|enum|delete|exists]");
            Console.WriteLine("");
            Console.WriteLine("User commands:");
            Console.WriteLine("  Graphs                     : graph [create|update|all|read|enum|stats|delete|exists|search|subgraph|subgraph stats|enable index|rebuild index|delete index|read index config|index stats]");
            Console.WriteLine("  Nodes                      : node [create|update|all|read|enum|delete|exists|search|edges|parents|children|all tenant|all graph|most connected|least connected|delete all tenant]");
            Console.WriteLine("  Edges                      : edge [create|update|all|read|enum|delete|exists|from|to|search|between|all tenant|all graph|delete all tenant|delete node|delete nodes]");
            Console.WriteLine("  Vector search              : vsearch");
            Console.WriteLine("");
            Console.WriteLine("Test commands:");
            Console.WriteLine("  Subgraph automated test    : test subgraph");
            Console.WriteLine("  Subgraph stats test        : test subgraph stats");
            Console.WriteLine("");
            Console.WriteLine("Authentication commands:");
            Console.WriteLine("  auth tenants               : Get tenants for email");
            Console.WriteLine("  auth generate token        : Generate authentication token");
            Console.WriteLine("  auth token details         : Get token details");
            Console.WriteLine("");
            Console.WriteLine("Routing commands:");
            Console.WriteLine("  route");
            Console.WriteLine("");
        }

        private static void Logger(SeverityEnum sev, string msg)
        {
            Console.WriteLine(sev.ToString() + " " + msg);
        }

        private static void SetTenant()
        {
            _Tenant = Inputty.GetGuid("Tenant GUID:", _Tenant);
        }

        private static void SetGraph()
        {
            _Graph = Inputty.GetGuid("Graph GUID:", _Graph);
        }

        private static string GetName()
        {
            return Inputty.GetString("Name:", null, false);
        }

        private static List<string> GetLabels()
        {
            string val = GetJson("Labels JSON:");
            if (!String.IsNullOrEmpty(val)) return Serializer.DeserializeJson<List<string>>(val);
            return null;
        }

        private static NameValueCollection GetTags()
        {
            string val = GetJson("Tags JSON:");
            if (!String.IsNullOrEmpty(val)) return Serializer.DeserializeJson<NameValueCollection>(val);
            return null;
        }

        private static object GetData()
        {
            string val = GetJson("Data JSON:");
            if (!String.IsNullOrEmpty(val)) return Serializer.DeserializeJson<object>(val);
            return null;
        }

        private static List<VectorMetadata> GetVectors()
        {
            string val = GetJson("Vectors JSON:");
            if (!String.IsNullOrEmpty(val)) return Serializer.DeserializeJson<List<VectorMetadata>>(val);
            return null;
        }

        private static List<float> GetEmbeddings()
        {
            string val = GetJson("Embeddings JSON:");
            if (!String.IsNullOrEmpty(val)) return Serializer.DeserializeJson<List<float>>(val);
            return null;
        }

        private static string GetJson(string prompt)
        {
            return Inputty.GetString(prompt, null, true);
        }

        private static Guid GetGuid(string prompt, Guid? guid = null)
        {
            if (guid == null) return Inputty.GetGuid(prompt, default(Guid));
            else return Inputty.GetGuid(prompt, guid.Value);
        }

        private static bool GetBoolean(string prompt)
        {
            return Inputty.GetBoolean(prompt, true);
        }

        private static SearchRequest BuildSearchRequest(bool includeGraphGuid)
        {
            List<string> labels = GetLabels();
            NameValueCollection nvc = BuildNameValueCollection();
            Expr expr = BuildExpression();

            SearchRequest req = new SearchRequest();
            req.TenantGUID = Inputty.GetGuid("Tenant GUID:", _Tenant);
            if (includeGraphGuid) req.GraphGUID = Inputty.GetGuid("Graph GUID:", _Graph);
            req.Labels = labels;
            req.Tags = nvc;
            req.Expr = expr;
            return req;
        }

        private static VectorSearchRequest BuildVectorSearchRequest()
        {
            List<string> labels = GetLabels();
            NameValueCollection nvc = BuildNameValueCollection();
            Expr expr = BuildExpression();
            List<float> embeddings = GetEmbeddings();

            VectorSearchRequest req = new VectorSearchRequest();
            req.TenantGUID = Inputty.GetGuid("Tenant GUID:", _Tenant);
            string graphGuid = Inputty.GetString("Graph GUID:", null, true);
            string domain = Inputty.GetString("Domain [Graph|Node|Edge]:", "Node", false);
            string searchType = Inputty.GetString("Search type [CosineSimilarity|CosineDistance|EuclidianSimilarity|EuclidianDistance|DotProduct]:", "CosineSimilarity", false);

            if (!String.IsNullOrEmpty(graphGuid)) req.GraphGUID = Guid.Parse(graphGuid);
            req.Domain = (VectorSearchDomainEnum)(Enum.Parse(typeof(VectorSearchDomainEnum), domain));
            req.SearchType = (VectorSearchTypeEnum)(Enum.Parse(typeof(VectorSearchTypeEnum), searchType));
            req.Labels = GetLabels();
            req.Tags = nvc;
            req.Expr = expr;
            req.Embeddings = embeddings;
            return req;
        }

        private static NameValueCollection BuildNameValueCollection()
        {
            Console.WriteLine("");
            Console.WriteLine("Add keys and values to build a name value collection");
            Console.WriteLine("Press ENTER on a key to end");

            NameValueCollection ret = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            while (true)
            {
                string key = Inputty.GetString("Key   :", null, true);
                if (String.IsNullOrEmpty(key)) break;

                string val = Inputty.GetString("Value :", null, true);
                ret.Add(key, val);
            }

            return ret;
        }

        private static Expr BuildExpression()
        {
            Console.WriteLine("");
            Console.WriteLine("Add JSON values to build an expression");
            Console.WriteLine("Example expressions:");

            Expr e1 = new Expr("Age", OperatorEnum.GreaterThan, 38);
            e1.PrependAnd("Hobby.Name", OperatorEnum.Equals, "BJJ");
            Console.WriteLine(Serializer.SerializeJson(e1, false));

            Expr e2 = new Expr("Mbps", OperatorEnum.GreaterThan, 250);
            Console.WriteLine(Serializer.SerializeJson(e2, false));
            Console.WriteLine("");

            string json = Inputty.GetString("JSON:", null, true);
            if (String.IsNullOrEmpty(json)) return null;

            Expr expr = Serializer.DeserializeJson<Expr>(json);
            Console.WriteLine("");
            Console.WriteLine("Using expression: " + expr.ToString());
            Console.WriteLine("");
            return expr;
        }
         
        private static EnumerationRequest BuildEnumerationQuery()
        {
            return new EnumerationRequest
            {
                TenantGUID = _Tenant,
                GraphGUID = _Graph
            };
        }

        private static void EnumerateResult(object obj)
        {
            Console.WriteLine("");
            Console.Write("Result: ");
            if (obj == null) Console.WriteLine("(null)");
            else Console.WriteLine(Environment.NewLine + Serializer.SerializeJson(obj, true));
            Console.WriteLine("");
        }

        private static void ValidateConnectivity()
        {
            Console.WriteLine("Connected: " + _Sdk.ValidateConnectivity().Result);
        }

        #region Admin

        private static void BackupDatabase()
        {
            string filename = Inputty.GetString("Backup filename:", null, true);
            if (String.IsNullOrEmpty(filename)) return;
            _Sdk.Admin.Backup(filename).Wait();
        }

        private static void FlushDatabase()
        {
            _Sdk.Admin.FlushDatabase().Wait();
        }

        #endregion

        #region Tenant

        private static void ShowSampleTenant()
        {
            Console.WriteLine("Sample JSON:");
            Console.WriteLine(Serializer.SerializeJson(new TenantMetadata
            {
                GUID = Guid.NewGuid(),
                Name = "My tenant"
            }, false));
        }

        private static void TenantExists()
        {
            EnumerateResult(_Sdk.Tenant.ExistsByGuid(
                GetGuid("GUID:", _Tenant)).Result);
        }

        private static void TenantCreate()
        {
            EnumerateResult(_Sdk.Tenant.Create(Serializer.DeserializeJson<TenantMetadata>(GetJson("Tenant JSON:"))).Result);
        }

        private static void TenantUpdate()
        {
            ShowSampleTenant();
            EnumerateResult(_Sdk.Tenant.Update(
                Serializer.DeserializeJson<TenantMetadata>(GetJson("Tenant JSON:"))).Result);
        }

        private static void TenantAll()
        {
            EnumerateResult(_Sdk.Tenant.ReadMany().Result);
        }

        private static void TenantRead()
        {
            EnumerateResult(_Sdk.Tenant.ReadByGuid(
                GetGuid("GUID:", _Tenant)).Result);
        }

        private static void TenantEnumerate()
        {
            EnumerateResult(_Sdk.Tenant.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void TenantStats()
        {
            EnumerateResult(_Sdk.Tenant.GetStatistics().Result);
        }

        private static void TenantDelete()
        {
            _Sdk.Tenant.DeleteByGuid(
                GetGuid("GUID:", _Tenant),
                GetBoolean("Force:")).Wait();
        }

        #endregion

        #region User

        private static void ShowSampleUser()
        {
            Console.WriteLine("Sample JSON:");
            Console.WriteLine(Serializer.SerializeJson(new UserMaster
            {
                GUID = Guid.NewGuid(),
                FirstName = "First",
                LastName = "Last",
                Email = "first@last.com",
                Password = "password"
            }, false));
        }

        private static void UserExists()
        {
            EnumerateResult(_Sdk.User.ExistsByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void UserCreate()
        {
            ShowSampleUser();
            EnumerateResult(_Sdk.User.Create(Serializer.DeserializeJson<UserMaster>(GetJson("User JSON:"))).Result);
        }

        private static void UserAll()
        {
            EnumerateResult(_Sdk.User.ReadMany(GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void UserRead()
        {
            EnumerateResult(_Sdk.User.ReadByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void UserEnumerate()
        {
            EnumerateResult(_Sdk.User.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void UserUpdate()
        {
            ShowSampleUser();
            EnumerateResult(_Sdk.User.Update(Serializer.DeserializeJson<UserMaster>(GetJson("User JSON:"))).Result);
        }

        private static void UserDelete()
        {
            _Sdk.User.DeleteByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Wait();
        }

        #endregion

        #region Credential

        private static void ShowSampleCredential()
        {
            Console.WriteLine("Sample JSON:");
            Console.WriteLine(Serializer.SerializeJson(new Credential
            {
                Name = "My credential"
            }, false));
        }

        private static void CredentialExists()
        {
            EnumerateResult(_Sdk.Credential.ExistsByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void CredentialCreate()
        {
            ShowSampleCredential();
            EnumerateResult(_Sdk.Credential.Create(Serializer.DeserializeJson<Credential>(GetJson("Credential JSON:"))).Result);
        }

        private static void CredentialAll()
        {
            EnumerateResult(_Sdk.Credential.ReadMany(GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void CredentialRead()
        {
            EnumerateResult(_Sdk.Credential.ReadByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void CredentialEnumerate()
        {
            EnumerateResult(_Sdk.Credential.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void CredentialUpdate()
        {
            ShowSampleCredential();
            EnumerateResult(_Sdk.Credential.Update(Serializer.DeserializeJson<Credential>(GetJson("Credential JSON:"))).Result);
        }

        private static void CredentialDelete()
        {
            _Sdk.Credential.DeleteByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Wait();
        }

        private static void CredentialReadByBearerToken()
        {
            string bearerToken = Inputty.GetString("Bearer Token:", null, false);
            EnumerateResult(_Sdk.Credential.ReadByBearerToken(bearerToken).Result);
        }

        private static void CredentialDeleteAllInTenant()
        {
            _Sdk.Credential.DeleteAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Wait();
            Console.WriteLine("All credentials in tenant deleted successfully.");
        }

        private static void CredentialDeleteByUser()
        {
            _Sdk.Credential.DeleteByUser(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("User GUID:")).Wait();
            Console.WriteLine("All credentials for user deleted successfully.");
        }

        #endregion

        #region Label

        private static void ShowSampleLabel()
        {
            Console.WriteLine("Sample JSON:");
            Console.WriteLine(Serializer.SerializeJson(new LabelMetadata
            {
                Label = "label"
            }, false));
        }

        private static void LabelExists()
        {
            EnumerateResult(_Sdk.Label.ExistsByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void LabelCreate()
        {
            ShowSampleLabel();
            EnumerateResult(_Sdk.Label.Create(Serializer.DeserializeJson<LabelMetadata>(GetJson("Label JSON:"))).Result);
        }

        private static void LabelAll()
        {
            EnumerateResult(_Sdk.Label.ReadMany(GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void LabelRead()
        {
            EnumerateResult(_Sdk.Label.ReadByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void LabelEnumerate()
        {
            EnumerateResult(_Sdk.Label.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void LabelUpdate()
        {
            ShowSampleLabel();
            EnumerateResult(_Sdk.Label.Update(Serializer.DeserializeJson<LabelMetadata>(GetJson("Label JSON:"))).Result);
        }

        private static void LabelDelete()
        {
            _Sdk.Label.DeleteByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Wait();
        }

        private static void LabelReadAllInTenant()
        {
            EnumerateResult(_Sdk.Label.ReadAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void LabelReadAllInGraph()
        {
            EnumerateResult(_Sdk.Label.ReadAllInGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void LabelReadManyGraph()
        {
            EnumerateResult(_Sdk.Label.ReadManyGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void LabelReadManyNode()
        {
            EnumerateResult(_Sdk.Label.ReadManyNode(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Node GUID:")).Result);
        }

        private static void LabelReadManyEdge()
        {
            EnumerateResult(_Sdk.Label.ReadManyEdge(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Edge GUID:")).Result);
        }

        private static void LabelDeleteAllInTenant()
        {
            _Sdk.Label.DeleteAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Wait();
            Console.WriteLine("All labels in tenant deleted successfully.");
        }

        private static void LabelDeleteAllInGraph()
        {
            _Sdk.Label.DeleteAllInGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Wait();
            Console.WriteLine("All labels in graph deleted successfully.");
        }

        private static void LabelDeleteGraphLabels()
        {
            _Sdk.Label.DeleteGraphLabels(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Wait();
            Console.WriteLine("Graph labels deleted successfully.");
        }

        private static void LabelDeleteNodeLabels()
        {
            _Sdk.Label.DeleteNodeLabels(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Node GUID:")).Wait();
            Console.WriteLine("Node labels deleted successfully.");
        }

        private static void LabelDeleteEdgeLabels()
        {
            _Sdk.Label.DeleteEdgeLabels(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Edge GUID:")).Wait();
            Console.WriteLine("Edge labels deleted successfully.");
        }

        #endregion

        #region Tag

        private static void ShowSampleTag()
        {
            Console.WriteLine("Sample JSON:");
            Console.WriteLine(Serializer.SerializeJson(new TagMetadata
            {
                Key = "foo",
                Value = "bar"
            }, false));
        }

        private static void TagExists()
        {
            EnumerateResult(_Sdk.Tag.ExistsByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void TagCreate()
        {
            ShowSampleTag();
            EnumerateResult(_Sdk.Tag.Create(Serializer.DeserializeJson<TagMetadata>(GetJson("Tag JSON:"))).Result);
        }

        private static void TagAll()
        {
            EnumerateResult(_Sdk.Tag.ReadMany(GetGuid("Tenant GUID:", _Tenant), null, null, null).Result);
        }

        private static void TagRead()
        {
            EnumerateResult(_Sdk.Tag.ReadByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void TagEnumerate()
        {
            EnumerateResult(_Sdk.Tag.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void TagUpdate()
        {
            ShowSampleTag();
            EnumerateResult(_Sdk.Tag.Update(Serializer.DeserializeJson<TagMetadata>(GetJson("Tag JSON:"))).Result);
        }

        private static void TagDelete()
        {
            _Sdk.Tag.DeleteByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Wait();
        }

        private static void TagReadAllInTenant()
        {
            EnumerateResult(_Sdk.Tag.ReadAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void TagReadAllInGraph()
        {
            EnumerateResult(_Sdk.Tag.ReadAllInGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void TagReadManyGraph()
        {
            EnumerateResult(_Sdk.Tag.ReadManyGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void TagReadManyNode()
        {
            EnumerateResult(_Sdk.Tag.ReadManyNode(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Node GUID:")).Result);
        }

        private static void TagReadManyEdge()
        {
            EnumerateResult(_Sdk.Tag.ReadManyEdge(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Edge GUID:")).Result);
        }

        private static void TagDeleteAllInTenant()
        {
            _Sdk.Tag.DeleteAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Wait();
            Console.WriteLine("All tags in tenant deleted successfully.");
        }

        private static void TagDeleteAllInGraph()
        {
            _Sdk.Tag.DeleteAllInGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Wait();
            Console.WriteLine("All tags in graph deleted successfully.");
        }

        private static void TagDeleteGraphTags()
        {
            _Sdk.Tag.DeleteGraphTags(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Wait();
            Console.WriteLine("Graph tags deleted successfully.");
        }

        private static void TagDeleteNodeTags()
        {
            _Sdk.Tag.DeleteNodeTags(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Node GUID:")).Wait();
            Console.WriteLine("Node tags deleted successfully.");
        }

        private static void TagDeleteEdgeTags()
        {
            _Sdk.Tag.DeleteEdgeTags(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Edge GUID:")).Wait();
            Console.WriteLine("Edge tags deleted successfully.");
        }

        #endregion

        #region Vector

        private static void ShowSampleVector()
        {
            Console.WriteLine("Sample JSON:");
            Console.WriteLine(Serializer.SerializeJson(new VectorMetadata
            {
                TenantGUID = _Tenant,
                GraphGUID = default(Guid),
                NodeGUID = default(Guid),
                EdgeGUID = default(Guid),
                Model = "test",
                Dimensionality = 384,
                Content = "content",
                Vectors = new List<float> { 0.1f, 0.2f, 0.3f }
            }, false));
        }

        private static void VectorExists()
        {
            EnumerateResult(_Sdk.Vector.ExistsByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void VectorCreate()
        {
            ShowSampleVector();
            EnumerateResult(_Sdk.Vector.Create(Serializer.DeserializeJson<VectorMetadata>(GetJson("Vector JSON:"))).Result);
        }

        private static void VectorAll()
        {
            EnumerateResult(_Sdk.Vector.ReadMany(GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void VectorRead()
        {
            EnumerateResult(_Sdk.Vector.ReadByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void VectorEnumerate()
        {
            EnumerateResult(_Sdk.Vector.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void VectorUpdate()
        {
            ShowSampleVector();
            EnumerateResult(_Sdk.Vector.Update(Serializer.DeserializeJson<VectorMetadata>(GetJson("Vector JSON:"))).Result);
        }

        private static void VectorDelete()
        {
            _Sdk.Vector.DeleteByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Wait();
        }

        private static void VectorReadAllInTenant()
        {
            EnumerateResult(_Sdk.Vector.ReadAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void VectorReadAllInGraph()
        {
            EnumerateResult(_Sdk.Vector.ReadAllInGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void VectorReadManyGraph()
        {
            EnumerateResult(_Sdk.Vector.ReadManyGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void VectorReadManyNode()
        {
            EnumerateResult(_Sdk.Vector.ReadManyNode(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Node GUID:")).Result);
        }

        private static void VectorReadManyEdge()
        {
            EnumerateResult(_Sdk.Vector.ReadManyEdge(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Edge GUID:")).Result);
        }

        private static void VectorDeleteAllInTenant()
        {
            _Sdk.Vector.DeleteAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Wait();
            Console.WriteLine("All vectors in tenant deleted successfully.");
        }

        private static void VectorDeleteAllInGraph()
        {
            _Sdk.Vector.DeleteAllInGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Wait();
            Console.WriteLine("All vectors in graph deleted successfully.");
        }

        private static void VectorDeleteGraphVectors()
        {
            _Sdk.Vector.DeleteGraphVectors(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Wait();
            Console.WriteLine("Graph vectors deleted successfully.");
        }

        private static void VectorDeleteNodeVectors()
        {
            _Sdk.Vector.DeleteNodeVectors(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Node GUID:")).Wait();
            Console.WriteLine("Node vectors deleted successfully.");
        }

        private static void VectorDeleteEdgeVectors()
        {
            _Sdk.Vector.DeleteEdgeVectors(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Edge GUID:")).Wait();
            Console.WriteLine("Edge vectors deleted successfully.");
        }

        #endregion

        #region Graph

        private static void ShowSampleGraph()
        {
            List<string> labels = new List<string>();
            labels.Add("test");

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("key", "value");

            NameValueCollection tags = new NameValueCollection();
            tags.Add("key", "value");

            List<VectorMetadata> vectors = new List<VectorMetadata>
            {
                new VectorMetadata
                {
                    TenantGUID = default(Guid),
                    GraphGUID = default(Guid),
                    Model = "test model",
                    Dimensionality = 3,
                    Content = "test data",
                    Vectors = new List<float> { 0.1f, 0.2f, 0.3f }
                }
            };

            Console.WriteLine("Sample JSON:");
            Console.WriteLine(Serializer.SerializeJson(new Graph
            {
                TenantGUID = _Tenant,
                Name = "Test graph",
                Labels = labels,
                Tags = tags,
                Data = data,
                Vectors = vectors
            }, false));
        }

        private static void GraphExists()
        {
            EnumerateResult(_Sdk.Graph.ExistsByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:")).Result);
        }

        private static void GraphCreate()
        {
            ShowSampleGraph();
            EnumerateResult(_Sdk.Graph.Create(Serializer.DeserializeJson<Graph>(GetJson("Graph JSON:"))).Result);
        }

        private static void GraphAll()
        {
            EnumerateResult(_Sdk.Graph.ReadMany(GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void GraphRead()
        {
            EnumerateResult(_Sdk.Graph.ReadByGuid(
                GetGuid("Tenant GUID:", _Tenant), 
                GetGuid("GUID:", _Graph)).Result);
        }

        private static void GraphEnumerate()
        {
            EnumerateResult(_Sdk.Graph.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void GraphUpdate()
        {
            ShowSampleGraph();
            EnumerateResult(_Sdk.Graph.Update(Serializer.DeserializeJson<Graph>(GetJson("Graph JSON:"))).Result);
        }

        private static void GraphStats()
        {
            EnumerateResult(_Sdk.Graph.GetStatistics(_Tenant).Result);
        }

        private static void GraphDelete()
        {
            _Sdk.Graph.DeleteByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("GUID:", _Graph), 
                GetBoolean("Force:")).Wait();
        }

        private static void GraphSearch()
        {
            SearchRequest req = BuildSearchRequest(false);
            if (req == null) return;
            EnumerateResult(_Sdk.Graph.Search(req).Result);
        }

        private static void GraphSubgraph()
        {
            Guid tenantGuid = GetGuid("Tenant GUID:", _Tenant);
            Guid graphGuid = GetGuid("Graph GUID:", _Graph);
            Guid nodeGuid = GetGuid("Node GUID:", default(Guid));
            
            string maxDepthStr = Inputty.GetString("Max depth (default 2):", "2", true);
            int maxDepth = 2;
            if (!String.IsNullOrEmpty(maxDepthStr) && int.TryParse(maxDepthStr, out int parsedDepth))
            {
                maxDepth = parsedDepth;
            }
            
            string maxNodesStr = Inputty.GetString("Max nodes (default 0 = unlimited):", "0", true);
            int maxNodes = 0;
            if (!String.IsNullOrEmpty(maxNodesStr) && int.TryParse(maxNodesStr, out int parsedNodes))
            {
                maxNodes = parsedNodes;
            }
            
            string maxEdgesStr = Inputty.GetString("Max edges (default 0 = unlimited):", "0", true);
            int maxEdges = 0;
            if (!String.IsNullOrEmpty(maxEdgesStr) && int.TryParse(maxEdgesStr, out int parsedEdges))
            {
                maxEdges = parsedEdges;
            }
            
            bool includeData = Inputty.GetBoolean("Include data:", false);
            bool includeSubordinates = Inputty.GetBoolean("Include subordinates:", false);
            
            SearchResult result = _Sdk.Graph.GetSubgraph(
                tenantGuid,
                graphGuid,
                nodeGuid,
                maxDepth,
                maxNodes,
                maxEdges,
                includeData,
                includeSubordinates).Result;
            
            EnumerateResult(result);
        }

        private static void GraphSubgraphStats()
        {
            Guid tenantGuid = GetGuid("Tenant GUID:", _Tenant);
            Guid graphGuid = GetGuid("Graph GUID:", _Graph);
            Guid nodeGuid = GetGuid("Node GUID:", default(Guid));
            
            string maxDepthStr = Inputty.GetString("Max depth (default 2):", "2", true);
            int maxDepth = 2;
            if (!String.IsNullOrEmpty(maxDepthStr) && int.TryParse(maxDepthStr, out int parsedDepth))
            {
                maxDepth = parsedDepth;
            }
            
            string maxNodesStr = Inputty.GetString("Max nodes (default 0 = unlimited):", "0", true);
            int maxNodes = 0;
            if (!String.IsNullOrEmpty(maxNodesStr) && int.TryParse(maxNodesStr, out int parsedNodes))
            {
                maxNodes = parsedNodes;
            }
            
            string maxEdgesStr = Inputty.GetString("Max edges (default 0 = unlimited):", "0", true);
            int maxEdges = 0;
            if (!String.IsNullOrEmpty(maxEdgesStr) && int.TryParse(maxEdgesStr, out int parsedEdges))
            {
                maxEdges = parsedEdges;
            }
            
            GraphStatistics stats = _Sdk.Graph.GetSubgraphStatistics(
                tenantGuid,
                graphGuid,
                nodeGuid,
                maxDepth,
                maxNodes,
                maxEdges).Result;
            
            EnumerateResult(stats);
        }

        private static void TestSubgraph()
        {
            Console.WriteLine("");
            Console.WriteLine("=== AUTOMATED SUBGRAPH TEST ===");
            Console.WriteLine("");

            #region Create Tenant and Graph

            Console.WriteLine("Creating test tenant and graph...");
            TenantMetadata tenant = _Sdk.Tenant.Create(new TenantMetadata { Name = "Subgraph Test Tenant" }).Result;
            Console.WriteLine("| Created tenant: " + tenant.GUID);

            Graph graph = _Sdk.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Subgraph Test Graph"
            }).Result;
            Console.WriteLine("| Created graph: " + graph.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Nodes

            // Create a hierarchical structure for testing:
            // Node A (root/starting node)
            //   -> Node B, C (layer 1)
            //       -> Node D, E (from B, layer 2)
            //       -> Node F (from C, layer 2)
            //           -> Node G (from D, layer 3)

            Console.WriteLine("Creating test nodes...");
            Node nodeA = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node A (Root)",
                Data = new { Type = "Root", Level = 0 }
            }).Result;
            Console.WriteLine("  | Created Node A: " + nodeA.GUID);

            Node nodeB = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node B (Layer 1)",
                Data = new { Type = "Layer1", Level = 1 }
            }).Result;
            Console.WriteLine("  | Created Node B: " + nodeB.GUID);

            Node nodeC = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node C (Layer 1)",
                Data = new { Type = "Layer1", Level = 1 }
            }).Result;
            Console.WriteLine("  | Created Node C: " + nodeC.GUID);

            Node nodeD = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node D (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            }).Result;
            Console.WriteLine("  | Created Node D: " + nodeD.GUID);

            Node nodeE = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node E (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            }).Result;
            Console.WriteLine("  | Created Node E: " + nodeE.GUID);

            Node nodeF = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node F (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            }).Result;
            Console.WriteLine("  | Created Node F: " + nodeF.GUID);

            Node nodeG = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node G (Layer 3)",
                Data = new { Type = "Layer3", Level = 3 }
            }).Result;
            Console.WriteLine("  | Created Node G: " + nodeG.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Edges

            Console.WriteLine("Creating test edges...");
            Edge edgeAB = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeA.GUID,
                To = nodeB.GUID,
                Name = "A -> B",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge A->B: " + edgeAB.GUID);

            Edge edgeAC = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeA.GUID,
                To = nodeC.GUID,
                Name = "A -> C",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge A->C: " + edgeAC.GUID);

            Edge edgeBD = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeB.GUID,
                To = nodeD.GUID,
                Name = "B -> D",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge B->D: " + edgeBD.GUID);

            Edge edgeBE = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeB.GUID,
                To = nodeE.GUID,
                Name = "B -> E",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge B->E: " + edgeBE.GUID);

            Edge edgeCF = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeC.GUID,
                To = nodeF.GUID,
                Name = "C -> F",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge C->F: " + edgeCF.GUID);

            Edge edgeDG = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeD.GUID,
                To = nodeG.GUID,
                Name = "D -> G",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge D->G: " + edgeDG.GUID);

            // Add a back edge to test bidirectional traversal
            Edge edgeCA = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeC.GUID,
                To = nodeA.GUID,
                Name = "C -> A (back edge)",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge C->A (back): " + edgeCA.GUID);
            Console.WriteLine("");

            #endregion

            #region Test Subgraph Retrieval

            Console.WriteLine("=== TESTING SUBGRAPH RETRIEVAL ===");
            Console.WriteLine("");
            Console.WriteLine("Graph Structure:");
            Console.WriteLine("  A (root)");
            Console.WriteLine("  ├─> B");
            Console.WriteLine("  │   ├─> D");
            Console.WriteLine("  │   │   └─> G");
            Console.WriteLine("  │   └─> E");
            Console.WriteLine("  └─> C");
            Console.WriteLine("      ├─> F");
            Console.WriteLine("      └─> A (back edge)");
            Console.WriteLine("");

            // Test Case 1: Max depth 0 (only starting node)
            Console.WriteLine("--- Test Case 1: Max Depth 0 (starting node only) ---");
            SearchResult result1 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 0,
                maxNodes: 0,
                maxEdges: 0,
                includeData: false,
                includeSubordinates: false).Result;

            if (result1 == null)
            {
                Console.WriteLine("  [ERROR] Result is null - API call failed");
                Console.WriteLine("");
                return;
            }

            Console.WriteLine("  Nodes: " + (result1.Nodes?.Count ?? 0) + " (expected: 1)");
            Console.WriteLine("  Edges: " + (result1.Edges?.Count ?? 0) + " (expected: 0)");
            bool test1Pass = (result1.Nodes?.Count ?? 0) == 1 && (result1.Edges?.Count ?? 0) == 0;
            Console.WriteLine("  Result: " + (test1Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("");

            // Test Case 2: Max depth 1 (immediate neighbors)
            Console.WriteLine("--- Test Case 2: Max Depth 1 (immediate neighbors) ---");
            SearchResult result2 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 1,
                maxNodes: 0,
                maxEdges: 0,
                includeData: false,
                includeSubordinates: false).Result;

            if (result2 == null)
            {
                Console.WriteLine("  [ERROR] Result is null - API call failed");
                Console.WriteLine("");
                return;
            }

            Console.WriteLine("  Nodes: " + (result2.Nodes?.Count ?? 0) + " (expected: 3 - A, B, C)");
            Console.WriteLine("  Edges: " + (result2.Edges?.Count ?? 0) + " (expected: 3 - A->B, A->C, C->A)");
            Console.WriteLine("  Note: Includes back edge C->A since both endpoints are in subgraph");
            bool test2Pass = (result2.Nodes?.Count ?? 0) >= 3 && (result2.Edges?.Count ?? 0) >= 3;
            Console.WriteLine("  Result: " + (test2Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("");

            // Test Case 3: Max depth 2 (two layers)
            Console.WriteLine("--- Test Case 3: Max Depth 2 (two layers) ---");
            SearchResult result3 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 0,
                maxEdges: 0,
                includeData: false,
                includeSubordinates: false).Result;

            if (result3 == null)
            {
                Console.WriteLine("  [ERROR] Result is null - API call failed");
                Console.WriteLine("");
                return;
            }

            Console.WriteLine("  Nodes: " + (result3.Nodes?.Count ?? 0) + " (expected: 6 - A, B, C, D, E, F)");
            Console.WriteLine("  Edges: " + (result3.Edges?.Count ?? 0) + " (expected: 6 - A->B, A->C, B->D, B->E, C->F, C->A)");
            Console.WriteLine("  Note: Includes back edge C->A since both endpoints are in subgraph");
            bool test3Pass = (result3.Nodes?.Count ?? 0) >= 6 && (result3.Edges?.Count ?? 0) >= 6;
            Console.WriteLine("  Result: " + (test3Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("");

            // Test Case 4: Max nodes limit
            Console.WriteLine("--- Test Case 4: Max Nodes Limit (3) ---");
            SearchResult result4 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 3,
                maxEdges: 0,
                includeData: false,
                includeSubordinates: false).Result;

            if (result4 == null)
            {
                Console.WriteLine("  [ERROR] Result is null - API call failed");
                Console.WriteLine("");
                return;
            }

            Console.WriteLine("  Nodes: " + (result4.Nodes?.Count ?? 0) + " (expected: 3)");
            bool test4Pass = (result4.Nodes?.Count ?? 0) == 3;
            Console.WriteLine("  Result: " + (test4Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("");

            // Test Case 5: Max edges limit
            Console.WriteLine("--- Test Case 5: Max Edges Limit (2) ---");
            SearchResult result5 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 0,
                maxEdges: 2,
                includeData: false,
                includeSubordinates: false).Result;

            if (result5 == null)
            {
                Console.WriteLine("  [ERROR] Result is null - API call failed");
                Console.WriteLine("");
                return;
            }

            Console.WriteLine("  Edges: " + (result5.Edges?.Count ?? 0) + " (expected: 2)");
            bool test5Pass = (result5.Edges?.Count ?? 0) == 2;
            Console.WriteLine("  Result: " + (test5Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("");

            // Test Case 6: Include data
            Console.WriteLine("--- Test Case 6: Include Data ---");
            SearchResult result6 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 1,
                maxNodes: 0,
                maxEdges: 0,
                includeData: true,
                includeSubordinates: false).Result;

            if (result6 == null)
            {
                Console.WriteLine("  [ERROR] Result is null - API call failed");
                Console.WriteLine("");
                return;
            }

            bool hasData = result6.Nodes?.Any(n => n.Data != null) ?? false;
            Console.WriteLine("  Data included: " + hasData + " (expected: true)");
            Console.WriteLine("  Result: " + (hasData ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("");

            // Validation checks
            Console.WriteLine("=== VALIDATION ===");
            Console.WriteLine("");

            if (result3.Edges != null && result3.Nodes != null)
            {
                HashSet<Guid> nodeGuids = new HashSet<Guid>(result3.Nodes.Select(n => n.GUID));
                int invalidEdges = 0;

                foreach (Edge edge in result3.Edges)
                {
                    if (!nodeGuids.Contains(edge.From) || !nodeGuids.Contains(edge.To))
                    {
                        Console.WriteLine("  [WARNING] Edge " + edge.GUID + " connects to nodes outside the subgraph!");
                        invalidEdges++;
                    }
                }

                if (invalidEdges == 0)
                    Console.WriteLine("  [OK] All edges connect nodes within the subgraph");
                else
                    Console.WriteLine("  [ERROR] " + invalidEdges + " edge(s) connect to nodes outside the subgraph");
            }

            Console.WriteLine("");
            Console.WriteLine("=== TEST SUMMARY ===");
            Console.WriteLine("  Test Case 1 (Max Depth 0): " + (test1Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 2 (Max Depth 1): " + (test2Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 3 (Max Depth 2): " + (test3Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 4 (Max Nodes): " + (test4Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 5 (Max Edges): " + (test5Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 6 (Include Data): " + (hasData ? "PASS" : "FAIL"));

            int passCount = (test1Pass ? 1 : 0) + (test2Pass ? 1 : 0) + (test3Pass ? 1 : 0) + (test4Pass ? 1 : 0) + (test5Pass ? 1 : 0) + (hasData ? 1 : 0);
            Console.WriteLine("");
            Console.WriteLine("  Total: " + passCount + "/6 tests passed");
            Console.WriteLine("");

            Console.WriteLine("Cleaning up test data...");
            _Sdk.Graph.DeleteByGuid(tenant.GUID, graph.GUID, force: true).Wait();
            _Sdk.Tenant.DeleteByGuid(tenant.GUID, force: true).Wait();
            Console.WriteLine("  Cleanup complete.");
            Console.WriteLine("");

            #endregion
        }

        private static void TestSubgraphStats()
        {
            Console.WriteLine("");
            Console.WriteLine("=== AUTOMATED SUBGRAPH STATISTICS TEST ===");
            Console.WriteLine("");

            #region Create Tenant and Graph

            Console.WriteLine("Creating test tenant and graph...");
            TenantMetadata tenant = _Sdk.Tenant.Create(new TenantMetadata { Name = "Subgraph Stats Test Tenant" }).Result;
            Console.WriteLine("| Created tenant: " + tenant.GUID);

            Graph graph = _Sdk.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Subgraph Stats Test Graph"
            }).Result;
            Console.WriteLine("| Created graph: " + graph.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Nodes

            Console.WriteLine("Creating test nodes...");
            Node nodeA = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node A (Root)",
                Data = new { Type = "Root", Level = 0 }
            }).Result;
            Console.WriteLine("  | Created Node A: " + nodeA.GUID);

            Node nodeB = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node B (Layer 1)",
                Data = new { Type = "Layer1", Level = 1 }
            }).Result;
            Console.WriteLine("  | Created Node B: " + nodeB.GUID);

            Node nodeC = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node C (Layer 1)",
                Data = new { Type = "Layer1", Level = 1 }
            }).Result;
            Console.WriteLine("  | Created Node C: " + nodeC.GUID);

            Node nodeD = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node D (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            }).Result;
            Console.WriteLine("  | Created Node D: " + nodeD.GUID);

            Node nodeE = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node E (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            }).Result;
            Console.WriteLine("  | Created Node E: " + nodeE.GUID);

            Node nodeF = _Sdk.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node F (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            }).Result;
            Console.WriteLine("  | Created Node F: " + nodeF.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Edges

            Console.WriteLine("Creating test edges...");
            Edge edgeAB = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeA.GUID,
                To = nodeB.GUID,
                Name = "A -> B",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge A->B: " + edgeAB.GUID);

            Edge edgeAC = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeA.GUID,
                To = nodeC.GUID,
                Name = "A -> C",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge A->C: " + edgeAC.GUID);

            Edge edgeBD = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeB.GUID,
                To = nodeD.GUID,
                Name = "B -> D",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge B->D: " + edgeBD.GUID);

            Edge edgeBE = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeB.GUID,
                To = nodeE.GUID,
                Name = "B -> E",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge B->E: " + edgeBE.GUID);

            Edge edgeCF = _Sdk.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeC.GUID,
                To = nodeF.GUID,
                Name = "C -> F",
                Cost = 1
            }).Result;
            Console.WriteLine("  | Created Edge C->F: " + edgeCF.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Labels, Tags, and Vectors

            Console.WriteLine("Creating labels, tags, and vectors...");
            
            // Add labels to nodes
            LabelMetadata labelA1 = _Sdk.Label.Create(new LabelMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeA.GUID,
                Label = "root-node"
            }).Result;
            Console.WriteLine("  | Created Label for Node A: " + labelA1.GUID);

            LabelMetadata labelA2 = _Sdk.Label.Create(new LabelMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeA.GUID,
                Label = "level-0"
            }).Result;
            Console.WriteLine("  | Created Label for Node A: " + labelA2.GUID);

            LabelMetadata labelB1 = _Sdk.Label.Create(new LabelMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeB.GUID,
                Label = "layer-1"
            }).Result;
            Console.WriteLine("  | Created Label for Node B: " + labelB1.GUID);

            LabelMetadata labelC1 = _Sdk.Label.Create(new LabelMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeC.GUID,
                Label = "layer-1"
            }).Result;
            Console.WriteLine("  | Created Label for Node C: " + labelC1.GUID);

            // Add label to edge
            LabelMetadata labelEdgeAB = _Sdk.Label.Create(new LabelMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                EdgeGUID = edgeAB.GUID,
                Label = "primary-edge"
            }).Result;
            Console.WriteLine("  | Created Label for Edge A->B: " + labelEdgeAB.GUID);

            // Add tags to nodes
            TagMetadata tagA1 = _Sdk.Tag.Create(new TagMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeA.GUID,
                Key = "type",
                Value = "root"
            }).Result;
            Console.WriteLine("  | Created Tag for Node A: " + tagA1.GUID);

            TagMetadata tagA2 = _Sdk.Tag.Create(new TagMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeA.GUID,
                Key = "category",
                Value = "start"
            }).Result;
            Console.WriteLine("  | Created Tag for Node A: " + tagA2.GUID);

            TagMetadata tagB1 = _Sdk.Tag.Create(new TagMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeB.GUID,
                Key = "type",
                Value = "child"
            }).Result;
            Console.WriteLine("  | Created Tag for Node B: " + tagB1.GUID);

            // Add tag to edge
            TagMetadata tagEdgeAC = _Sdk.Tag.Create(new TagMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                EdgeGUID = edgeAC.GUID,
                Key = "weight",
                Value = "1"
            }).Result;
            Console.WriteLine("  | Created Tag for Edge A->C: " + tagEdgeAC.GUID);

            // Add vectors to nodes
            VectorMetadata vectorA1 = _Sdk.Vector.Create(new VectorMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeA.GUID,
                Model = "test-model",
                Dimensionality = 3,
                Content = "root node embedding",
                Vectors = new List<float> { 0.1f, 0.2f, 0.3f }
            }).Result;
            Console.WriteLine("  | Created Vector for Node A: " + vectorA1.GUID);

            VectorMetadata vectorB1 = _Sdk.Vector.Create(new VectorMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                NodeGUID = nodeB.GUID,
                Model = "test-model",
                Dimensionality = 3,
                Content = "child node embedding",
                Vectors = new List<float> { 0.4f, 0.5f, 0.6f }
            }).Result;
            Console.WriteLine("  | Created Vector for Node B: " + vectorB1.GUID);

            // Add vector to edge
            VectorMetadata vectorEdgeBD = _Sdk.Vector.Create(new VectorMetadata
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                EdgeGUID = edgeBD.GUID,
                Model = "test-model",
                Dimensionality = 3,
                Content = "edge embedding",
                Vectors = new List<float> { 0.7f, 0.8f, 0.9f }
            }).Result;
            Console.WriteLine("  | Created Vector for Edge B->D: " + vectorEdgeBD.GUID);
            Console.WriteLine("");

            #endregion

            #region Test Subgraph Statistics

            Console.WriteLine("=== TESTING SUBGRAPH STATISTICS ===");
            Console.WriteLine("");
            Console.WriteLine("Graph Structure:");
            Console.WriteLine("  A (root)");
            Console.WriteLine("  ├─> B");
            Console.WriteLine("  │   ├─> D");
            Console.WriteLine("  │   └─> E");
            Console.WriteLine("  └─> C");
            Console.WriteLine("      └─> F");
            Console.WriteLine("");

            // Test Case 1: Max depth 0 (only starting node)
            Console.WriteLine("--- Test Case 1: Max Depth 0 (starting node only) ---");
            GraphStatistics stats1 = _Sdk.Graph.GetSubgraphStatistics(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 0,
                maxNodes: 0,
                maxEdges: 0).Result;

            if (stats1 == null)
            {
                Console.WriteLine("  [ERROR] Stats is null - API call failed");
                Console.WriteLine("");
                return;
            }

            // Verify stats match actual subgraph
            SearchResult result1 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 0,
                maxNodes: 0,
                maxEdges: 0,
                includeData: false,
                includeSubordinates: false).Result;

            int expectedNodes1 = result1.Nodes?.Count ?? 0;
            int expectedEdges1 = result1.Edges?.Count ?? 0;

            Console.WriteLine("  Stats Nodes: " + stats1.Nodes + " (expected: " + expectedNodes1 + ")");
            Console.WriteLine("  Stats Edges: " + stats1.Edges + " (expected: " + expectedEdges1 + ")");
            Console.WriteLine("  Stats Labels: " + stats1.Labels + " (expected: 2 - labels for Node A)");
            Console.WriteLine("  Stats Tags: " + stats1.Tags + " (expected: 2 - tags for Node A)");
            Console.WriteLine("  Stats Vectors: " + stats1.Vectors + " (expected: 1 - vector for Node A)");
            bool test1Pass = stats1.Nodes == expectedNodes1 && stats1.Edges == expectedEdges1 && stats1.Labels == 2 && stats1.Tags == 2 && stats1.Vectors == 1;
            Console.WriteLine("  Result: " + (test1Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("  Response JSON:");
            Console.WriteLine(Serializer.SerializeJson(stats1, true));
            Console.WriteLine("");

            // Test Case 2: Max depth 1 (immediate neighbors)
            Console.WriteLine("--- Test Case 2: Max Depth 1 (immediate neighbors) ---");
            GraphStatistics stats2 = _Sdk.Graph.GetSubgraphStatistics(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 1,
                maxNodes: 0,
                maxEdges: 0).Result;

            if (stats2 == null)
            {
                Console.WriteLine("  [ERROR] Stats is null - API call failed");
                Console.WriteLine("");
                return;
            }

            SearchResult result2 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 1,
                maxNodes: 0,
                maxEdges: 0,
                includeData: false,
                includeSubordinates: false).Result;

            int expectedNodes2 = result2.Nodes?.Count ?? 0;
            int expectedEdges2 = result2.Edges?.Count ?? 0;

            Console.WriteLine("  Stats Nodes: " + stats2.Nodes + " (expected: " + expectedNodes2 + ")");
            Console.WriteLine("  Stats Edges: " + stats2.Edges + " (expected: " + expectedEdges2 + ")");
            Console.WriteLine("  Stats Labels: " + stats2.Labels + " (expected: >= 4 - labels for A, B, C, and edge A->B)");
            Console.WriteLine("  Stats Tags: " + stats2.Tags + " (expected: >= 3 - tags for A, B, and edge A->C)");
            Console.WriteLine("  Stats Vectors: " + stats2.Vectors + " (expected: >= 2 - vectors for A and B)");
            bool test2Pass = stats2.Nodes == expectedNodes2 && stats2.Edges == expectedEdges2 && stats2.Labels >= 4 && stats2.Tags >= 3 && stats2.Vectors >= 2;
            Console.WriteLine("  Result: " + (test2Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("  Response JSON:");
            Console.WriteLine(Serializer.SerializeJson(stats2, true));
            Console.WriteLine("");

            // Test Case 3: Max depth 2 (two layers)
            Console.WriteLine("--- Test Case 3: Max Depth 2 (two layers) ---");
            GraphStatistics stats3 = _Sdk.Graph.GetSubgraphStatistics(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 0,
                maxEdges: 0).Result;

            if (stats3 == null)
            {
                Console.WriteLine("  [ERROR] Stats is null - API call failed");
                Console.WriteLine("");
                return;
            }

            SearchResult result3 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 0,
                maxEdges: 0,
                includeData: false,
                includeSubordinates: false).Result;

            int expectedNodes3 = result3.Nodes?.Count ?? 0;
            int expectedEdges3 = result3.Edges?.Count ?? 0;

            Console.WriteLine("  Stats Nodes: " + stats3.Nodes + " (expected: " + expectedNodes3 + ")");
            Console.WriteLine("  Stats Edges: " + stats3.Edges + " (expected: " + expectedEdges3 + ")");
            Console.WriteLine("  Stats Labels: " + stats3.Labels + " (expected: >= 4 - labels for nodes and edges)");
            Console.WriteLine("  Stats Tags: " + stats3.Tags + " (expected: >= 3 - tags for nodes and edges)");
            Console.WriteLine("  Stats Vectors: " + stats3.Vectors + " (expected: >= 2 - vectors for nodes and edges)");
            bool test3Pass = stats3.Nodes == expectedNodes3 && stats3.Edges == expectedEdges3 && stats3.Labels >= 4 && stats3.Tags >= 3 && stats3.Vectors >= 2;
            Console.WriteLine("  Result: " + (test3Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("  Response JSON:");
            Console.WriteLine(Serializer.SerializeJson(stats3, true));
            Console.WriteLine("");

            // Test Case 4: Max nodes limit
            Console.WriteLine("--- Test Case 4: Max Nodes Limit (3) ---");
            GraphStatistics stats4 = _Sdk.Graph.GetSubgraphStatistics(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 3,
                maxEdges: 0).Result;

            if (stats4 == null)
            {
                Console.WriteLine("  [ERROR] Stats is null - API call failed");
                Console.WriteLine("");
                return;
            }

            SearchResult result4 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 3,
                maxEdges: 0,
                includeData: false,
                includeSubordinates: false).Result;

            int expectedNodes4 = result4.Nodes?.Count ?? 0;
            int expectedEdges4 = result4.Edges?.Count ?? 0;

            Console.WriteLine("  Stats Nodes: " + stats4.Nodes + " (expected: " + expectedNodes4 + ")");
            Console.WriteLine("  Stats Edges: " + stats4.Edges + " (expected: " + expectedEdges4 + ")");
            bool test4Pass = stats4.Nodes == expectedNodes4 && stats4.Edges == expectedEdges4 && stats4.Nodes <= 3;
            Console.WriteLine("  Result: " + (test4Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("  Response JSON:");
            Console.WriteLine(Serializer.SerializeJson(stats4, true));
            Console.WriteLine("");

            // Test Case 5: Max edges limit
            Console.WriteLine("--- Test Case 5: Max Edges Limit (2) ---");
            GraphStatistics stats5 = _Sdk.Graph.GetSubgraphStatistics(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 0,
                maxEdges: 2).Result;

            if (stats5 == null)
            {
                Console.WriteLine("  [ERROR] Stats is null - API call failed");
                Console.WriteLine("");
                return;
            }

            SearchResult result5 = _Sdk.Graph.GetSubgraph(
                tenant.GUID,
                graph.GUID,
                nodeA.GUID,
                maxDepth: 2,
                maxNodes: 0,
                maxEdges: 2,
                includeData: false,
                includeSubordinates: false).Result;

            int expectedNodes5 = result5.Nodes?.Count ?? 0;
            int expectedEdges5 = result5.Edges?.Count ?? 0;

            Console.WriteLine("  Stats Nodes: " + stats5.Nodes + " (expected: " + expectedNodes5 + ")");
            Console.WriteLine("  Stats Edges: " + stats5.Edges + " (expected: " + expectedEdges5 + ")");
            bool test5Pass = stats5.Nodes == expectedNodes5 && stats5.Edges == expectedEdges5 && stats5.Edges <= 2;
            Console.WriteLine("  Result: " + (test5Pass ? "[PASS]" : "[FAIL]"));
            Console.WriteLine("  Response JSON:");
            Console.WriteLine(Serializer.SerializeJson(stats5, true));
            Console.WriteLine("");

            Console.WriteLine("=== TEST SUMMARY ===");
            Console.WriteLine("  Test Case 1 (Max Depth 0): " + (test1Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 2 (Max Depth 1): " + (test2Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 3 (Max Depth 2): " + (test3Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 4 (Max Nodes): " + (test4Pass ? "PASS" : "FAIL"));
            Console.WriteLine("  Test Case 5 (Max Edges): " + (test5Pass ? "PASS" : "FAIL"));

            int passCount = (test1Pass ? 1 : 0) + (test2Pass ? 1 : 0) + (test3Pass ? 1 : 0) + (test4Pass ? 1 : 0) + (test5Pass ? 1 : 0);
            Console.WriteLine("");
            Console.WriteLine("  Total: " + passCount + "/5 tests passed (all tests validate nodes, edges, labels, tags, and vectors)");
            Console.WriteLine("");

            Console.WriteLine("Cleaning up test data...");
            _Sdk.Graph.DeleteByGuid(tenant.GUID, graph.GUID, force: true).Wait();
            _Sdk.Tenant.DeleteByGuid(tenant.GUID, force: true).Wait();
            Console.WriteLine("  Cleanup complete.");
            Console.WriteLine("");

            #endregion
        }

        private static void ShowSampleVectorIndexConfig()
        {
            Console.WriteLine("Sample JSON:");
            Console.WriteLine(Serializer.SerializeJson(new VectorIndexConfiguration
            {
                VectorIndexType = VectorIndexTypeEnum.HnswSqlite,
                VectorIndexFile = "graph-00000000-0000-0000-0000-000000000000-hnsw.db",
                VectorIndexThreshold = null,
                VectorDimensionality = 384,
                VectorIndexM = 16,
                VectorIndexEf = 50,
                VectorIndexEfConstruction = 200
            }, false));
        }

        private static void GraphEnableVectorIndex()
        {
            ShowSampleVectorIndexConfig();
            Guid tenantGuid = GetGuid("Tenant GUID:", _Tenant);
            Guid graphGuid = GetGuid("Graph GUID:", _Graph);
            string json = GetJson("Vector Index Configuration JSON:");
            if (String.IsNullOrEmpty(json)) return;
            
            VectorIndexConfiguration config = Serializer.DeserializeJson<VectorIndexConfiguration>(json);
            _Sdk.Graph.EnableVectorIndexing(tenantGuid, graphGuid, config).Wait();
            Console.WriteLine("Vector indexing enabled successfully.");
        }

        private static void GraphRebuildVectorIndex()
        {
            Guid tenantGuid = GetGuid("Tenant GUID:", _Tenant);
            Guid graphGuid = GetGuid("Graph GUID:", _Graph);
            _Sdk.Graph.RebuildVectorIndex(tenantGuid, graphGuid).Wait();
            Console.WriteLine("Vector index rebuild initiated successfully.");
        }

        private static void GraphDeleteVectorIndex()
        {
            Guid tenantGuid = GetGuid("Tenant GUID:", _Tenant);
            Guid graphGuid = GetGuid("Graph GUID:", _Graph);
            _Sdk.Graph.DeleteVectorIndex(tenantGuid, graphGuid).Wait();
            Console.WriteLine("Vector index deleted successfully.");
        }

        private static void GraphReadVectorIndexConfig()
        {
            Guid tenantGuid = GetGuid("Tenant GUID:", _Tenant);
            Guid graphGuid = GetGuid("Graph GUID:", _Graph);
            EnumerateResult(_Sdk.Graph.ReadVectorIndexConfig(tenantGuid, graphGuid).Result);
        }

        private static void GraphVectorIndexStats()
        {
            Guid tenantGuid = GetGuid("Tenant GUID:", _Tenant);
            Guid graphGuid = GetGuid("Graph GUID:", _Graph);
            EnumerateResult(_Sdk.Graph.GetVectorIndexStatistics(tenantGuid, graphGuid).Result);
        }

        private static void GraphReadAllInTenant()
        {
            EnumerateResult(_Sdk.Graph.ReadAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void GraphDeleteAllInTenant()
        {
            _Sdk.Graph.DeleteAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Wait();
            Console.WriteLine("All graphs in tenant deleted successfully.");
        }

        #endregion

        #region Node

        private static void ShowSampleNode()
        {
            List<string> labels = new List<string>();
            labels.Add("test");

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("key", "value");

            NameValueCollection tags = new NameValueCollection();
            tags.Add("key", "value");

            List<VectorMetadata> vectors = new List<VectorMetadata>
            {
                new VectorMetadata
                {
                    TenantGUID = default(Guid),
                    GraphGUID = default(Guid),
                    NodeGUID = default(Guid),
                    Model = "test model",
                    Dimensionality = 3,
                    Content = "test data",
                    Vectors = new List<float> { 0.1f, 0.2f, 0.3f }
                }
            };

            Console.WriteLine("Sample JSON:");

            Console.WriteLine(Serializer.SerializeJson(new Node
            {
                TenantGUID = _Tenant,
                GraphGUID = _Graph,
                Name = "My node",
                Labels = labels,
                Tags = tags,
                Data = data,
                Vectors = vectors
            }, false));
        }

        private static void NodeExists()
        {
            EnumerateResult(
                _Sdk.Node.ExistsByGuid(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:", _Graph),
                    GetGuid("Node GUID:")
                )
                .Result);
        }

        private static void NodeCreate()
        {
            new Node
            {
                Name = "My node 1",

            };
            ShowSampleNode();
            EnumerateResult(
                _Sdk.Node.Create(Serializer.DeserializeJson<Node>(GetJson("Node JSON:")))
                .Result);
        }

        private static void NodeAll()
        {
            EnumerateResult(_Sdk.Node.ReadMany(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void NodeRead()
        {
            EnumerateResult(
                _Sdk.Node.ReadByGuid(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:", _Graph),
                    GetGuid("Node GUID:")
                )
                .Result);
        }

        private static void NodeEnumerate()
        {
            EnumerateResult(_Sdk.Node.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void NodeUpdate()
        {
            ShowSampleNode();
            EnumerateResult(_Sdk.Node.Update(Serializer.DeserializeJson<Node>(GetJson("Node JSON:"))).Result);
        }

        private static void NodeDelete()
        {
            _Sdk.Node.DeleteByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph), 
                GetGuid("Node GUID:")
                )
                .Wait();
        }
        
        private static void NodeSearch()
        {
            SearchRequest req = BuildSearchRequest(true);
            if (req == null) return;
            EnumerateResult(_Sdk.Node.Search(req).Result);
        }

        private static void NodeEdges()
        {
            EnumerateResult(
                _Sdk.Edge.ReadNodeEdges(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:"),
                    GetGuid("Node GUID:")
                )
                .Result);
        }

        private static void NodeParents()
        {
            EnumerateResult(
                _Sdk.Node.ReadParents(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:"),
                    GetGuid("Node GUID:")
                )
                .Result);
        }

        private static void NodeChildren()
        {
            EnumerateResult(
                _Sdk.Node.ReadChildren(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:"),
                    GetGuid("Node GUID:")
                )
                .Result);
        }

        private static void NodeReadAllInTenant()
        {
            EnumerateResult(_Sdk.Node.ReadAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void NodeReadAllInGraph()
        {
            EnumerateResult(_Sdk.Node.ReadAllInGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void NodeReadMostConnected()
        {
            EnumerateResult(_Sdk.Node.ReadMostConnected(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void NodeReadLeastConnected()
        {
            EnumerateResult(_Sdk.Node.ReadLeastConnected(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void NodeDeleteAllInTenant()
        {
            _Sdk.Node.DeleteAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Wait();
            Console.WriteLine("All nodes in tenant deleted successfully.");
        }

        #endregion

        #region Edge

        private static void ShowSampleEdge()
        {
            List<string> labels = new List<string>();
            labels.Add("test");

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("key", "value");

            NameValueCollection tags = new NameValueCollection();
            tags.Add("key", "value");

            List<VectorMetadata> vectors = new List<VectorMetadata>
            {
                new VectorMetadata
                {
                    TenantGUID = default(Guid),
                    GraphGUID = default(Guid),
                    EdgeGUID = default(Guid),
                    Model = "test model",
                    Dimensionality = 3,
                    Content = "test data",
                    Vectors = new List<float> { 0.1f, 0.2f, 0.3f }
                }
            };

            Console.WriteLine("Sample JSON:");

            Console.WriteLine(Serializer.SerializeJson(new Edge
            {
                TenantGUID = _Tenant,
                GUID = _Graph,
                From = Guid.NewGuid(),
                To = Guid.NewGuid(),
                Name = "My edge",
                Labels = labels,
                Tags = tags,
                Data = data,
                Vectors = vectors
            }, false));
        }

        private static void EdgeExists()
        {
            EnumerateResult(
                _Sdk.Edge.ExistsByGuid(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:", _Graph),
                    GetGuid("Edge GUID:")
                )
                .Result);
        }

        private static void EdgeCreate()
        {
            ShowSampleEdge();
            EnumerateResult(_Sdk.Edge.Create(Serializer.DeserializeJson<Edge>(GetJson("Edge JSON:"))).Result);
        }

        private static void EdgeAll()
        {
            EnumerateResult(
                _Sdk.Edge.ReadMany(
                    GetGuid("Tenant GUID:", _Tenant), 
                    GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void EdgeRead()
        {
            EnumerateResult(
                _Sdk.Edge.ReadByGuid(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:", _Graph),
                    GetGuid("Edge GUID:")
                )
                .Result);
        }

        private static void EdgeEnumerate()
        {
            EnumerateResult(_Sdk.Edge.Enumerate(BuildEnumerationQuery()).Result);
        }

        private static void EdgeUpdate()
        {
            ShowSampleEdge();
            EnumerateResult(_Sdk.Edge.Update(Serializer.DeserializeJson<Edge>(GetJson("Edge JSON:"))).Result);
        }

        private static void EdgeDelete()
        {
            _Sdk.Edge.DeleteByGuid(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Edge GUID:")
                )
                .Wait();
        }

        private static void EdgeSearch()
        {
            SearchRequest req = BuildSearchRequest(true);
            if (req == null) return;
            EnumerateResult(_Sdk.Edge.Search(req).Result);
        }

        private static void EdgesFrom()
        {
            EnumerateResult(
                _Sdk.Edge.ReadEdgesFromNode(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:", _Graph),
                    GetGuid("Node GUID:")
                )
                .Result);
        }

        private static void EdgesTo()
        {
            EnumerateResult(
                _Sdk.Edge.ReadEdgesToNode(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:", _Graph),
                    GetGuid("Edge GUID:")
                )
                .Result);
        }

        private static void EdgesBetween()
        {
            EnumerateResult(
                _Sdk.Edge.ReadEdgesBetweenNodes(
                    GetGuid("Tenant GUID:", _Tenant),
                    GetGuid("Graph GUID:", _Graph),
                    GetGuid("From GUID :"),
                    GetGuid("To GUID   :")
                )
                .Result);
        }

        private static void EdgeReadAllInTenant()
        {
            EnumerateResult(_Sdk.Edge.ReadAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Result);
        }

        private static void EdgeReadAllInGraph()
        {
            EnumerateResult(_Sdk.Edge.ReadAllInGraph(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph)).Result);
        }

        private static void EdgeDeleteAllInTenant()
        {
            _Sdk.Edge.DeleteAllInTenant(
                GetGuid("Tenant GUID:", _Tenant)).Wait();
            Console.WriteLine("All edges in tenant deleted successfully.");
        }

        private static void EdgeDeleteNodeEdges()
        {
            _Sdk.Edge.DeleteNodeEdges(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                GetGuid("Node GUID:")).Wait();
            Console.WriteLine("All edges for node deleted successfully.");
        }

        private static void EdgeDeleteNodeEdgesMany()
        {
            string nodeGuidsInput = Inputty.GetString("Node GUIDs (comma-separated):", null, false);
            if (string.IsNullOrEmpty(nodeGuidsInput)) return;

            var nodeGuidsStrings = nodeGuidsInput.Split(',');
            var nodeGuids = new List<Guid>();
            
            foreach (string guidString in nodeGuidsStrings)
            {
                if (Guid.TryParse(guidString.Trim(), out Guid guid))
                {
                    nodeGuids.Add(guid);
                }
            }

            if (nodeGuids.Count == 0)
            {
                Console.WriteLine("No valid GUIDs provided.");
                return;
            }

            _Sdk.Edge.DeleteNodeEdgesMany(
                GetGuid("Tenant GUID:", _Tenant),
                GetGuid("Graph GUID:", _Graph),
                nodeGuids).Wait();
            Console.WriteLine($"All edges for {nodeGuids.Count} nodes deleted successfully.");
        }

        #endregion

        #region Route

        private static void Route()
        {
            EnumerateResult(
                _Sdk.Node.ReadRoutes(
                    SearchTypeEnum.DepthFirstSearch,
                    GetGuid("Tenant GUID    :", _Tenant),
                    GetGuid("Graph GUID     :", _Graph),
                    GetGuid("From node GUID :"),
                    GetGuid("To node GUID   :")
                )
                .Result);
        }

        #endregion

        #region Vector

        private static void VectorSearch()
        {
            VectorSearchRequest req = BuildVectorSearchRequest();
            EnumerateResult(
                _Sdk.Vector.SearchVectors(
                    req.TenantGUID,
                    req.GraphGUID,
                    req)
                .Result);
        }

        #endregion

        #region Authentication

        private static void AuthGetTenantsForEmail()
        {
            string endpoint = Inputty.GetString("Endpoint:", _Endpoint, false);
            if (String.IsNullOrEmpty(endpoint)) return;
            string email = Inputty.GetString("Email:", null, false);
            if (String.IsNullOrEmpty(email)) return;

            EnumerateResult(LiteGraphSdk.GetTenantsForEmail(email, endpoint));
        }

        private static void AuthGenerateToken()
        {
            string endpoint = Inputty.GetString("Endpoint:", _Endpoint, false);
            if (String.IsNullOrEmpty(endpoint)) return;
            string email = Inputty.GetString("Email:", null, false);
            if (String.IsNullOrEmpty(email)) return;
            string password = Inputty.GetString("Password:", null, false);
            if (String.IsNullOrEmpty(password)) return;
            Guid tenantGuid = GetGuid("Tenant GUID:", _Tenant);

            using (LiteGraphSdk authSdk = new LiteGraphSdk(email, password, tenantGuid, endpoint))
            {
                EnumerateResult(authSdk.UserAuthentication.GenerateToken().Result);
            }
        }

        private static void AuthGetTokenDetails()
        {
            string authToken = Inputty.GetString("Authentication Token:", null, false);
            if (String.IsNullOrEmpty(authToken)) return;
            EnumerateResult(_Sdk.UserAuthentication.GetTokenDetails(authToken).Result);
        }

        #endregion

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
    }
}