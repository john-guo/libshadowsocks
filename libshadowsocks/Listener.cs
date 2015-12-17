using Shadowsocks;
using Shadowsocks.Relay;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Shadowsocks
{
    public class Listener
    {
        public interface Service
        {
            bool Handle(byte[] firstPacket, int length, Socket socket, object state);
            void Close();

            event Action OnClose;
        }

        public class UDPState
        {
            public byte[] buffer = new byte[4096];
            public EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 1);
        }

        Configuration _config;
        bool _shareOverLAN;
        Socket _tcpSocket;
        Socket _udpSocket;
        IList<Service> _services;


        public event Action OnBroken;

        private bool broken = false;
        private bool forced = false;
        private void Broken()
        {
            if (broken)
                return;
            broken = true;
            if (forced)
                return;
            if (OnBroken != null)
                OnBroken();
        }

        public Listener()
        {
            this._services = new List<Service>();
        }

        public void AddService(Service svc)
        {
            this._services.Add(svc);
        }

        private bool CheckIfPortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return true;
                }
            }
            return false;
        }

        public void Start(string server, int port, string password, string method, int lport = 1080, bool localOnly = true)
        {
            var config = new Configuration
            {
                server = new Server
                {
                    server = server,
                    server_port = port,
                    password = password,
                    method = method
                },
                localPort = lport,
                shareOverLan = !localOnly
            };

            Start(config);
        }

        public void Start(Configuration config)
        {
            this._config = config;

            if (CheckIfPortInUse(_config.localPort))
                throw new SSPortAlreadyInUseException();

            this._shareOverLAN = config.shareOverLan;

            var tcpservice = new TCPRelay(_config);
            tcpservice.OnClose += this.Broken;

            var udpservice = new UDPRelay(_config);
            udpservice.OnClose += this.Broken;

            AddService(tcpservice);
            AddService(udpservice);

            try
            {
                // Create a TCP/IP socket.
                _tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                IPEndPoint localEndPoint = null;
                if (_shareOverLAN)
                {
                    localEndPoint = new IPEndPoint(IPAddress.Any, _config.localPort);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Loopback, _config.localPort);
                }

                // Bind the socket to the local endpoint and listen for incoming connections.
                _tcpSocket.Bind(localEndPoint);
                _udpSocket.Bind(localEndPoint);
                _tcpSocket.Listen(1024);


                // Start an asynchronous socket to listen for connections.
                Trace.WriteLine("Shadowsocks started");
                _tcpSocket.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    _tcpSocket);
                UDPState udpState = new UDPState();
                _udpSocket.BeginReceiveFrom(udpState.buffer, 0, udpState.buffer.Length, 0, ref udpState.remoteEndPoint, new AsyncCallback(RecvFromCallback), udpState);
            }
            catch (SocketException)
            {
                _tcpSocket.Close();
                this.Broken();
                throw;
            }
        }

        public void Stop()
        {
            forced = true;

            if (_tcpSocket != null)
            {
                _tcpSocket.Close();
                _tcpSocket = null;
            }
            if (_udpSocket != null)
            {
                _udpSocket.Close();
                _udpSocket = null;
            }
            foreach (Service service in _services)
            {
                service.Close();
            }

            _services.Clear();
        }

        public void RecvFromCallback(IAsyncResult ar)
        {
            UDPState state = (UDPState)ar.AsyncState;
            try
            {
                int bytesRead = _udpSocket.EndReceiveFrom(ar, ref state.remoteEndPoint);
                foreach (Service service in _services)
                {
                    if (service.Handle(state.buffer, bytesRead, _udpSocket, state))
                    {
                        break;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {
                this.Broken();
            }
            finally
            {
                try
                {
                    _udpSocket.BeginReceiveFrom(state.buffer, 0, state.buffer.Length, 0, ref state.remoteEndPoint, new AsyncCallback(RecvFromCallback), state);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing
                }
                catch (Exception)
                {
                    this.Broken();
                }
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            try
            {
                Socket conn = listener.EndAccept(ar);

                byte[] buf = new byte[4096];
                object[] state = new object[] {
                    conn,
                    buf
                };

                conn.BeginReceive(buf, 0, buf.Length, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                this.Broken();
            }
            finally
            {
                try
                {
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing
                }
                catch (Exception e)
                {
                    Logging.LogUsefulException(e);
                }
            }
        }


        private void ReceiveCallback(IAsyncResult ar)
        {
            object[] state = (object[])ar.AsyncState;

            Socket conn = (Socket)state[0];
            byte[] buf = (byte[])state[1];
            try
            {
                int bytesRead = conn.EndReceive(ar);
                foreach (Service service in _services)
                {
                    if (service.Handle(buf, bytesRead, conn, null))
                    {
                        return;
                    }
                }
                // no service found for this
                if (conn.ProtocolType == ProtocolType.Tcp)
                {
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                conn.Close();
                this.Broken();
            }
        }
    }
}
