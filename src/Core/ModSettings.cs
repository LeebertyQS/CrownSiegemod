using BepInEx.Configuration;

namespace CrownSiegeToolkit.Core
{
    /// <summary>
    /// 所有配置集中在这里，落到 BepInEx/config/crownsiege.toolkit.cfg。
    /// 原则：任何会改变游戏行为的模块都必须能一句话关掉，方便二分定位是谁弄崩的。
    /// </summary>
    internal sealed class ModSettings
    {
        private readonly ConfigFile _cfg;

        public ConfigEntry<bool> VerboseLogging { get; }
        public ConfigEntry<bool> EnforceVersion { get; }

        public ModSettings(ConfigFile cfg)
        {
            _cfg = cfg;
            VerboseLogging = cfg.Bind("General", "VerboseLogging", false,
                "输出调试级日志。默认关，只在排查时打开。");

            EnforceVersion = cfg.Bind("General", "EnforceVersion", true,
                "启动时校验游戏 build-guid，不匹配就拒绝加载。关掉只警告不拦截，用于临时排查。");
        }

        public bool IsFeatureEnabled(string featureId, string displayName)
        {
            return _cfg.Bind("Features", featureId, true, $"{displayName}：是否启用").Value;
        }

        /// <summary>模块自己需要的配置项走这里，别各自 new ConfigFile。</summary>
        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            return _cfg.Bind(section, key, defaultValue, description);
        }
    }
}
