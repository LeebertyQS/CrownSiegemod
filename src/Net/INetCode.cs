using System;

namespace CrownSiegeToolkit.Net
{
    /// <summary>联机角色。Offline 表示没开任何会话。</summary>
    public enum NetRole
    {
        Offline,
        Host,
        Client
    }

    /// <summary>
    /// 联机后端的契约。**先定接口，再选实现**——因为这个游戏没有任何联机底座，
    /// 实现方式完全没定，现在写实现只会在错误的方向上堆代码。
    ///
    /// 已知的硬约束（决定了实现难度，见 docs/ARCHITECTURE.md 第 4 节）：
    ///   - 战场状态活在 ECS 里，模拟在 Burst Job 里跑，没有可序列化的快照接口
    ///   - 单位数量级是「几百」，全量状态同步的带宽和频率都不现实
    ///   - 没有任何确定性保证（浮点 + Job 调度顺序），回滚式同步也做不了
    /// </summary>
    public interface INetCode
    {
        NetRole Role { get; }
        bool IsRunning { get; }

        /// 开房。返回 false 表示开房失败，调用方负责提示玩家。
        bool StartHost(int port);

        /// 加入房间。
        bool StartClient(string address, int port);

        /// 关会话。必须幂等，可重复调用。
        void Shutdown();

        event Action<NetRole> RoleChanged;
    }
}
