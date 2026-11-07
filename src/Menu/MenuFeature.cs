using BepInEx.Configuration;
using CrownSiegeToolkit.Core;

namespace CrownSiegeToolkit.Menu
{
    /// <summary>
    /// 主菜单注入（占位）。
    ///
    /// 目标形态：主菜单多一个「联机」入口。
    /// 当前状态：**只做到「摸清结构」，还没插按钮**。原因不是不想写，是缺两样事实：
    ///   1. 主菜单由哪个类型、哪个方法展示（探针能给出）
    ///   2. 按钮是 prefab 克隆还是代码构造（决定是走 Instantiate 还是 new GameObject+Button）
    /// 这两条一确认，本文件补两个方法就完事，改动量大约 60 行。
    ///
    /// 提醒：按钮本身是 5 分钟的事。它后面的联机是另一回事，见 src/Net/NetFeature.cs。
    /// </summary>
    internal sealed class MenuFeature : IModFeature
    {
        public string Id => "menu";
        public string DisplayName => "主菜单注入";

        private ConfigEntry<bool> _dumpTypes;

        public void OnEnable()
        {
            _dumpTypes = Plugin.Settings.Bind("Menu", "DumpMenuTypes", true,
                "启动时把可能相关的菜单/面板类型和签名打进日志。定位完成设成 false。");

            ModLog.Info("[菜单] 模块已加载，注入点待定位（这是预期状态，不是缺陷）");

            if (_dumpTypes.Value)
                MenuProbe.Dump();
        }

        public void OnDisable() { }
        public void Tick() { }

        // ---- 下一步要填的坑（按顺序）----
        // TODO-1  根据探针输出确认主菜单的展示方法，在这里加 Harmony postfix
        // TODO-2  在 postfix 里把「联机」按钮挂到主菜单的按钮容器上
        // TODO-3  点击 → 回调 NetFeature.RequestOpenLobby()，不要在这里写联机逻辑
    }
}
