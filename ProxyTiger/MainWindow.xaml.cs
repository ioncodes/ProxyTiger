using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ActiproSoftware.Windows.Themes;

namespace ProxyTiger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnScrape_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            LblStatus.Text = "Scraping";
            HideMyName();
            SamairRu();
            GetProxyJp();
            ProxyDb();
            ProxySpy();
            FreeProxyListsNet();
            ProxyListOrg();
            MultiProxy();
        }

        private void HideMyName()
        {
            string url = "https://hidemy.name/en/proxy-list/?start=";
            new Task(() =>
            {
                for (int i = 0; i < 30; i++)
                {
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i * 64);
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3})</td><td>([0-9]{1,4})"))
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            }).Start();
        }

        private void SamairRu()
        {
            string url = "http://samair.ru/proxy/list-IP-port/proxy-{0}.htm";
            new Task(() =>
            {
                for (int i = 1; i <= 20; i++)
                {
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url.Replace("{0}", i.ToString()));
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            }).Start();
        }

        private void GetProxyJp()
        {
            string url = "http://www.getproxy.jp/en/default/";
            new Task(() =>
            {
                for (int i = 1; i <= 5; i++)
                {
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url+i);
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            }).Start();
        }

        private void ProxyDb()
        {
            string url = "http://proxydb.net/?offset=";
            new Task(() =>
            {
                for (int i = 0; i <= 50; i++)
                {
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i * 50);
                    if (source.Contains("You are sending requests too fast, please wait a moment "))
                    {
                        var timeoutString = source.Replace("You are sending requests too fast, please wait a moment (", "");
                        timeoutString = timeoutString.Replace("s) and try again.", "");
                        var timeout = Convert.ToInt32(timeoutString);
                        Application.Current.Dispatcher.Invoke((Action) (() =>
                        {
                            LblAdditionalInfo.Text = "Waiting for a provider to unblock us...";
                        }));
                        Thread.Sleep(timeout*1024);
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            LblAdditionalInfo.Text = "";
                        }));
                    }
                    foreach (
                        Match match in
                        Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                    Thread.Sleep(1000);
                }
            }).Start();
        }

        private void ProxySpy()
        {
            string url = "http://txt.proxyspy.net/proxy.txt";
            new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            }).Start();
        }

        private void FreeProxyListsNet()
        {
            string url = "http://www.freeproxylists.net/?page="; // + 10 i think
            // uses IPDecode() => IPDecode("%3c%61%20%68%72%65%66%3d%22%68%74%74%70%3a%2f%2f%77%77%77%2e%66%72%65%65%70%72%6f%78%79%6c%69%73%74%73%2e%6e%65%74%2f%32%31%38%2e%39%30%2e%31%37%34%2e%31%36%37%2e%68%74%6d%6c%22%3e%32%31%38%2e%39%30%2e%31%37%34%2e%31%36%37%3c%2f%61%3e")

        }

        private void ProxyListOrg()
        {
            string url = "https://proxy-list.org/german/search.php?search=&country=any&type=any&port=any&ssl=any&p=";
            new Task(() =>
            {
                for (int i = 1; i <= 10; i++)
                {
                    var wc = new WebClient();
                    wc.Headers.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                    var source = wc.DownloadString(url + i);
                    foreach (
                        Match match in
                        Regex.Matches(source, "Proxy\\(\'([a-zA-Z0-9=]+)\'\\)"))
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            string[] proxy = Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups[1].Value)).Split(':');
                            LvProxies.Items.Add(new Proxy(proxy[0], proxy[1]));
                            LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                        }));
                    }
                }
            }).Start();
        }

        private void MultiProxy()
        {
            string url = "http://multiproxy.org/txt_all/proxy.txt";
            new Task(() =>
            {
                var wc = new WebClient();
                wc.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
                var source = wc.DownloadString(url);
                foreach (
                    Match match in
                    Regex.Matches(source, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\:?([0-9]{1,5})?"))
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        LvProxies.Items.Add(new Proxy(match.Groups[1].Value, match.Groups[2].Value));
                        LblProxyStatus.Text = $"Proxies: {LvProxies.Items.Count}";
                    }));
                }
            }).Start();
        }
    }
}
