using System.Collections.Generic;
using System.Text;

namespace S_Utilities.Utils
{
    public class LogHandler
    {
        public int MinLogLevel { get; set; } = 0;
        public int MaxLogLevel { get; set; } = 5;
        public string DiscordWebHook { get; set; } = "";
        public List<string> LogNames { get; set; } = new List<string>();
        public List<string> LogNames_Ignore { get; set; } = new List<string>();
        public string Name { get; set; } = "";
        public List<ulong> RolesToPing { get; set; } = new List<ulong>();

        public string CreateRolePingText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ulong RoleId in RolesToPing)
            {
                sb.Append($"<@&{RoleId}> ");
            }

            return sb.ToString();
        }
    }
}