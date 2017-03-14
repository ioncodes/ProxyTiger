using System;
using System.Net;
using System.Text.RegularExpressions;
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
        private readonly string[] _sources = { "https://hidemy.name/en/proxy-list/?start=" };
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnScrape_Click(object sender, ActiproSoftware.Windows.Controls.Ribbon.Controls.ExecuteRoutedEventArgs e)
        {
            LblStatus.Text = "Scraping";
            foreach (var url in _sources)
            {
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
        }
    }
}
