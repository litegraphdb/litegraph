namespace LiteGraph.GraphRepositories.Interfaces
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
    /// Interface for admin methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IAdminMethods
    {
        /// <summary>
        /// Batch existence request.
        /// </summary>
        /// <param name="outputFilename">Output filename.</param>
        void Backup(string outputFilename);
    }
}
