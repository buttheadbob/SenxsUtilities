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
            
            foreach (LogHandler logHandler in Config.LogHandlers)
            {
                if (logHandler.LogNames.Count > 0 && !logHandler.LogNames.Contains(logEvent.LoggerName))
                    continue;
                
                if (logHandler.LogNames_Ignore.Count > 0 && logHandler.LogNames_Ignore.Contains(logEvent.LoggerName))
                    continue;
                
                if (logEvent.Level.Ordinal < logHandler.MinLogLevel || logEvent.Level.Ordinal > logHandler.MaxLogLevel)
                    continue;

                using DiscordWebhookClient client = new DiscordWebhookClient(logHandler.DiscordWebHook);
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"**{logEvent.Level.Name}**");
                sb.AppendLine($"**Logger:** {logEvent.LoggerName}");
                sb.AppendLine($"**Message:** {logEvent.Message}");
                sb.AppendLine($"**Exception:** {logEvent.Exception?.Message}");
                
                if (logEvent.HasStackTrace)
                    sb.AppendLine($"**Stack Trace:** {logEvent.Exception?.StackTrace}");

                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = "Log Event",
                    Description = sb.ToString()
                };

                AllowedMentions allowedMentions = new AllowedMentions
                {
                    RoleIds = logHandler.RolesToPing
                };
                await client.SendMessageAsync(text:"NEW TORCH LOG EVENT", embeds: new[] { embed.Build() }, allowedMentions: allowedMentions);
            }
        }
    }
}