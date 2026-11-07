using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrownSiegeToolkit.Core;

namespace CrownSiegeToolkit.Menu
{
    /// <summary>
    /// 菜单探针。目的很实在：**把定位工作变成一次运行的结果，而不是每人都去 dnSpy 里翻一遍**。
    ///
    /// 已知线索（来自 Assembly-CSharp 的元数据字符串堆，2026-11-07）：
    ///   ConfirmMainMenu / ReturnToMainMenu / returnToMainMenu / ShowMenu
    ///   currentMenu / subscribedMenu / BindToGameMenu / SettingsPopupMenu
    /// 这些名字说明存在一个「当前菜单」状态机，ShowMenu 是最可能的注入点，
    /// 但**签名未验证**——本探针就是用来把签名打出来的。
    /// </summary>
    internal static class MenuProbe
    {
        private static readonly string[] Keywords =
            { "menu", "panel", "screen", "popup", "hud", "lobby" };

        public static void Dump(int maxTypes = 40, int maxMembers = 12)
        {
            Assembly asm = null;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name == "Assembly-CSharp") { asm = a; break; }
            }

            if (asm == null)
            {
                ModLog.Warn("[菜单探针] 找不到 Assembly-CSharp，跳过");
                return;
            }

            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
                ModLog.Warn($"[菜单探针] 部分类型加载失败，已跳过 {e.LoaderExceptions?.Length ?? 0} 个");
            }
            catch (Exception e)
            {
                ModLog.Warn($"[菜单探针] GetTypes 失败: {e.Message}");
                return;
            }

            var hits = types
                .Where(t => t != null && Keywords.Any(k =>
                    t.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(t => t.FullName)
                .ToArray();

            ModLog.Info($"[菜单探针] Assembly-CSharp 共 {types.Length} 个类型，命中 {hits.Length} 个候选：");

            foreach (var t in hits.Take(maxTypes))
            {
                ModLog.Info($"[菜单探针]   {t.FullName}  (base={t.BaseType?.Name})");

                MethodInfo[] methods;
                try
                {
                    methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                           BindingFlags.Instance | BindingFlags.Static |
                                           BindingFlags.DeclaredOnly);
                }
                catch { continue; }

                foreach (var m in methods
                             .Where(m => !m.IsSpecialName)
                             .OrderBy(m => m.Name)
                             .Take(maxMembers))
                {
                    var ps = string.Join(", ", m.GetParameters()
                        .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    ModLog.Info($"[菜单探针]       {(m.IsStatic ? "static " : "")}{m.ReturnType.Name} {m.Name}({ps})");
                }
            }

            if (hits.Length > maxTypes)
                ModLog.Info($"[菜单探针] 还有 {hits.Length - maxTypes} 个候选未打印，需要时调大 maxTypes");
        }
    }
}
