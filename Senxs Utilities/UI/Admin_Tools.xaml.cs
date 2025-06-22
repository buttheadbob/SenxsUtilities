using System;
using System.Windows;
using System.Windows.Controls;
using S_Utilities.Settings;

namespace Senxs_Utilities.Senxs_Utilities.UI
{
    public partial class Admin_ToolsSettings : UserControl
    {
        private readonly S_Config? Config;
        private readonly Action SaveConfig;
        
        public Admin_ToolsSettings(S_Config? config, Action saveMethod)
        {
            InitializeComponent();
            Config = config;
            DataContext = Config;
            SaveConfig = saveMethod;
        }
        
        private void SaveSettingsButtonClick(object sender, RoutedEventArgs e) => SaveConfig();
    }
}