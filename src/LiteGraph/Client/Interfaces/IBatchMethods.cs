namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Serialization;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// Interface for batch methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface IBatchMethods
    {
        /// <summary>
        /// Batch existence request.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="req">Existence request.</param>
        /// <returns>Existence result.</returns>
        ExistenceResult Existence(Guid tenantGuid, Guid graphGuid, ExistenceRequest req);
    }
}
