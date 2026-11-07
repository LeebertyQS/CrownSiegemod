using HarmonyLib;
using UnityEngine;

namespace CrownSiegeToolkit.Core
{
    /// <summary>
    /// P0 探针：证明「Harmony 补丁真的进了运行时」。
    ///
    /// 为什么挑 persistentDataPath：游戏启动阶段必然读它（存档路径），命中率高、副作用为零。
    /// 为什么必须有这一行：BepInEx 日志里出现 "plugin loaded" 只说明**程序集被加载了**，
    /// 不说明补丁生效。这两者之间的差距，是这个项目最容易误判的地方。
    /// </summary>
    [HarmonyPatch(typeof(Application), nameof(Application.persistentDataPath), MethodType.Getter)]
    internal static class BootProbe
    {
        private static bool _logged;

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (_logged) return;
            _logged = true;
            ModLog.Info("[P0] Harmony injected");
        }
    }
}
