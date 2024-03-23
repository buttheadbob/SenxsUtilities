using System.Text;
using Discord;
using Discord.Webhook;
using NLog;
using S_Utilities.Settings;

namespace S_Utilities.Utils
{
    public static class Log2Discord_Processor
    {
        private static S_Config? Config => Senxs_Utilities.Config;
        
        public static async void ProcessLogEvent(LogEventInfo logEvent)
        {
            if (Config is null) return;
            if (!Config.MasterSwitch) return;
            if (!Config.Log2Discord) return;
            
            foreach (LogHandler logHandler in Config.LogHandlers)
            {
                if (logHandler.OnlyWhenServerOnline && !Senxs_Utilities.IsOnline)
                    continue;
                
                if (logHandler.LogNames.Count > 0 && !logHandler.LogNames.Contains(logEvent.LoggerName))
                    continue;
                
                if (logHandler.LogNames_Ignore.Count > 0 && logHandler.LogNames_Ignore.Contains(logEvent.LoggerName))
                    continue;
                
                if (logEvent.Level.Ordinal < logHandler.MinLogLevel || logEvent.Level.Ordinal > logHandler.MaxLogLevel)
                    continue;

                using DiscordWebhookClient client = new (logHandler.DiscordWebHook);
                
                StringBuilder sb = new ();
                sb.AppendLine($"**{logEvent.Level.Name}**");
                sb.AppendLine();
                sb.AppendLine($"**Logger:** {logEvent.LoggerName}");
                sb.AppendLine($"**Message:** {logEvent.Message}");
                
                if (!string.IsNullOrWhiteSpace(logEvent.Exception?.Message))
                    sb.AppendLine($"**Exception:** {logEvent.Exception?.Message}");
                
                if (logEvent.HasStackTrace)
                    sb.AppendLine($"**Stack Trace:** {logEvent.Exception?.StackTrace}");

                sb.AppendLine(logHandler.CreateRolePingText());
                sb.AppendLine(logHandler.CreateMemberPingText());

                EmbedBuilder embed = new ()
                {
                    Description = sb.ToString(), Color = logEvent.Level.Name switch
                    {
                        "Fatal" => Color.DarkRed,
                        "Error" => Color.Red,
                        "Warn" => Color.Gold,
                        "Info" => Color.Green,
                        "Debug" => Color.LightGrey,
                        _ => Color.Default
                    }
                };

                AllowedMentions allowedMentions = new ()
                {
                    RoleIds = logHandler.RolesToPing,
                    UserIds = logHandler.MembersToPing
                };
                
                await client.SendMessageAsync(text: Senxs_Utilities.InstName, embeds: new[] { embed.Build() }, allowedMentions: allowedMentions);
            }
        }
    }
}