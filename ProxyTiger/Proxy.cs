using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTiger
{
    public class Proxy
    {
        public Proxy(string ip, string port)
        {
            IP = ip;
            Port = port;
        }

        public string IP { get; }
        public string Port { get; }
    }
}
