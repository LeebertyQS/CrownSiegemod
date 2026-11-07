using CrownSiegeToolkit.Core;

namespace CrownSiegeToolkit.Net
{
    /// <summary>
    /// 联机（占位，**故意不实现**）。
    ///
    /// 这一层现在能提供的价值只有一个：把「联机」从一句口号，变成一个必须填的接口。
    /// 任何想在这里写「先做个假联机看看效果」的冲动都要按住——
    /// 假联机会让所有人误判进度，真联机要解决的三个问题一个都没少。
    ///
    /// 真做之前必须先答的（见 docs/ARCHITECTURE.md 第 4 节）：
    ///   1. 同步粒度：整局状态 / 只同步指令 / 只同步种子+输入
    ///   2. 谁是权威：主机算所有人，还是各自模拟+校正
    ///   3. 状态怎么读出 ECS：没有快照接口，要自己反射遍历 chunk，性能代价未知
    /// 这三个答不上来，写任何代码都是浪费。
    /// </summary>
    internal sealed class NetFeature : IModFeature
    {
        public string Id => "net";
        public string DisplayName => "联机（占位）";

        internal static NetFeature Instance { get; private set; }

        /// 当前后端。永远是 null 直到有人真正实现 INetCode。
        internal INetCode Backend { get; private set; }

        public void OnEnable()
        {
            Instance = this;
            ModLog.Info("[联机] 占位模块加载。当前无后端实现，菜单里的联机入口点了会明确报「未实现」。");
            ModLog.Info("[联机] 这是预期状态。别把它当成 bug 修，要修的是「先定同步方案」。");
        }

        public void OnDisable()
        {
            Backend?.Shutdown();
            Backend = null;
            Instance = null;
        }

        public void Tick() { }

        /// <summary>
        /// 菜单按钮的统一入口。MenuFeature 只调这个，不直接碰网络代码。
        /// 返回 false = 当前不可用（现在恒为 false）。
        /// </summary>
        internal bool RequestOpenLobby()
        {
            if (Backend == null || !Backend.IsRunning)
            {
                ModLog.Warn("[联机] 请求打开联机界面，但没有后端实现 —— 未实现（预期）");
                return false;
            }

            ModLog.Info("[联机] 打开联机界面");
            return true;
        }

        /// <summary>给未来的实现用。挂上后立刻广播角色状态。</summary>
        internal void Install(INetCode backend)
        {
            Backend = backend;
            ModLog.Info($"[联机] 已安装后端 {backend.GetType().Name}");
        }
    }
}
