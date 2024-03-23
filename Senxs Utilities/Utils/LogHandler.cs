using System.Collections.Generic;
using System.Text;

namespace S_Utilities.Utils
{
    public class LogHandler
    {
        public int MinLogLevel { get; set; } = 0;
        public int MaxLogLevel { get; set; } = 5;
        public string DiscordWebHook { get; set; } = "";
        public List<string> LogNames { get; set; } = new ();
        public List<string> LogNames_Ignore { get; set; } = new ();
        public string Name { get; set; } = "";
        public List<ulong> RolesToPing { get; set; } = new ();
        public List<ulong> MembersToPing { get; set; } = new ();
        public bool OnlyWhenServerOnline { get; set; } = false;

        public string CreateRolePingText()
        {
            StringBuilder sb = new ();
            foreach (ulong RoleId in RolesToPing)
            {
                sb.Append($"<@&{RoleId}> ");
            }

            return sb.ToString();
        }
        
        public string CreateMemberPingText()
        {
            StringBuilder sb = new ();
            foreach (ulong MemberId in MembersToPing)
            {
                sb.Append($"<@{MemberId}> ");
            }

            return sb.ToString();
        }
    }
}