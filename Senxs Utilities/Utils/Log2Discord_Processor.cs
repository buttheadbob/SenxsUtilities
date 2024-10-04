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
using VRage.Collections;

namespace S_Utilities.Utils
{
    public static class Log2Discord_Processor
    {
        private static S_Config? Config => Senxs_Utilities.Config;
        private static readonly Dictionary<string,List<WebHookMessage>> MessagesForDiscord = new();
        private static readonly Timer SendTimer = new(TimeSpan.FromSeconds(5).TotalMilliseconds);
        private static readonly FastResourceLock _lock = new();

        static Log2Discord_Processor()
        {
            SendTimer.Elapsed += SendMessages;
            SendTimer.Start();
        }

        private static async void SendMessages(object sender, ElapsedEventArgs e)
        {
            using (_lock.AcquireExclusiveUsing())
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
                    HashSet<ulong> allowedRoleID = new();
                    HashSet<ulong> allowedMemberIds = new();
                
                    foreach (WebHookMessage hookMessage in messages)
                    {
                        embeds.AddRange(hookMessage.Embeds);

                        foreach (ulong roleID in hookMessage.RoleIDs)
                            allowedRoleID.Add(roleID);

                        foreach (ulong userID in hookMessage.UserIDs)
                            allowedMemberIds.Add(userID);
                    }
                
                    AllowedMentions mentions = new()
                    {
                        RoleIds = allowedRoleID.ToList(), 
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

                using (_lock.AcquireSharedUsing())
                {
                    MessagesForDiscord[logHandler.DiscordWebHook] = new List<WebHookMessage>{webhookMessage};
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

