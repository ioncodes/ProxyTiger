using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ActiproSoftware.Windows.Themes;

namespace ProxyTiger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeManager.BeginUpdate();
            try
            {
                ThemeManager.AreNativeThemesEnabled = true;
                ThemeManager.CurrentTheme = ThemeName.MetroDark.ToString();
            }
            finally
            {
                ThemeManager.EndUpdate();
            }
            base.OnStartup(e);
        }

    }
}
