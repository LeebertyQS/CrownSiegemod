using System.IO;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using UnityEngine;

namespace CrownSiegeToolkit.Core
{
    /// <summary>
    /// 版本哨兵。游戏一更新，所有依赖内部结构的补丁都可能静默失效或乱打，
    /// 这种「看起来在跑其实全错」的状态比直接崩更害人。所以在加载阶段就拦。
    /// </summary>
    internal static class VersionGuard
    {
        // 必须与仓库根 game-version.txt 的 build-guid 一致。
        // 游戏更新后：跑 detect-game.ps1 拿到新值 → 同步改这里和 game-version.txt → 重跑 P0/P1/P2。
        public const string ExpectedBuildGuid = "d07d7b203ec34f6b9a35c7796923a472";

        public static bool CheckAndLog(ManualLogSource log)
        {
            string actual;
            try
            {
                // Application.dataPath 指向 <游戏根>/CrownSiege_Data，boot.config 就在里面
                var path = Path.Combine(Application.dataPath, "boot.config");
                if (!File.Exists(path))
                {
                    log.LogWarning($"读不到 {path}，跳过版本校验（放行）");
                    return true;
                }

                var m = Regex.Match(File.ReadAllText(path), @"build-guid=([0-9a-fA-F]{32})");
                if (!m.Success)
                {
                    log.LogWarning("boot.config 里没有 build-guid，跳过版本校验（放行）");
                    return true;
                }
                actual = m.Groups[1].Value.ToLowerInvariant();
            }
            catch (System.Exception e)
            {
                log.LogWarning($"版本校验异常，放行: {e.Message}");
                return true;
            }

            if (actual == ExpectedBuildGuid)
            {
                ModLog.Info($"[版本] build-guid 匹配 ({actual})");
                return true;
            }

            log.LogError($"[版本] 不匹配！期望 {ExpectedBuildGuid}，实际 {actual}");
            log.LogError("[版本] 游戏更新过。先跑 P0/P1/P2 确认补丁还命中，再改 ExpectedBuildGuid。");

            bool enforce = Plugin.Settings == null || Plugin.Settings.EnforceVersion.Value;
            if (!enforce)
            {
                log.LogWarning("[版本] EnforceVersion=false，仅警告，继续加载。风险自负。");
                return true;
            }
            return false;
        }
    }
}
