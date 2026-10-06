namespace Test.Aot
{
    using System.Collections.Generic;

    /// <summary>
    /// Application-defined data stored in Node.Data, registered through AotAppJsonContext.
    /// </summary>
    public class AotAppData
    {
        #region Public-Members

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Score.
        /// </summary>
        public int Score { get; set; } = 0;

        /// <summary>
        /// Status.
        /// </summary>
        public AotAppStatusEnum Status { get; set; } = AotAppStatusEnum.Active;

        /// <summary>
        /// Keywords.
        /// </summary>
        public List<string> Keywords { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AotAppData()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
