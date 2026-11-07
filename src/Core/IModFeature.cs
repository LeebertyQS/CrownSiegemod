namespace CrownSiegeToolkit.Core
{
    /// <summary>
    /// 一个可独立开关的 mod 功能模块。
    /// 加新模块的做法：新建文件夹 → 实现这个接口 → 在 Plugin.Awake 里 Features.Add 一行。
    /// 注册表按 Id 生成配置项，所以模块天然可以单独关掉，坏了不会连坐。
    /// </summary>
    public interface IModFeature
    {
        /// 稳定不变的小写标识，同时是配置文件里的键名。改名等于让别人的配置失效，别改。
        string Id { get; }

        /// 日志和配置里显示的名字。
        string DisplayName { get; }

        void OnEnable();
        void OnDisable();

        /// 每帧调用。不需要每帧的模块留空实现即可。
        void Tick();
    }
}
