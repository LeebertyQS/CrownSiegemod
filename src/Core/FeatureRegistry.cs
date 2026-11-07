using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace CrownSiegeToolkit.Core
{
    /// <summary>
    /// 模块注册表。核心职责只有一个：**一个模块崩了，不能把游戏和别的模块一起带走**。
    /// 所以每个入口都包了 try/catch，异常只记一次，不每帧刷屏。
    /// </summary>
    internal sealed class FeatureRegistry
    {
        private readonly ManualLogSource _log;
        private readonly List<IModFeature> _all = new List<IModFeature>();
        private readonly HashSet<string> _enabled = new HashSet<string>();
        private readonly HashSet<string> _faulted = new HashSet<string>();

        public FeatureRegistry(ManualLogSource log) { _log = log; }

        public int TotalCount => _all.Count;
        public int EnabledCount => _enabled.Count;

        public void Add(IModFeature feature)
        {
            if (feature == null) return;
            foreach (var f in _all)
                if (f.Id == feature.Id)
                {
                    _log.LogWarning($"重复注册 feature: {feature.Id}，已忽略");
                    return;
                }
            _all.Add(feature);
        }

        public void EnableAccordingTo(ModSettings settings)
        {
            foreach (var f in _all)
            {
                bool want;
                try { want = settings.IsFeatureEnabled(f.Id, f.DisplayName); }
                catch (Exception e) { _log.LogError($"读取 {f.Id} 配置失败: {e.Message}"); continue; }

                if (!want)
                {
                    _log.LogInfo($"[skip] {f.Id} —— 配置里关了");
                    continue;
                }

                try
                {
                    f.OnEnable();
                    _enabled.Add(f.Id);
                    ModLog.Info($"[on]   {f.Id} —— {f.DisplayName}");
                }
                catch (Exception e)
                {
                    _log.LogError($"[fail] {f.Id} OnEnable: {e}");
                }
            }
        }

        public void Tick()
        {
            if (_enabled.Count == 0) return;

            for (int i = 0; i < _all.Count; i++)
            {
                var f = _all[i];
                if (!_enabled.Contains(f.Id)) continue;
                try { f.Tick(); }
                catch (Exception e)
                {
                    if (_faulted.Add(f.Id))
                        _log.LogError($"[fault] {f.Id} Tick 抛异常，该模块本次会话停用: {e}");
                    _enabled.Remove(f.Id);
                }
            }
        }

        public void DisableAll()
        {
            foreach (var f in _all)
            {
                if (!_enabled.Contains(f.Id)) continue;
                try { f.OnDisable(); }
                catch (Exception e) { _log.LogError($"[fail] {f.Id} OnDisable: {e.Message}"); }
            }
            _enabled.Clear();
        }
    }
}
