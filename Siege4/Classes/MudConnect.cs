using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace Siege4.Classes
{
    /// <summary>
    /// A simple class for handling a threaded connection
    /// with public events for sending and recieving messages.
    /// </summary>
    public class Network
    {
        /// <summary>
        /// Refers to logical states for a given connection.
        /// </summary>
        public enum ConnectionState
        {
            /// <summary>
            /// The connection is attempting to connect with the remote host.
            /// </summary>
            Connecting,
            /// <summary>
            /// A connection is established.
            /// </summary>
            Connected,
            /// <summary>
            /// The connection is being severed for whatever reason.
            /// </summary>
            Disconnecting,
            /// <summary>
            /// Currently not connected to any server.
            /// </summary>
            Disconnected,
            /// <summary>
            /// There was an error.
            /// </summary>
            Error
        }
        /// <summary>
        /// To be used for informing the user of connnection changes.
        /// </summary>
        public ConnectionState NetworkConnection;

        #region  Mudconnect Properties

        private TcpClient tcpclient;
        private NetworkStream ns;

        private DateTime LastActivity;
        private string input = String.Empty;
        private bool connected;
        private string remotehost;
        private string remoteport;

        /// <summary>
        /// If set to falase while the network is working will call itself to disconnect.
        /// </summary>
        public bool Connected
        {
            get
            {
                return this.connected;
            }
            set
            {
                if (connected == true)
                    ConnectionChanged(this, new MudConnectionArgs(ConnectionState.Disconnecting, String.Format("[{0}] User Request for Disconnect.", DateTime.Now)));
                connected = value;

            }
        }

        /// <summary>
        /// The message to be sent to the Remote-Host.
        /// Raises and internal event to send the message when changed.
        /// </summary>
        public string Input
        {
            get
            {
                return input;
            }
            set
            {
                input = value;
                if (value != String.Empty)
                    MudConnect_SendMessage(this, new SendMessageArgs(value));
            }
        }

        /// <summary>
        /// A DNS or IP address of the Remote metwork server.  (The MUD)
        /// </summary>
        public string RemoteHost
        {
            get { return remotehost; }
            set { remotehost = value; }
        }
        /// <summary>
        /// A string of the Integer for the Remote Port of the MUD.
        /// </summary>
        public string RemotePort
        {
            get
            {
                return remoteport;
            }
            set
            {
                remoteport = value;
            }
        }
        #endregion

        #region Events n' Delegates
        /// <summary>
        /// Raised whenever the network has changed to a represeted ConnectionState enum.
        /// </summary>
        public event EventHandler<MudConnectionArgs> ConnectionChanged;
        /// <summary>
        /// Hook into this when you would like know when the server has sent a message.
        /// </summary>
        public event EventHandler<MessageReceivedArgs> MessageReceived;
        private event EventHandler<SendMessageArgs> SendMessage;

        /// <summary>
        /// Raised when a connection has changed and includes a brief message describing it.
        /// </summary>
        public class MudConnectionArgs : EventArgs
        {
            /// <summary>
            /// A logical connection state
            /// </summary>
            public ConnectionState Connection_State;
            /// <summary>
            /// The Message to be sent to whatever is handling it.
            /// </summary>
            public string Message = String.Empty;
            /// <summary>
            /// Basicly a message to be forwarded about a Connection change. 
            /// </summary>
            /// <param name="Connection_State"></param>
            /// <param name="Message"></param>
            public MudConnectionArgs(ConnectionState Connection_State, string Message)
            {
                this.Connection_State = Connection_State;
                this.Message = Message;
            }
        }
        /// <summary>
        /// When a message is received handle it.
        /// </summary>
        public class MessageReceivedArgs : EventArgs
        {
            /// <summary>
            /// The message received.
            /// </summary>
            public string Message = String.Empty;
            /// <summary>
            /// Message is ready.
            /// </summary>
            /// <param name="Message"></param>
            public MessageReceivedArgs(string Message)
            {
                this.Message = Message;
            }
        }
        private class SendMessageArgs : EventArgs
        {
            public string message = String.Empty;
            public SendMessageArgs(string Message)
            {
                this.message = Message;
            }
        }

        private void CC(ConnectionState cc, string message)
        {
            this.NetworkConnection = cc;
            EventHandler<MudConnectionArgs> con = ConnectionChanged;
            if (con != null)
                ConnectionChanged(this, new MudConnectionArgs(cc, message));

        }
        private void MR(string message)
        {
            EventHandler<MessageReceivedArgs> mr = MessageReceived;
            if (mr != null)
                MessageReceived(this, new MessageReceivedArgs(message));
        }
        private void SM(string message)
        {
            EventHandler<SendMessageArgs> sm = SendMessage;
            if (sm != null)
                SM(message);
        }

        #endregion

        /// <summary>
        /// Inialize MudConnect library with the provided
        /// remote connection information and set NetworkConnection
        /// to Disconnected.
        /// </summary>
        /// <param name="RemoteHost"></param>
        /// <param name="RemotePort"></param>
        public Network(string RemoteHost, string RemotePort)
        {
            this.remotehost = RemoteHost;
            this.remoteport = RemotePort;
            this.NetworkConnection = ConnectionState.Disconnected;
        }

        /// <summary>
        /// A wrapper for the internal Listen method.
        /// Calls listen from within a try-catch statement for
        /// error handling.
        /// </summary>
        /// <param name="state"></param>
        public void Connect(object state)
        {
            try
            {
                this.LastActivity = DateTime.Now;
                CC(ConnectionState.Connecting, String.Format("Attempting a connection with {0}:{1}...", this.RemoteHost, this.RemotePort));
                Listen();
            }
            catch (Exception e)
            {
                this.connected = false;
                CC(ConnectionState.Error, e.Message);
            }
        }
        private void Listen()
        {
            using (tcpclient = new TcpClient(remotehost, Convert.ToInt32(remoteport)))
            {
                using (ns = tcpclient.GetStream())
                {
                    ns.WriteTimeout = 2000; this.connected = true;
                    CC(ConnectionState.Connected, String.Format("Established with `{0}:{1}`", remotehost, remoteport));

                    while (this.connected)
                    {
                        Thread.Sleep(50);
                        if (ns.CanRead)
                        {
                            if (ns.DataAvailable)
                                ReadMessage();
                        }
                    }
                    this.connected = false;
                    CC(ConnectionState.Disconnected, String.Format("Disconnected from `{0}:{1}`", remotehost, remoteport));
                }
            }

        }

        void ReadMessage()
        {
            StringBuilder sb = new StringBuilder();

            while (ns.DataAvailable)
                sb.Append(Convert.ToChar(ns.ReadByte()));

            if (sb.ToString() != String.Empty)
                MR(sb.ToString());

            this.LastActivity = DateTime.Now;
        }

        void WriteMessage(object buffer)
        {
            //this.LastActivity = DateTime.Now;
        }

        void MudConnect_SendMessage(object sender, Network.SendMessageArgs e)
        {

            lock (this.Input)
            {
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(e.message);
                try
                {
                    ns.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(WriteMessage), buffer);
                }
                catch (Exception ex)
                {
                    connected = false;
                    CC(ConnectionState.Error, String.Format("[{0}] >> {1}.", DateTime.Now, ex.Message));
                    return;
                }
                this.Input = String.Empty;
            }
        }
    }
}
