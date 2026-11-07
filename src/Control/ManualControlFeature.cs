using System;
using BepInEx.Configuration;
using CrownSiegeToolkit.Core;
using UnityEngine;

namespace CrownSiegeToolkit.Control
{
    /// <summary>
    /// 手动操控（骨架）。
    ///
    /// 目标形态：玩家能直接操纵场上单位，而不是只看自动战斗。
    /// 当前状态：**输入通道打通了，但还没有接任何单位**。
    ///
    /// 已确认的技术前提：游戏带 UnityEngine.InputLegacyModule.dll，
    /// 旧版 Input 大概率可用（DiagnosticsFeature 启动时会实测并写日志）。
    ///
    /// 真正的难点不在读键，在于**找到「玩家该控制哪个实体」**：
    /// 这个游戏是自动战斗，逻辑跑在 Burst Job 里，没有既有的 player-controlled 实体。
    /// 可行路径是把某个单位的 AI 输入（target 选择 / 移动意图）在托管层替换掉，
    /// 而不是去改 Job —— Job 内部改不了。这条路径没验证过，属于待验证假设。
    /// </summary>
    internal sealed class ManualControlFeature : IModFeature
    {
        public string Id => "manualcontrol";
        public string DisplayName => "手动操控";

        private ConfigEntry<string> _toggleKey;
        private KeyCode _key = KeyCode.F8;
        private bool _inputOk;
        private bool _armed;

        public void OnEnable()
        {
            _toggleKey = Plugin.Settings.Bind("Control", "ToggleKey", "F8",
                "手动操控开关热键（KeyCode 名称，如 F8 / LeftControl）");

            _inputOk = ProbeInput();

            if (Enum.TryParse(_toggleKey.Value, true, out KeyCode parsed))
                _key = parsed;
            else
                ModLog.Warn($"[操控] 无法解析热键 \"{_toggleKey.Value}\"，沿用 F8");

            ModLog.Info($"[操控] 骨架已加载。热键 {_key} 切换「预演模式」（只打日志，不接管单位）");
        }

        public void OnDisable()
        {
            _armed = false;
        }

        public void Tick()
        {
            if (!_inputOk) return;

            try
            {
                if (Input.GetKeyDown(_key))
                {
                    _armed = !_armed;
                    ModLog.Info(_armed
                        ? "[操控] 预演模式 ON —— 输入通道可用，但未绑定单位"
                        : "[操控] 预演模式 OFF");
                }

                if (!_armed) return;

                // 稀疏日志：只在按下瞬间记，避免每帧刷屏
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
                    Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
                {
                    ModLog.Debug($"[操控] 方向键命中 W={Input.GetKey(KeyCode.W)} " +
                                 $"A={Input.GetKey(KeyCode.A)} S={Input.GetKey(KeyCode.S)} D={Input.GetKey(KeyCode.D)}");
                }
            }
            catch (InvalidOperationException)
            {
                // 旧 Input 在运行中被判定不可用（新 Input System 独占），停掉本模块
                _inputOk = false;
                ModLog.Warn("[操控] 旧版 Input 抛异常，模块停用。需要改走新 Input System。");
            }
        }

        private static bool ProbeInput()
        {
            try
            {
                Input.GetKey(KeyCode.None);
                return true;
            }
            catch (Exception e)
            {
                ModLog.Warn($"[操控] 旧版 Input 不可用：{e.GetType().Name}");
                return false;
            }
        }

        // ---- 下一步（这两条是分水岭，先做 1）----
        // TODO-1  用 ECS 查询拿到场上单位列表（EntityManager / EntityQuery），确认能读到位置与阵营
        // TODO-2  选一个单位，在托管层覆盖它的移动意图，验证它真的跟着按键走
        // 如果 TODO-2 做不成，说明「手动操控」这条路要整体重估，不要硬耗。
    }
}
