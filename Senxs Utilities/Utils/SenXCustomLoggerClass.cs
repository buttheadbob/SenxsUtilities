using System;
using NLog;
using NLog.Targets;

namespace S_Utilities.Utils
{
    [Target("SenXCustomTarget")]
    public class SenXCustomTarget : TargetWithLayout
    {
        public static event Action<LogEventInfo>? CustomLogEventReceived;
        protected override void Write(LogEventInfo logEvent)
        {
            CustomLogEventReceived?.Invoke(logEvent);
        }
    }
}