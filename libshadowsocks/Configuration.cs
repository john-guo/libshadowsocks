using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shadowsocks
{
    [Serializable]
    public class Server
    {
        public string server;
        public int server_port;
        public string password;
        public string method;

    }

    [Serializable]
    public class Configuration
    {
        public Server server;
        public int localPort;
        public bool shareOverLan;
    }
}
