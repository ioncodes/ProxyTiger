using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using Microsoft.Win32;
using Color = System.Drawing.Color;

namespace ProxyTiger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region UI

        // http://sguru.org/files/proxy-source_sguru.txt
        private readonly List<Task> _tasks = new List<Task>();
        private bool _stop = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnStop_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            LblStatus.Text = "Stopping";
            _stop = true;
        }

        private void BtnCopyProxies_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            var proxies = new List<string>();
            foreach (Proxy proxy in LvProxies.Items)
            {
                proxies.Add($"{proxy.IP}:{proxy.Port}");
            }
            Clipboard.SetText(string.Join("\n", proxies));
        }

        private void BtnCheck_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            LblStatus.Text = "Scanning";
            int working = 0;
            int notWorking = 0;
            new Thread(() =>
            {
                int threads = 0;
                foreach (Proxy proxy in LvProxies.Items)
                {
                    while (threads >= 200)
                    {
                    }
                    new Thread(() =>
                    {
                        try
                        {
                            threads++;
                            Stopwatch sw = new Stopwatch();
                            sw.Start();
                            var status = CheckProxy(proxy.IP, proxy.Port);
                            sw.Stop();
                            if(status != Proxy.ProxyType.Unknown)
                            {
                                proxy.Ping = sw.Elapsed.Milliseconds.ToString();
                                proxy.Type = status;
                                proxy.Color = new SolidColorBrush(Colors.Green);
                                working++;
                            }
                            else
                            {
                                proxy.Color = new SolidColorBrush(Colors.Red);
                                notWorking++;
                            }
                            proxy.Status = status != Proxy.ProxyType.Unknown
                                ? Proxy.StatusType.Working
                                : Proxy.StatusType.NotWorking;
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
                                Application.Current.Dispatcher.Invoke(
                                    (Action)
                                    (() =>
                                    {
                                        LblAdditionalInfo.Text = $"Working: {working}, Not Working: {notWorking}";
                                    }));
                            }
                            catch
                            {
                            }
                            threads--;
                        }
                    }).Start();
                }
            }).Start();
        }

        private Proxy.ProxyType IsProxyUp(WebProxy proxy)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("http://samair.ru/anonymity-test/");
            try
            {
                httpWebRequest.Timeout = 5000;
                httpWebRequest.Proxy = proxy;
                HttpWebResponse httpWebResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                string text2 = streamReader.ReadToEnd();
                if (!text2.Contains("Your IP is detected"))
                {
                    if (text2.Contains(">high-anonymous (elite) proxy</"))
                    {
                        return Proxy.ProxyType.HighAnonymous;
                    }
                    else
                    {
                        return Proxy.ProxyType.Anonymous;
                    }
                }
                else
                {
                    return Proxy.ProxyType.Normal;
                }
            }
            catch
            {
                return Proxy.ProxyType.Unknown;
            }
        }

        private Proxy.ProxyType CheckProxy(string ip, string port)
        {
            return IsProxyUp(new WebProxy(ip, Convert.ToInt32(port)));
        }

        private void BtnScrape_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            LblStatus.Text = "Scraping";
            _tasks.Add(HideMyName()); //216 out of 785
            _tasks.Add(SamairRu()); //144 out of 600
            _tasks.Add(GetProxyJp()); //6 out of 150...all anon wtf
            _tasks.Add(ProxyDb()); //90 out of 950
            _tasks.Add(ProxySpy()); //67 out of 300
            _tasks.Add(ProxyListOrg()); //45 out of 140
            _tasks.Add(NnTime()); //80 out of 600
            _tasks.Add(ProxyMore()); //14 out of 125
            _tasks.Add(FreeTao()); //4 out of 27
            _tasks.Add(MorphIo()); //204 out of 2046
            _tasks.Add(IpAddress()); //11 out of 50
            _tasks.Add(MeilleurVpn()); //40 out of 180
            _tasks.Add(HideMyIp()); //80 out of 445
            _tasks.AddRange(RmcCurdy()); //142 out of 2091
            _tasks.Add(SslProxies()); //52 out of 100
            _tasks.Add(ProxyApe()); //213 out of 3100
            _tasks.Add(OrcaTech()); // 1200 out of 3000
            _tasks.Add(SslProxies24()); // we need to only scrape from the day of scrapings posts not all time
            _tasks.Add(AliveProxy()); //23 out of 223
            foreach (var task in _tasks)
            {
                task.Start();
            }
            new Task(async () =>
            {
                await Task.WhenAll(_tasks.ToArray());
                _stop = false;
                Application.Current.Dispatcher.Invoke((Action) (() =>
                {
                    LblStatus.Text = "Idle";
                    new MsgBox("ProxyTiger", "Scraped " + LvProxies.Items.Count + " proxies.").ShowDialog();
                }));
            }).Start();
        }

        private void BtnExportProxies_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            var proxies = (from Proxy proxy in LvProxies.Items select $"{proxy.IP}:{proxy.Port}").ToList();
            SaveFileDialog sfd = new SaveFileDialog
            {
                InitialDirectory = @"C:\",
                Title = "Save proxies",
                DefaultExt = "txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (sfd.ShowDialog() == true)
            {
                File.WriteAllLines(sfd.FileName, proxies);
                new MsgBox("ProxyTiger", "Wrote successfully " + proxies.Count + " proxies.").Show();
            }
        }

        private void BtnPasteProxies_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            var proxies = Clipboard.GetText().Split('\n').Select(proxy => proxy.Split(':')).Select(p => new Proxy(p[0], p[1])).ToList();
            foreach (var proxy in proxies)
            {
                LvProxies.Items.Add(proxy);
            }
        }

        private void BtnImportProxies_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                InitialDirectory = @"C:\",
                Title = "Browse Text Files",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true
            };
            if (ofd.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(ofd.FileName);
                foreach (var proxy in lines)
                {
                    var p = proxy.Split(':');
                    LvProxies.Items.Add(new Proxy(p[0], p[1]));
                    new MsgBox("ProxyTiger", "Imported " + lines.Length + " proxies.").ShowDialog();
                }
            }
        }

        #endregion

        #region Modules

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
            string url = "http://www.freeproxylists.net/?page=";
            Task task = new Task(() =>
            {
                for (int i = 0; i <= 15; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i);

                    foreach (
                        Match match in
                        Regex.Matches(source,
                            "IPDecode\\(\"([%0-9a-z]+)\"\\)<\\/script><\\/td><td align=\"center\">([0-9]{1,5})<"))
                    {
                        string ip =
                            Regex.Match(match.Value, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})").Groups[1]
                                .Value;
                        string port = match.Groups[2].Value;
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

        private Task WorkingProxies()
        {
            string url = "http://workingproxies.org/?page=";
            Task task = new Task(() =>
            {
                for (int i = 0; i < 40; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i);
                    foreach (
                        Match match in
                        Regex.Matches(source,
                            "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})[<\\/td>\\n]+.*<td>([0-9]{1,5})<"))
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

        private Task SslProxies()
        {
            string url = "https://www.sslproxies.org/";
            Task task = new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source,
                        "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})[<\\/td>\\n]+.*<td>([0-9]{1,5})<"))
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

        private Task CloudProxies()
        {
            string url = "http://cloudproxies.com/proxylist/?page=";
            Task task = new Task(() =>
            {
                for (int i = 1; i <= 33; i++)
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
                            "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})[<\\/td>\\n]+.*<td>([0-9]{1,5})<"))
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

        private Task ProxyApe()
        {
            string url = "http://proxyape.com/";
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

        private Task ProxyListMe()
        {
            string url = "http://proxylist.me/proxys/index/";
            Task task = new Task(() =>
            {
                for (int i = 0; i < 114; i++)
                {
                    if (_stop)
                        break;
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i * 20);
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

        private Task OrcaTech()
        {
            string url = "https://orca.tech/community-proxy-list/";
            Task task = new Task(() =>
            {
                List<string> urls = new List<string>();
                var matches = Regex.Matches(new WebClient().DownloadString(url), "\'([0-9]{5,10}\\.txt)\'");
                for (var index = 0;
                    index < matches.Count;
                    index++)
                {
                    if (index == 20 || _stop)
                        break;
                    Match match = matches[index];
                    urls.Add("https://orca.tech/community-proxy-list/" + match.Groups[1].Value);
                }
                foreach (var file in urls)
                {
                    if (_stop)
                        break;
                    string source = new WebClient().DownloadString(file);
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}):([0-9]{1,5})"))
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

        private Task SslProxies24()
        {
            string url = "http://sslproxies24.blogspot.com/feeds/posts/default";
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
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            });
            return task;
        }

        private Task AliveProxy()
        {
            string[] urls =
            {
                "http://aliveproxy.com/anonymous-proxy-list/",
                "http://aliveproxy.com/ca-proxy-list/",
                "http://aliveproxy.com/de-proxy-list/",
                "http://aliveproxy.com/fastest-proxies/",
                "http://aliveproxy.com/fr-proxy-list/",
                "http://aliveproxy.com/gb-proxy-list/",
                "http://aliveproxy.com/high-anonymity-proxy-list/",
                "http://aliveproxy.com/jp-proxy-list/",
                "http://aliveproxy.com/proxy-list-port-3128/",
                "http://aliveproxy.com/proxy-list-port-80/",
                "http://aliveproxy.com/proxy-list-port-8000/",
                "http://aliveproxy.com/proxy-list-port-8080/",
                "http://aliveproxy.com/proxy-list-port-81/",
                "http://aliveproxy.com/ru-proxy-list/",
                "http://aliveproxy.com/us-proxy-list/"
            };
            Task task = new Task(() =>
            {
                foreach (var url in urls)
                {
                    if (_stop)
                        break;
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
                }
            });
        return task;
        }

        #endregion
    }
}