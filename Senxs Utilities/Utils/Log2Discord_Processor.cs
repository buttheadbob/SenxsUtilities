using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Discord;
using Discord.Webhook;
using NLog;
using S_Utilities.Settings;
using VRage;

namespace S_Utilities.Utils
{
    public static class Log2DiscordProcessor
    {
        private static S_Config? Config => Senxs_Utilities.Config;
        private static readonly Dictionary<string,List<WebHookMessage>> MessagesForDiscord = new();
        private static readonly Timer SendTimer = new(TimeSpan.FromSeconds(5).TotalMilliseconds);
        private static readonly FastResourceLock Lock = new();

        static Log2DiscordProcessor()
        {
            SendTimer.Elapsed += SendMessages;
            SendTimer.Start();
        }

        private static async void SendMessages(object sender, ElapsedEventArgs e)
        {
            using (Lock.AcquireExclusiveUsing())
            {
                List<string> keys = MessagesForDiscord.Keys.ToList();
                if (!keys.Any()) return;
            
                foreach (string message in keys)
                {
                    using DiscordWebhookClient client = new (message);
                    if (!MessagesForDiscord.TryGetValue(message, out List<WebHookMessage> messages)) continue;
                
                    if (messages.Count > 10)
                    {
                        EmbedBuilder embed = new ()
                        {
                            Description = "WARNING:  GENERATING TOO MANY LOGS TO POST!!!", Color = Color.DarkRed,
                        };

                        try
                        {
                            await client.SendMessageAsync(Senxs_Utilities.InstName, embeds: new[] { embed.Build() });
                        }
                        catch (Exception exception)
                        {
                            Senxs_Utilities.Log.Error(exception, exception.Message);
                        }
                        MessagesForDiscord.Remove(message);
                        continue;
                    }

                    List<Embed> embeds = new();
                    HashSet<ulong> allowedRoleId = new();
                    HashSet<ulong> allowedMemberIds = new();
                
                    foreach (WebHookMessage hookMessage in messages)
                    {
                        embeds.AddRange(hookMessage.Embeds);

                        foreach (ulong roleId in hookMessage.RoleIDs)
                            allowedRoleId.Add(roleId);

                        foreach (ulong userId in hookMessage.UserIDs)
                            allowedMemberIds.Add(userId);
                    }
                
                    AllowedMentions mentions = new()
                    {
                        RoleIds = allowedRoleId.ToList(), 
                        UserIds = allowedMemberIds.ToList()
                    };

                    try
                    {
                        await client.SendMessageAsync(Senxs_Utilities.InstName, embeds: embeds, allowedMentions: mentions);
                    }
                    catch (Exception exception)
                    {
                        Senxs_Utilities.Log.Error(exception, exception.Message);
                    }
                    
                    MessagesForDiscord.Remove(message);
                }
            }
        }

        public static void ProcessLogEvent(LogEventInfo logEvent)
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
                
                StringBuilder sb = new ();
                sb.AppendLine($"**{logEvent.Level.Name}**");
                sb.AppendLine();
                sb.AppendLine("**Logger**");
                sb.AppendLine($"```{logEvent.LoggerName}```");
                sb.AppendLine();
                sb.AppendLine("**Message:**");
                sb.AppendLine(string.IsNullOrWhiteSpace(logEvent.Message)
                    ? "```No Message Provided```"
                    : logEvent.Message.Equals("{0}")
                        ? "```Invalid Parameters Provided => {0}```"
                        : $"```{logEvent.Message}```");
                if (logEvent.Exception is not null)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Exception Message**");
                    sb.AppendLine(string.IsNullOrWhiteSpace(logEvent.Exception.Message)
                        ? "```No Exception Message Provided```"
                        : $"```{logEvent.Exception.Message}```");
                    sb.AppendLine();
                    sb.AppendLine("**Exception Type**");
                    sb.AppendLine($"```{logEvent.Exception.GetType()}```");
                    sb.AppendLine();
                    sb.AppendLine("**Exception Stack Trace**");
                    sb.AppendLine(logEvent.Exception.StackTrace is null
                        ? "```No Stack Trace Provided```"
                        : $"```{logEvent.Exception.StackTrace}```");
                    sb.AppendLine();
                    sb.AppendLine("**Exception Inner Exception**");
                    sb.AppendLine(logEvent.Exception.InnerException is null
                        ? "```No Inner Exception Provided```"
                        : $"```{logEvent.Exception.InnerException}```");
                    sb.AppendLine();
                    sb.AppendLine("**Exception Target Site**");
                    sb.AppendLine($"```{logEvent.Exception.TargetSite}```");
                }

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

                WebHookMessage webhookMessage = new()
                {
                    RoleIDs = logHandler.RolesToPing,
                    UserIDs = logHandler.MembersToPing
                };
                
                webhookMessage.Embeds.Add(embed.Build());

                if (MessagesForDiscord.TryGetValue(logHandler.DiscordWebHook, out List<WebHookMessage>? list))
                {
                    list.Add(webhookMessage);
                    return;
                }

                using (Lock.AcquireSharedUsing())
                {
                    MessagesForDiscord[logHandler.DiscordWebHook] = new(){webhookMessage};
                }
            }
        }
    }
    
    public sealed class WebHookMessage
    {
        public List<Embed> Embeds = new ();
        public List<ulong> RoleIDs = new ();
        public List<ulong> UserIDs = new ();
    }
}

