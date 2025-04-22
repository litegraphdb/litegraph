namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Batch methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class BatchMethods : IBatchMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Batch methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public BatchMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public ExistenceResult Existence(Guid tenantGuid, Guid graphGuid, ExistenceRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            ExistenceResult resp = new ExistenceResult();

            #region Nodes

            if (req.Nodes != null)
            {
                resp.ExistingNodes = new List<Guid>();
                resp.MissingNodes = new List<Guid>();

                string nodesQuery = NodeQueries.BatchExists(tenantGuid, graphGuid, req.Nodes);
                DataTable nodesResult = _Repo.ExecuteQuery(nodesQuery);
                if (nodesResult != null && nodesResult.Rows != null && nodesResult.Rows.Count > 0)
                {
                    foreach (DataRow row in nodesResult.Rows)
                    {
                        if (row["exists"] != null && row["exists"] != DBNull.Value)
                        {
                            int exists = Convert.ToInt32(row["exists"]);
                            if (exists == 1)
                                resp.ExistingNodes.Add(Guid.Parse(row["guid"].ToString()));
                            else
                                resp.MissingNodes.Add(Guid.Parse(row["guid"].ToString()));
                        }
                    }
                }
            }

            #endregion

            #region Edges

            if (req.Edges != null)
            {
                resp.ExistingEdges = new List<Guid>();
                resp.MissingEdges = new List<Guid>();

                string edgesQuery = EdgeQueries.BatchExists(tenantGuid, graphGuid, req.Edges);
                DataTable edgesResult = _Repo.ExecuteQuery(edgesQuery);
                if (edgesResult != null && edgesResult.Rows != null && edgesResult.Rows.Count > 0)
                {
                    foreach (DataRow row in edgesResult.Rows)
                    {
                        if (row["exists"] != null && row["exists"] != DBNull.Value)
                        {
                            int exists = Convert.ToInt32(row["exists"]);
                            if (exists == 1)
                                resp.ExistingEdges.Add(Guid.Parse(row["guid"].ToString()));
                            else
                                resp.MissingEdges.Add(Guid.Parse(row["guid"].ToString()));
                        }
                    }
                }
            }

            #endregion

            #region Edges-Between

            if (req.EdgesBetween != null)
            {
                resp.ExistingEdgesBetween = new List<EdgeBetween>();
                resp.MissingEdgesBetween = new List<EdgeBetween>();

                string betweenQuery = EdgeQueries.BatchExistsBetween(tenantGuid, graphGuid, req.EdgesBetween);
                DataTable betweenResult = _Repo.ExecuteQuery(betweenQuery);
                if (betweenResult != null && betweenResult.Rows != null && betweenResult.Rows.Count > 0)
                {
                    foreach (DataRow row in betweenResult.Rows)
                    {
                        if (row["exists"] != null && row["exists"] != DBNull.Value)
                        {
                            int exists = Convert.ToInt32(row["exists"]);
                            if (exists == 1)
                                resp.ExistingEdgesBetween.Add(new EdgeBetween
                                {
                                    From = Guid.Parse(row["fromguid"].ToString()),
                                    To = Guid.Parse(row["toguid"].ToString())
                                });
                            else
                                resp.MissingEdgesBetween.Add(new EdgeBetween
                                {
                                    From = Guid.Parse(row["fromguid"].ToString()),
                                    To = Guid.Parse(row["toguid"].ToString())
                                });
                        }
                    }
                }
            }

            #endregion

            return resp;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
