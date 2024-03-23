using NLog;
using System;
using System.IO;
using System.Windows.Controls;
using NLog.Config;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using S_Utilities.UI;
using S_Utilities.Settings;
using S_Utilities.Utils;

// ReSharper disable ClassNeverInstantiated.Global

namespace S_Utilities
{
    public class Senxs_Utilities : TorchPluginBase, IWpfPlugin
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string CONFIG_FILE_NAME = "Senxs_UtilitiesConfig.cfg";

        private SenX_UI? _control;
        public UserControl GetControl() => _control ??= new SenX_UI(this);

        private static Persistent<S_Config>? _config;
        public static S_Config? Config => _config?.Data;

        public static bool IsOnline = false;
        public static string InstName = "";

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            InstName = Torch.Config.InstanceName;

            SetupConfig();

            TorchSessionManager? sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            // Torch's new event handler logger things is still WIP so lets not get to cozy with it yet...
            LoggingConfiguration config = LogManager.Configuration ?? new LoggingConfiguration();
            SenXCustomTarget customTarget = new ();
            config.AddTarget("SenXCustomTarget", customTarget);
            LoggingRule rule = new ("*", LogLevel.Trace, customTarget);
            config.LoggingRules.Add(rule);
            LogManager.Configuration = config;
            
            SenXCustomTarget.CustomLogEventReceived += Log2Discord_Processor.ProcessLogEvent;
            Save();
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    IsOnline = true;
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    IsOnline = false;
                    break;
            }
        }

        private void SetupConfig()
        {
            string configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

            try
            {
                _config = Persistent<S_Config>.Load(configFile);
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data != null) return;
            Log.Info("Create Default Config, because none was found!");

            _config = new Persistent<S_Config>(configFile, new S_Config());
            _config.Save();
        }

        public void Save()
        {
            try
            {
                _config?.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }
    }
}
