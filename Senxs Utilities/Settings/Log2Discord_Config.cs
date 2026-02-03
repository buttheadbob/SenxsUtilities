
using System.Collections.ObjectModel;
using S_Utilities.Utils;

namespace S_Utilities.Settings
{
    public partial class S_Config
    {
        private ObservableCollection<LogHandler> _LogHandlers = new ();
        public ObservableCollection<LogHandler> LogHandlers { get => _LogHandlers; set => SetValue(ref _LogHandlers, value); }

        private int _Log2DiscordRateLimitPerSecond = 3;
        public int Log2DiscordRateLimitPerSecond
        {
            get => _Log2DiscordRateLimitPerSecond;
            set => SetValue(ref _Log2DiscordRateLimitPerSecond, value);
        }

        private int _Log2DiscordStackTraceEmbedLimit = 4500;
        public int Log2DiscordStackTraceEmbedLimit
        {
            get => _Log2DiscordStackTraceEmbedLimit;
            set => SetValue(ref _Log2DiscordStackTraceEmbedLimit, value);
        }
    }
}
