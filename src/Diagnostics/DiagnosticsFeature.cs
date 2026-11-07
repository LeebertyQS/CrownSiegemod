using CrownSiegeToolkit.Core;
using UnityEngine;

namespace CrownSiegeToolkit.Diagnostics
{
    /// <summary>
    /// 把「当前跑在什么环境里」一次性写进日志。
    /// 协作者报问题的时候，这几行能省掉三轮来回问。
    /// </summary>
    internal sealed class DiagnosticsFeature : IModFeature
    {
        public string Id => "diagnostics";
        public string DisplayName => "环境诊断";

        public void OnEnable()
        {
            ModLog.Info("---- 环境诊断 ----");
            ModLog.Info($"unity={Application.unityVersion}  platform={Application.platform}");
            ModLog.Info($"dataPath={Application.dataPath}");
            ModLog.Info($"persistentDataPath={Application.persistentDataPath}");
            ModLog.Info($"screen={Screen.width}x{Screen.height}  fullscreen={Screen.fullScreen}");
            ModLog.Info($"modDll={typeof(Plugin).Assembly.Location}");

            // 旧版 Input 是否可用：游戏若禁用了 Input Manager，这里会抛异常。
            // 手动操控模块依赖它，所以先探明，别等到写操作时才发现。
            try
            {
                var k = Input.GetKey(KeyCode.None);
                ModLog.Info($"[输入] 旧版 Input 可用（探测返回值={k}）");
            }
            catch (System.Exception e)
            {
                ModLog.Warn($"[输入] 旧版 Input 不可用：{e.GetType().Name}: {e.Message}");
                ModLog.Warn("[输入] 手动操控模块需要改用新 Input System，或直接读 InputSystem 类");
            }

            ModLog.Info("------------------");
        }

        public void OnDisable() { }
        public void Tick() { }
    }
}
