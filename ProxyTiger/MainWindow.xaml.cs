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
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Serialization;
using ActiproSoftware.Windows.Controls.Ribbon.Controls;
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

        private readonly List<Task> _tasks = new List<Task>();
        private bool _stop = false;
        private string[] _sources = {};

        private enum Job
        {
            Checking,
            Scraping,
            Idle
        }
        private Job _runningJob = Job.Idle;

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
            if (_runningJob == Job.Idle)
            {
                LockControls();
                LblStatus.Text = "Scanning";
                BtnCheck.IsEnabled = false;
                BtnScrape.IsEnabled = false;
                BtnStop.IsEnabled = false;
                _runningJob = Job.Checking;
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
                                if (status != Proxy.ProxyType.Unknown)
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
                                //working++;
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
                var t = new Thread(() =>
                {
                    while (working + notWorking != LvProxies.Items.Count) ;
                    Application.Current.Dispatcher.Invoke((Action) (() =>
                    {
                        new MsgBox("ProxyTiger", "Scanning has finished").ShowDialog();
                        LblStatus.Text = "Idle";
                        UnlockControls();
                    }));
                    _runningJob = Job.Idle;
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
            else
            {
                new MsgBox("ProxyTiger", "ProxyTiger is still scraping").ShowDialog();
            }
        }

        private Proxy.ProxyType IsProxyUp(WebProxy proxy)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create("http://samair.ru/anonymity-test/");
            try
            {
                httpWebRequest.Timeout = 2000;
                httpWebRequest.Proxy = proxy;
                HttpWebResponse httpWebResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                string text2 = streamReader.ReadToEnd();
                if (!text2.Contains("Your IP is detected"))
                {
                    if (text2.Contains(">high-anonymous (elite) proxy</"))
                    {
                        return Proxy.ProxyType.HighAnonymous; //possibly change names to what people recongnize elite,anon,transparent
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
            LockControls(true);
            LblStatus.Text = "Scraping";
            _runningJob = Job.Scraping;
            _tasks.Add(HideMyName()); //216 out of 785
            _tasks.Add(SamairRu()); //144 out of 600
            //_tasks.Add(ProxyDb()); //90 out of 950
            //_tasks.Add(ProxySpy()); //67 out of 300
            //_tasks.Add(ProxyListOrg()); //45 out of 140
            _tasks.Add(MorphIo()); //204 out of 2046
            //_tasks.Add(IpAddress()); //11 out of 50
            //_tasks.Add(MeilleurVpn()); //40 out of 180
            //_tasks.Add(HideMyIp()); //80 out of 445
            //_tasks.Add(SslProxies()); //52 out of 100
            //_tasks.Add(ProxyApe()); //213 out of 3100
            //_tasks.Add(OrcaTech()); // 1200 out of 3000
            _tasks.Add(SslProxies24()); // we need to only scrape from the day of scrapings posts not all time
            //_tasks.Add(AliveProxy()); //23 out of 223
            _tasks.Add(UserProxy());
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
                    _runningJob = Job.Idle;
                    new MsgBox("ProxyTiger", "Scraped " + LvProxies.Items.Count + " proxies.").ShowDialog();
                    UnlockControls(true);
                }));
            }).Start();
        }

        private void BtnExportProxies_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
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

        private void BtnPasteProxies_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            var proxies =
                Clipboard.GetText()
                    .Split('\n')
                    .Select(proxy => proxy.Split(':'))
                    .Select(p => new Proxy(p[0], p[1]))
                    .ToList();
            foreach (var proxy in proxies)
            {
                LvProxies.Items.Add(proxy);
            }
        }

        private void BtnImportProxies_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
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

        private void BtnLoadSources_Click(object sender,
    ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog().HasValue.Equals(true))
            {
                _sources = File.ReadAllLines(ofd.FileName);
                new MsgBox("ProxyTiger", "Imported " + _sources.Length + " sources.").ShowDialog();

            }
        }

        private void BtnRemoveDuplicates_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            var proxies = LvProxies.Items.OfType<Proxy>().ToList().GroupBy(x => x.IP + ":" + x.Port).Select(x => x.First()).ToList();
            new MsgBox("ProxyTiger", "You removed " + (LvProxies.Items.Count - proxies.Count) + " duplicates.").ShowDialog();
            LblProxyStatus.Text = $"Proxies: {proxies.Count}"; ;
            LvProxies.Items.Clear();
            foreach (Proxy proxy in proxies)
            {
                LvProxies.Items.Add(proxy);
            }
        }

        private void BtnRemoveNotWorking_Click(object sender,
            ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            if (_runningJob == Job.Idle)
            {
                var proxies =
                    LvProxies.Items.OfType<Proxy>().Where(proxy => proxy.Status != Proxy.StatusType.NotWorking).ToList();
                new MsgBox("ProxyTiger",
                    "You removed " + (LvProxies.Items.Count - proxies.Count) + " not working proxies.").ShowDialog();
                LvProxies.Items.Clear();
                foreach (Proxy proxy in proxies)
                {
                    LvProxies.Items.Add(proxy);
                }
                LblAdditionalInfo.Text = $"Working: {proxies.Count}, Not Working: 0";
            }
            else
            {
                new MsgBox("ProxyTiger", "ProxyTiger is still running!").ShowDialog();
            }
        }

        private void LockControls(bool isScrape = false)
        {
            BtnCheck.IsEnabled = false;
            BtnScrape.IsEnabled = false;
            if(!isScrape)
                BtnStop.IsEnabled = false;
            BtnCopyProxies.IsEnabled = false;
            BtnPasteProxies.IsEnabled = false;
            BtnExportProxies.IsEnabled = false;
            BtnImportProxies.IsEnabled = false;
            BtnRemoveDuplicates.IsEnabled = false;
            BtnRemoveNotWorking.IsEnabled = false;
            BtnLoadSources.IsEnabled = false;
        }

        private void UnlockControls(bool isScrape = false)
        {
            BtnCheck.IsEnabled = true;
            BtnScrape.IsEnabled = true;
            if(!isScrape)
                BtnStop.IsEnabled = true;
            BtnCopyProxies.IsEnabled = true;
            BtnPasteProxies.IsEnabled = true;
            BtnExportProxies.IsEnabled = true;
            BtnImportProxies.IsEnabled = true;
            BtnRemoveDuplicates.IsEnabled = true;
            BtnRemoveNotWorking.IsEnabled = true;
            BtnLoadSources.IsEnabled = true;
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
                var matches = Regex.Matches(source, @"<content[^C]*");
                string content = "";
                for (int i = 0; i < 3; i++)
                {
                    content += matches[i].Value;
                }
                foreach (
                    Match match in
                    Regex.Matches(content, "([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}):([0-9]{1,5})"))
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

        private Task UserProxy()
        {
            Task task = new Task(() =>
            {
                foreach (var url in _sources)
                {

                    if (_stop)
                        break;
                    try
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
                    }
                    catch
                        (System.Net.WebException)
                    {
                        continue;
                    }

                }
            });
            return task;
        }

        #endregion
    }
}
