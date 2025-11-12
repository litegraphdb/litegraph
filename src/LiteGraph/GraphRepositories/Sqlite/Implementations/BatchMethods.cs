namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;

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
        public async Task<ExistenceResult> Existence(Guid tenantGuid, Guid graphGuid, ExistenceRequest req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            token.ThrowIfCancellationRequested();

            ExistenceResult resp = new ExistenceResult();

            #region Nodes

            if (req.Nodes != null)
            {
                resp.ExistingNodes = new List<Guid>();
                resp.MissingNodes = new List<Guid>();

                string nodesQuery = NodeQueries.BatchExists(tenantGuid, graphGuid, req.Nodes);
                token.ThrowIfCancellationRequested();
                DataTable nodesResult = await _Repo.ExecuteQueryAsync(nodesQuery, false, token).ConfigureAwait(false);
                if (nodesResult != null && nodesResult.Rows != null && nodesResult.Rows.Count > 0)
                {
                    foreach (DataRow row in nodesResult.Rows)
                    {
                        token.ThrowIfCancellationRequested();
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
                token.ThrowIfCancellationRequested();
                DataTable edgesResult = await _Repo.ExecuteQueryAsync(edgesQuery, false, token).ConfigureAwait(false);
                if (edgesResult != null && edgesResult.Rows != null && edgesResult.Rows.Count > 0)
                {
                    foreach (DataRow row in edgesResult.Rows)
                    {
                        token.ThrowIfCancellationRequested();
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
                token.ThrowIfCancellationRequested();
                DataTable betweenResult = await _Repo.ExecuteQueryAsync(betweenQuery, false, token).ConfigureAwait(false);
                if (betweenResult != null && betweenResult.Rows != null && betweenResult.Rows.Count > 0)
                {
                    foreach (DataRow row in betweenResult.Rows)
                    {
                        token.ThrowIfCancellationRequested();
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
