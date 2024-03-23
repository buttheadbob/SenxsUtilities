using System.Windows.Controls;
using S_Utilities.Settings;
using Senxs_Utilities.UI;

namespace S_Utilities.UI
{
    public partial class SenX_UI : UserControl
    {
        private readonly Senxs_Utilities Plugin;
        private static S_Config? Config => Senxs_Utilities.Config;

        public SenX_UI(Senxs_Utilities plugin)
        {
            InitializeComponent();
            Plugin = plugin;
            Log2Discord_Settings discordSettings = new Log2Discord_Settings(Config, SaveRequested);
            // EjectorSettings ejectorSettings = new EjectorSettings(Plugin.Config, SaveRequested);  --- NOT IMPLEMENTED YET
            DataContext = Config;
            DiscordSettingsTab.Content = discordSettings;
            // EjectorSettingsTab.Content = ejectorSettings;
        }

        private void SaveRequested()
        {
            Plugin.Save();
        }
    }
}
