using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace S_Utilities.Utils
{
    /// <summary>
    /// Represents a Discord embed for webhook messages.
    /// </summary>
    public class DiscordEmbed
    {
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("color")]
        public int Color { get; set; }

        public DiscordEmbed() { }

        public DiscordEmbed(string description, int color)
        {
            Description = description;
            Color = color;
        }
    }

    /// <summary>
    /// JSON converter that serializes ulong values as strings (required by Discord API).
    /// </summary>
    internal class UlongToStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(ulong) || objectType == typeof(List<ulong>);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(ulong))
                return ulong.Parse(reader.Value?.ToString() ?? "0");
            if (objectType == typeof(List<ulong>))
            {
                var list = new List<ulong>();
                if (reader.TokenType == JsonToken.StartArray)
                {
                    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        list.Add(ulong.Parse(reader.Value?.ToString() ?? "0"));
                }
                return list;
            }
            throw new JsonSerializationException($"Unexpected type {objectType}");
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is ulong ulongValue)
                writer.WriteValue(ulongValue.ToString());
            else if (value is List<ulong> list)
            {
                writer.WriteStartArray();
                foreach (var item in list)
                    writer.WriteValue(item.ToString());
                writer.WriteEndArray();
            }
            else
                throw new JsonSerializationException($"Unexpected value type {value?.GetType()}");
        }
    }

    /// <summary>
    /// Represents allowed mentions for a Discord webhook message.
    /// </summary>
    public class AllowedMentions
    {
        [JsonProperty("parse", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Parse { get; set; } = new();

        [JsonProperty("roles", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(UlongToStringConverter))]
        public List<ulong> RoleIds { get; set; } = new();

        [JsonProperty("users", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(UlongToStringConverter))]
        public List<ulong> UserIds { get; set; } = new();
    }

    /// <summary>
    /// Handles sending messages to Discord webhooks with rate limiting.
    /// </summary>
    public static class DiscordWebHook
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly Logger Log = LogManager.GetLogger("DiscordWebHook");
        
        // Rate limiting: Discord allows 5 requests per second per webhook (bursts).
        // We track requests per second with a counter reset by a timer.
        private static readonly Timer RateLimitResetTimer = new Timer(_ => ResetRateLimiter(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        private static int _requestsThisSecond = 0;
        private static readonly object RateLock = new();

        /// <summary>
        /// Sends a message to a Discord webhook.
        /// </summary>
        /// <param name="webhookUrl">Full Discord webhook URL.</param>
        /// <param name="username">Override the default username of the webhook.</param>
        /// <param name="embeds">Array of embeds to send.</param>
        /// <param name="allowedMentions">Allowed mentions object.</param>
        /// <param name="content">Text content of the message.</param>
        /// <param name="fileAttachment">Stream containing file data (optional).</param>
        /// <param name="fileName">Name of the file (required if fileAttachment provided).</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static async Task<bool> SendAsync(
            string webhookUrl,
            string? username = null,
            IEnumerable<DiscordEmbed>? embeds = null,
            AllowedMentions? allowedMentions = null,
            string? content = null,
            Stream? fileAttachment = null,
            string? fileName = null)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                Log.Warn("Webhook URL is empty.");
                return false;
            }

            // Wait for rate limit
            await WaitForRateLimit();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
                var payload = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(username))
                    payload["username"] = username!;
                if (!string.IsNullOrWhiteSpace(content))
                    payload["content"] = content!;
                if (embeds != null)
                    payload["embeds"] = embeds;
                if (allowedMentions != null)
                    payload["allowed_mentions"] = allowedMentions;

                if (fileAttachment != null && !string.IsNullOrWhiteSpace(fileName))
                {
                    // Use multipart/form-data for file upload
                    var formData = new MultipartFormDataContent();
                    var payloadJson = JsonConvert.SerializeObject(payload);
                    formData.Add(new StringContent(payloadJson, Encoding.UTF8, "application/json"), "payload_json");

                    fileAttachment.Position = 0;
                    var fileContent = new StreamContent(fileAttachment);
                    formData.Add(fileContent, "file", fileName);
                    request.Content = formData;
                }
                else
                {
                    var json = JsonConvert.SerializeObject(payload);
                    Log.Debug($"Sending webhook JSON: {json}");
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var response = await HttpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Log.Debug($"Successfully sent message to {webhookUrl}");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Warn($"Failed to send webhook: {response.StatusCode} - {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception while sending webhook to {webhookUrl}");
                return false;
            }
        }

        private static async Task WaitForRateLimit()
        {
            while (true)
            {
                lock (RateLock)
                {
                    if (_requestsThisSecond < 5)
                    {
                        _requestsThisSecond++;
                        return;
                    }
                }
                await Task.Delay(200); // wait a bit before checking again
            }
        }

        private static void ResetRateLimiter()
        {
            lock (RateLock)
            {
                _requestsThisSecond = 0;
            }
        }
    }
}
