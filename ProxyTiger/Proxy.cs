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
            Status = StatusType.Unknown;
            Type = ProxyType.Unknown;
            Ping = "Unknown";
        }

        public enum StatusType
        {
            Unknown,
            Working,
            NotWorking
        }

        public enum ProxyType
        {
            Unknown,
            Transparent,
            Anonymous,
            HighAnonymous
        }

        public string IP { get; }
        public string Port { get; }
        public StatusType Status { get; set; }
        public ProxyType Type { get; set; }
        public string Ping { get; set; }
    }
}
