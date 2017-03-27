using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ProxyTiger
{
    public class Proxy
    {
        private string _ping;

        public Proxy(string ip, string port)
        {
            IP = ip;
            Port = port;
            Status = StatusType.Unknown;
            Type = ProxyType.Unknown;
            Ping = "";
            Color = new SolidColorBrush(Colors.White);
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
            Normal,
            Anonymous,
            HighAnonymous
        }

        public string IP { get; }
        public string Port { get; }
        public StatusType Status { get; set; }
        public ProxyType Type { get; set; }
        public SolidColorBrush Color {get; set;}

        public string Ping
        {
            get { return _ping; }
            set
            {
                if (value == "")
                {
                    _ping = "";
                }
                else
                {
                    _ping = value + "ms";
                }
            }
        }
    }
}
