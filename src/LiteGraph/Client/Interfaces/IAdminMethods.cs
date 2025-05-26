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
    /// Interface for admin methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface IAdminMethods
    {
        /// <summary>
        /// Backup request.
        /// </summary>
        /// <param name="outputFilename">Output filename.</param>
        void Backup(string outputFilename);
    }
}
