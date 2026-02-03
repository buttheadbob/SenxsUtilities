using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using S_Utilities.Settings;
using VRage;

namespace S_Utilities.Utils
{
    public static class Log2DiscordProcessor
    {
        private const int DefaultMaxMessagesPerSecond = 3;
        private const int DefaultStackTraceEmbedLimit = 4500;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromSeconds(1);

        private static S_Config? Config => Senxs_Utilities.Config;
        private static int MaxMessagesPerSecond
        {
            get
            {
                int configured = Config?.Log2DiscordRateLimitPerSecond ?? DefaultMaxMessagesPerSecond;
                return configured > 0 ? configured : DefaultMaxMessagesPerSecond;
            }
        }

        private static int StackTraceEmbedLimit
        {
            get
            {
                int configured = Config?.Log2DiscordStackTraceEmbedLimit ?? DefaultStackTraceEmbedLimit;
                return configured > 0 ? configured : DefaultStackTraceEmbedLimit;
            }
        }
        private static readonly ConcurrentQueue<(WebHookMessage, StringBuilder)> MessagesForDiscord = new();
        private static readonly Timer SendTimer = new(TimeSpan.FromSeconds(1).TotalMilliseconds);
        private static readonly FastResourceLock Lock = new();
        private static readonly object RateLimitLock = new();
        private static readonly Dictionary<string, RateLimitState> RateLimits = new(StringComparer.OrdinalIgnoreCase);

        static Log2DiscordProcessor()
        {
            SendTimer.Elapsed += SendMessages;
            SendTimer.Start();
        }

        private static async void SendMessages(object sender, ElapsedEventArgs e)
        {
            using (Lock.AcquireExclusiveUsing())
            {
                FlushRateLimitNotices();
                while (!MessagesForDiscord.IsEmpty)
                {
                    if (!MessagesForDiscord.TryDequeue(out (WebHookMessage, StringBuilder) msg))
                        break;

                    await SendSingleMessageAsync(msg.Item1, msg.Item2);
                }
            }
        }

        private static async Task SendSingleMessageAsync(WebHookMessage webhookMessage, StringBuilder fileBuilder)
        {
            try
            {
                string webhookUrl = webhookMessage.DiscordWebHookUrl;
                if (string.IsNullOrEmpty(webhookUrl))
                {
                    Senxs_Utilities.Log.Warn("No Discord webhook URL configured for this log handler.");
                    return;
                }

                var allowedMentions = new AllowedMentions
                {
                    RoleIds = webhookMessage.RoleIDs,
                    UserIds = webhookMessage.UserIDs
                };

                Stream? fileStream = null;
                string? fileName = null;

                // If fileBuilder has characters, create a file attachment
                if (fileBuilder.Length > 0)
                {
                    var content = fileBuilder.ToString();
                    var bytes = Encoding.UTF8.GetBytes(content);
                    fileStream = new MemoryStream(bytes);
                    fileName = "stacktrace.txt";
                }

                // Ensure embed is not null before sending
                if (webhookMessage.Embed == null)
                {
                    Senxs_Utilities.Log.Warn("Webhook message embed is null, skipping.");
                    return;
                }

                bool success = await DiscordWebHook.SendAsync(
                    webhookUrl,
                    username: Senxs_Utilities.InstName,
                    embeds: new[] { webhookMessage.Embed },
                    allowedMentions: allowedMentions,
                    fileAttachment: fileStream,
                    fileName: fileName
                );

                if (!success)
                {
                    Senxs_Utilities.Log.Warn("Failed to send log to Discord.");
                }
            }
            catch (Exception ex)
            {
                Senxs_Utilities.Log.Error(ex, "Error sending Discord webhook.");
            }
        }

        private static int GetColorForLogLevel(string levelName)
        {
            return levelName switch
            {
                "Fatal" => 0x8B0000, // DarkRed
                "Error" => 0xFF0000, // Red
                "Warn"  => 0xFFD700, // Gold
                "Info"  => 0x00FF00, // Green
                "Debug" => 0xD3D3D3, // LightGrey
                _       => 0x000000  // Default black
            };
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

                if (!IsTestLog(logEvent))
                {
                    bool allowMessage = TryConsumeRateLimitSlot(logHandler, out int droppedCount);
                    if (droppedCount > 0)
                        EnqueueRateLimitNotice(logHandler, droppedCount);
                    if (!allowMessage)
                        return;
                }

                string? rawStackTrace = logEvent.Exception?.StackTrace;
                string stackTraceEmbedText;
                StringBuilder stackTraceAttachment = new();
                if (string.IsNullOrWhiteSpace(rawStackTrace))
                {
                    stackTraceEmbedText = "```No Stack Trace Provided```";
                }
                else if (rawStackTrace!.Length <= StackTraceEmbedLimit)
                {
                    stackTraceEmbedText = $"```{rawStackTrace}```";
                }
                else
                {
                    stackTraceEmbedText = "```Stack trace too long. See attached file.```";
                    stackTraceAttachment.AppendLine(rawStackTrace);
                }

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
                    sb.AppendLine(stackTraceEmbedText);
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
                
                DiscordEmbed embed = new DiscordEmbed
                {
                    Description = sb.ToString(),
                    Color = GetColorForLogLevel(logEvent.Level.Name)
                };

                WebHookMessage webhookMessage = new()
                {
                    RoleIDs = logHandler.RolesToPing,
                    UserIDs = logHandler.MembersToPing,
                    Embed = embed,
                    DiscordWebHookUrl = logHandler.DiscordWebHook
                };

                MessagesForDiscord.Enqueue((webhookMessage, stackTraceAttachment));
                return;
            }
        }

        private static bool TryConsumeRateLimitSlot(LogHandler logHandler, out int droppedCount)
        {
            droppedCount = 0;
            if (string.IsNullOrWhiteSpace(logHandler.DiscordWebHook))
                return false;

            DateTime now = DateTime.UtcNow;
            lock (RateLimitLock)
            {
                if (!RateLimits.TryGetValue(logHandler.DiscordWebHook, out RateLimitState? state))
                {
                    state = new(now);
                    RateLimits.Add(logHandler.DiscordWebHook, state);
                }

                state.LastHandlerName = string.IsNullOrWhiteSpace(logHandler.Name) ? null : logHandler.Name;

                if (now - state.WindowStartUtc >= RateLimitWindow)
                {
                    if (state.DroppedInWindow > 0)
                    {
                        droppedCount = state.DroppedInWindow;
                        state.DroppedInWindow = 0;
                        state.SentInWindow = 1;
                    }
                    else
                    {
                        state.SentInWindow = 0;
                    }

                    state.WindowStartUtc = now;
                }

                if (state.SentInWindow >= MaxMessagesPerSecond)
                {
                    state.DroppedInWindow++;
                    return false;
                }

                state.SentInWindow++;
                return true;
            }
        }

        private static bool IsTestLog(LogEventInfo logEvent)
        {
            return string.Equals(logEvent.LoggerName, "Test Logger", StringComparison.OrdinalIgnoreCase);
        }

        private static void FlushRateLimitNotices()
        {
            DateTime now = DateTime.UtcNow;
            List<(string WebhookUrl, string? HandlerName, int DroppedCount)> notices = new();

            lock (RateLimitLock)
            {
                foreach (KeyValuePair<string, RateLimitState> entry in RateLimits)
                {
                    RateLimitState state = entry.Value;
                    if (now - state.WindowStartUtc < RateLimitWindow)
                        continue;

                    if (state.DroppedInWindow > 0)
                    {
                        notices.Add((entry.Key, state.LastHandlerName, state.DroppedInWindow));
                        state.DroppedInWindow = 0;
                        state.SentInWindow = 1;
                    }
                    else
                    {
                        state.SentInWindow = 0;
                    }

                    state.WindowStartUtc = now;
                }
            }

            foreach ((string WebhookUrl, string? HandlerName, int DroppedCount) notice in notices)
            {
                MessagesForDiscord.Enqueue((BuildRateLimitNotice(notice.WebhookUrl, notice.HandlerName, notice.DroppedCount), new StringBuilder()));
            }
        }

        private static void EnqueueRateLimitNotice(LogHandler logHandler, int droppedCount)
        {
            MessagesForDiscord.Enqueue((BuildRateLimitNotice(logHandler.DiscordWebHook, logHandler.Name, droppedCount), new StringBuilder()));
        }

        private static WebHookMessage BuildRateLimitNotice(string webhookUrl, string? handlerName, int droppedCount)
        {
            string handlerLabel = string.IsNullOrWhiteSpace(handlerName) ? "handler" : $"handler \"{handlerName}\"";
            string message = $"Rate limit reached for {handlerLabel}. Dropped {droppedCount} log message{(droppedCount == 1 ? "" : "s")} in the last second.";

            DiscordEmbed embed = new DiscordEmbed
            {
                Description = message,
                Color = 0xFFA500 // Orange
            };

            return new WebHookMessage
            {
                DiscordWebHookUrl = webhookUrl,
                Embed = embed
            };
        }
    }
    
    public sealed class WebHookMessage
    {
        public DiscordEmbed? Embed;
        public List<ulong> RoleIDs = new ();
        public List<ulong> UserIDs = new ();
        public string DiscordWebHookUrl = "";
    }

    internal sealed class RateLimitState
    {
        public DateTime WindowStartUtc;
        public int SentInWindow;
        public int DroppedInWindow;
        public string? LastHandlerName;

        public RateLimitState(DateTime windowStartUtc)
        {
            WindowStartUtc = windowStartUtc;
        }
    }
}
