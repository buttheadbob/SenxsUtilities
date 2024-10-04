using System;
using System.Windows;
using System.Windows.Controls;
using S_Utilities.Settings;
using S_Utilities.Utils;

namespace Senxs_Utilities.Senxs_Utilities.UI
{
    public partial class LocalCommands_Settings : UserControl
    {
        private readonly S_Config? Config;
        private readonly Action SaveConfig;
        
        public LocalCommands_Settings(S_Config? config, Action saveMethod)
        {
            InitializeComponent();
            Config = config;
            DataContext = Config;
            SaveConfig = saveMethod;
            NameText.Text =NamedPipeServer.GetPipeName() ;
            init();
        }

        private void SaveSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        private void init()
        {
            NamedPipeServer.GUI_StatusUpdater += UpdatePipeStatus;
            if (Config is null) return;
            if (!Config.MasterSwitch) return;
            if (!Config.LocalCommandServer) return;
            
            if (Config.StartPipeOnPluginLoad && Config.MasterSwitch && Config.LocalCommandServer)
            {
                NamedPipeServer.StartServer();
            }
            
        }

        private void UpdatePipeStatus(byte code)
        {
            Dispatcher.Invoke(() =>
            {
                switch (code)
                {
                    case 0:
                        StatusText.Text = "Offline";
                        StatusText.Foreground = System.Windows.Media.Brushes.Black;
                        break;
                    case 1:
                        StatusText.Text = "Online";
                        StatusText.Foreground = System.Windows.Media.Brushes.Green;
                        break;
                    case 3:
                        StatusText.Text = "Stopping";
                        StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                        break;
                    case 4:
                        StatusText.Text = "ERROR";
                        StatusText.Foreground = System.Windows.Media.Brushes.DarkRed;
                        break;
                }
            });
        }
    }
}