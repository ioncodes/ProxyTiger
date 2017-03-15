﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using ActiproSoftware.Windows.Themes;

namespace ProxyTiger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // http://sguru.org/files/proxy-source_sguru.txt
        private readonly List<Task> _tasks = new List<Task>();
        private bool _stop = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnScrape_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            //FreeProxyListsNet();
            LblStatus.Text = "Scraping";
            _tasks.Add(HideMyName());
            _tasks.Add(SamairRu());
            _tasks.Add(GetProxyJp());
            _tasks.Add(ProxyDb());
            _tasks.Add(ProxySpy());
            _tasks.Add(ProxyListOrg());
            _tasks.Add(NnTime());
            _tasks.Add(MultiProxy());
            _tasks.Add(ProxyMore());
            _tasks.Add(FreeTao());
            _tasks.Add(MorphIo());
            _tasks.Add(IpAddress());
            _tasks.Add(MeilleurVpn());
            _tasks.Add(HideMyIp());
            _tasks.Add(WebBox());
            _tasks.Add(HOne());
            _tasks.AddRange(RmcCurdy());
            foreach (var task in _tasks)
            {
                task.Start();
            }
            new Task(async () =>
            {
                await Task.WhenAll(_tasks.ToArray());
                Application.Current.Dispatcher.Invoke((Action) (() => { LblStatus.Text = "Idle"; }));
                _stop = false;
                MessageBox.Show("Done");
            }).Start();
        }

        private Task HideMyName()
        {
            string url = "https://hidemy.name/en/proxy-list/?start=";
            Task task = new Task(() =>
            {
                for (int i = 0; i < 30; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i * 64);
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3})</td><td>([0-9]{1,4})"))
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private Task SamairRu()
        {
            string url = "http://samair.ru/proxy/list-IP-port/proxy-{0}.htm";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 20; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url.Replace("{0}", i.ToString()));
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private Task GetProxyJp()
        {
            string url = "http://www.getproxy.jp/en/default/";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 5; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i);
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private Task ProxyDb()
        {
            string url = "http://proxydb.net/?offset=";
            Task task = new Task(() =>
            {
                for (int i = 0; i <= 50; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i * 50);
                    if (source.Contains("You are sending requests too fast, please wait a moment "))
                    {
                        var timeoutString = source.Replace("You are sending requests too fast, please wait a moment (",
                            "");
                        timeoutString = timeoutString.Replace("s) and try again.", "");
                        var timeout = Convert.ToInt32(timeoutString);
                        Application.Current.Dispatcher.Invoke(
                            (Action) (() => { LblAdditionalInfo.Text = "Waiting for a provider to unblock us..."; }));
                        Thread.Sleep(timeout * 1024);
                        Application.Current.Dispatcher.Invoke((Action) (() => { LblAdditionalInfo.Text = ""; }));
                    }
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:([0-9]{1,5})?"))
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                    Thread.Sleep(1000);
                }
            });
            return task;
        }

        private Task ProxySpy()
        {
            string url = "http://txt.proxyspy.net/proxy.txt";
            Task task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            return task;
        }

        private Task FreeProxyListsNet()
        {
            string url = "http://www.freeproxylists.net/?page="; // + 10 i think
            // uses IPDecode() => IPDecode("%3c%61%20%68%72%65%66%3d%22%68%74%74%70%3a%2f%2f%77%77%77%2e%66%72%65%65%70%72%6f%78%79%6c%69%73%74%73%2e%6e%65%74%2f%32%31%38%2e%39%30%2e%31%37%34%2e%31%36%37%2e%68%74%6d%6c%22%3e%32%31%38%2e%39%30%2e%31%37%34%2e%31%36%37%3c%2f%61%3e")
            return null;
        }

        private Task ProxyListOrg()
        {
            string url = "https://proxy-list.org/german/search.php?search=&country=any&type=any&port=any&ssl=any&p=";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i);
                    foreach (
                        Match match in
                        Regex.Matches(source, "Proxy\\(\'([a-zA-Z0-9=]+)\'\\)"))
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            string[] proxy =
                                Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups[1].Value)).Split(':');
                            LvProxies.Items.Add(new Proxy(proxy[0], proxy[1]));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private Task NnTime()
        {
            string url = "http://nntime.com/proxy-list-{0}.htm";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 18; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    string id = "";
                    if (i >= 1 && i <= 9)
                        id = "0" + i;
                    else
                        id = i.ToString();
                    var source = wc.DownloadString(url.Replace("{0}", id));
                    string keyStore =
                        Regex.Match(source, "<script src=\"js1\\/.*<\\/script><script type=.*>\\n(.*);").Groups[1].Value;
                    Dictionary<string, int> keys =
                        keyStore.Split(';')
                            .Select(key => key.Split('='))
                            .ToDictionary(k => k[0], k => Convert.ToInt32(k[1]));
                    foreach (
                        Match match in
                        Regex.Matches(source,
                            ">([0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3})[<\\w =\"\\/>]+\\.write\\(\":\"([a-z+]+)\\)")
                    )
                    {
                        string ip = match.Groups[1].Value;
                        string[] values = match.Groups[2].Value.Split('+');
                        string port = values.Where(value => value != "")
                            .Aggregate("", (current, value) => current + keys[value]);
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(ip, port));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private Task MultiProxy()
        {
            string url = "http://multiproxy.org/txt_all/proxy.txt";
            Task task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            return task;
        }

        private List<Task> RmcCurdy()
        {
            string url = "https://www.rmccurdy.com/scripts/proxy/output/http/good.txt";
            var tasks = new List<Task>();
            Task task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            tasks.Add(task);

            url = "https://www.rmccurdy.com/scripts/proxy/output/socks/good.txt";
            task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            tasks.Add(task);

            url = "https://www.rmccurdy.com/scripts/proxy/good.txt";
            task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            tasks.Add(task);
            return tasks;
        }

        private Task ProxyMore()
        {
            string url = "http://www.proxymore.com/proxy-list-{0}.html";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 5; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url.Replace("{0}", i.ToString()));
                    foreach (
                        Match match in
                        Regex.Matches(source,
                            "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})<\\/td>\\s+<td align=\"center\">([0-9]{1,5})<\\/td>")
                    )
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private Task FreeTao()
        {
            string url = "http://freetao.org/socks/index.php?act=list&port=&type=&country=&page=";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 2; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i);
                    foreach (
                        Match match in
                        Regex.Matches(source, ">([0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3})[\\S]+\\n<td>([0-9]{1,5})<")
                    )
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private Task MorphIo()
        {
            string url = "https://morph.io/CookieMichal/us-proxy";
            Task task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            return task;
        }

        private Task IpAddress()
        {
            string url = "http://ipaddress.com/proxy-list/";
            Task task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:([0-9]{1,5})"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            return task;
        }

        private Task MeilleurVpn()
        {
            string url = "https://meilleurvpn.org/proxy/";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 40; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.UploadString(url, "pageNo=" + i);
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}).+\\s+[<td>]+([0-9]{1,5})<")
                    )
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private Task HideMyIp()
        {
            string url = "https://www.hide-my-ip.com/proxylist.shtml";
            Task task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source,
                        "{\"i\":\"([0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3})\",\"p\":\"([0-9]{1,5})"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            return task;
        }

        private Task WebBox()
        {
            string url = "http://proxy.web-box.ru/proxy-list";
            Task task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:([0-9]{1,5})"))
                {
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            return task;
        }

        private Task HOne()
        {
            string url = "http://notan.h1.ru/hack/xwww/proxy{0}.html";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url.Replace("{0}", i.ToString()));
                    foreach (
                        Match match in  
                        Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:([0-9]{1,5})"))
                    {
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            });
            return task;
        }

        private void BtnStop_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            LblStatus.Text = "Stopping";
            _stop = true;
        }

        private void BtnCopyProxies_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            var proxies = new List<string>();
            foreach (Proxy proxy in LvProxies.Items)
            {
                proxies.Add($"{proxy.IP}:{proxy.Port}");
            }
            Clipboard.SetText(string.Join("\n", proxies));
        }

        private void BtnCheck_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            LblStatus.Text = "Scanning";
            int working = 0;
            int notWorking = 0;
            new Thread(() =>
            {
                int threads = 0;
                foreach (Proxy proxy in LvProxies.Items)
                {
                    while (threads >= 200) { }
                    new Thread(() =>
                    {
                        try
                        {
                            threads++;
                            bool status = false;
                            bool checkOnline = true;
                            Application.Current.Dispatcher.Invoke((Action) (() =>
                            {
                                checkOnline = CbCheckOnline.IsChecked.Value;
                            }));
                            //if (!checkOnline)
                            //    status = SocketConnect(proxy.IP, Convert.ToInt32(proxy.Port));
                            if (!checkOnline)
                                status = CheckProxy(proxy.IP, proxy.Port);
                            else
                            {
                                var data = CheckProxyOnline(proxy.IP, proxy.Port);
                                status = data.working;
                                proxy.Ping = data.ping;
                                proxy.Type = data.proxyType;
                            }
                            proxy.Status = status
                                ? Proxy.StatusType.Working
                                : Proxy.StatusType.NotWorking;
                            if (status) working++;
                            else notWorking++;
                        }
                        catch
                        {
                            proxy.Status = Proxy.StatusType.NotWorking;
                            notWorking++;
                        }
                        finally
                        {
                            try
                            {
                                Application.Current.Dispatcher.Invoke((Action) (() =>
                                {
                                    LblAdditionalInfo.Text = $"Working: {working}, Not Working: {notWorking}";
                                }));
                            }
                            catch { }
                            threads--;
                        }
                    }).Start();
                }
            }).Start();
        }

        private bool SocketConnect(string host, int port)
        {
            var isSuccess = false;
            try
            {
                var connsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connsock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 50000);
                var hip = IPAddress.Parse(host);
                var ipep = new IPEndPoint(hip, port);
                connsock.Connect(ipep);
                if (connsock.Connected)
                {
                    isSuccess = true;
                }
                connsock.Close();
            }
            catch (Exception)
            {
                isSuccess = false;
            }
            return isSuccess;
        }

        private (bool working, string ping, string country, Proxy.ProxyType proxyType) CheckProxyOnline(string ip, string port)
        {
            string url = "http://api.proxyipchecker.com/pchk.php";
            string[] response = new WebClient().UploadString(url, $"ip={ip}&port={port}").Split(';');
            string ping = response[0];
            string country = response[2];
            string typeString = response[3];
            Proxy.ProxyType type;
            switch (typeString)
            {
                case "0":
                    type = Proxy.ProxyType.Unknown;
                    break;
                case "1":
                    type = Proxy.ProxyType.Transparent;
                    break;
                case "2":
                    type = Proxy.ProxyType.Anonymous;
                    break;
                default:
                    type = Proxy.ProxyType.HighAnonymous;
                    break;
            }
            bool working = !(ping == "0" && typeString == "0");
            return (working, ping, country, type);
        }

        private bool IsProxyUp(WebProxy proxy)
        {
            bool result = false;
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Proxy = proxy;
                    result = (webClient.DownloadString("https://api.ipify.org/") == proxy.Address.Host);
                }
            }
            catch
            {
                return false;
            }
            return result;
        }

        private bool CheckProxy(string ip, string port)
        {
            try
            {
                return IsProxyUp(new WebProxy(ip, Convert.ToInt32(port)));
            }
            catch
            {
                return false;
            }
        }
    }
}