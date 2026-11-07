// BepInEx 5 与 6 的插件基类不在同一个命名空间。
// 编译报「找不到 BaseUnityPlugin」时，把下面两行互换即可，别改别的。
// 两种基类都是 MonoBehaviour，所以生命周期统一用 Awake / Update / OnDestroy，
// 不用 Load / Unload（那是 IL2CPP 分支的写法，跟这个游戏无关）。
using BasePlugin = BepInEx.Unity.Mono.BaseUnityPlugin;
// using BasePlugin = BepInEx.BaseUnityPlugin;

using BepInEx;                 // [BepInPlugin] 特性在这里
using BepInEx.Logging;
using CrownSiegeToolkit.Core;
using CrownSiegeToolkit.Diagnostics;
using CrownSiegeToolkit.Menu;
using CrownSiegeToolkit.Control;
using CrownSiegeToolkit.Net;

namespace CrownSiegeToolkit
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BasePlugin
    {
        public const string Guid    = "community.crownsiege.toolkit";
        public const string Name    = "Crown Siege Toolkit";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;
        internal static FeatureRegistry Features;
        internal static ModSettings Settings;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{Name} v{Version} loading ...");

            Settings = new ModSettings(Config);
            ModLog.Verbose = Settings.VerboseLogging.Value;

            // 版本哨兵：游戏一更新先在这里拦下来，而不是让补丁乱打
            if (!VersionGuard.CheckAndLog(Log))
            {
                Log.LogError("目标版本不匹配，工具包不加载。更新 game-version.txt 并重跑探针后再启。");
                return;
            }

            Features = new FeatureRegistry(Log);
            Features.Add(new DiagnosticsFeature());
            Features.Add(new MenuFeature());
            Features.Add(new ManualControlFeature());
            Features.Add(new NetFeature());

            // 打完补丁再开模块：模块自己的补丁依赖这一步先成功
            try
            {
                new HarmonyLib.Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
                ModLog.Info("Harmony PatchAll ok");
            }
            catch (System.Exception e)
            {
                Log.LogError($"Harmony PatchAll 失败，后续补丁都不会生效: {e}");
            }

            Features.EnableAccordingTo(Settings);
            Log.LogInfo($"{Name} ready. features={Features.EnabledCount}/{Features.TotalCount}");
        }

        private void Update()
        {
            Features?.Tick();
        }

        private void OnDestroy()
        {
            Features?.DisableAll();
            Log?.LogInfo($"{Name} unloaded.");
        }
    }
}
