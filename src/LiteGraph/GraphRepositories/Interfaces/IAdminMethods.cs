namespace LiteGraph.GraphRepositories.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;

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
        /// <param name="token">Cancellation token.</param>
        Task Backup(string outputFilename, CancellationToken token = default);
    }
}
