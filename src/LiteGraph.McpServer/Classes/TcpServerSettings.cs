namespace LiteGraph.McpServer.Classes
{
    using System;
    using System.Net;

    /// <summary>
    /// TCP server settings.
    /// </summary>
    public class TcpServerSettings
    {
        #region Public-Members

        /// <summary>
        /// Address on which to listen. Use 0.0.0.0 to indicate any IP address.
        /// </summary>
        public string Address
        {
            get
            {
                return _Address;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Address));
                if (!value.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    IPAddress.Parse(value).ToString();
                }
                _Address = value;
            }
        }

        /// <summary>
        /// TCP server port.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Address = "127.0.0.1";
        private int _Port = 8201;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServerSettings"/> class.
        /// </summary>
        public TcpServerSettings()
        {
        }

        #endregion
    }
}

