using BepInEx.Logging;

namespace CrownSiegeToolkit.Core
{
    /// <summary>带级别开关的日志门面。Verbose 默认关，避免刷屏把关键行埋掉。</summary>
    internal static class ModLog
    {
        public static bool Verbose;

        public static void Info(string msg)  => Plugin.Log?.LogInfo(msg);
        public static void Warn(string msg)  => Plugin.Log?.LogWarning(msg);
        public static void Error(string msg) => Plugin.Log?.LogError(msg);

        public static void Debug(string msg)
        {
            if (Verbose) Plugin.Log?.LogDebug(msg);
        }

        /// <summary>只在第一次命中时打一条，用于「证明补丁进了运行时」这类断言。</summary>
        public static void Once(string key, string msg)
        {
            if (_seen.Add(key)) Info(msg);
        }

        private static readonly System.Collections.Generic.HashSet<string> _seen =
            new System.Collections.Generic.HashSet<string>();
    }
}
