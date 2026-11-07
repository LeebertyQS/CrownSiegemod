# Crown Siege Toolkit

给 [Crown Siege](https://store.steampowered.com/app/4310440/)（JOrbits Games，Unity 6000.6.7f1 / Mono / DOTS）做的社区 mod。

**当前状态：框架完成，探针未跑。没有任何玩法改动。**

---

## 先读哪个

| 你是谁 | 读这个 |
|---|---|
| 刚加入的开发 | [`docs/协作者安装指南.md`](docs/协作者安装指南.md) —— 从零到「日志里出现 P0 通过」 |
| 想改代码 | [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) —— 模块边界、哪些能改哪些改不了 |
| 想知道为什么这么定 | [`docs/DECISIONS.md`](docs/DECISIONS.md) —— 决策记录 |
| 要提交东西 | [`docs/GIT-WORKFLOW.md`](docs/GIT-WORKFLOW.md) —— 分支、提交、回滚、发布 |
| 想了解游戏内部结构 | [`docs/RESEARCH.md`](docs/RESEARCH.md) —— 引擎分析、类名、Job 清单 |

发给人看的版本：`docs/协作者安装指南.html`（单文件，手机可读）。

---

## 快速开始

```powershell
cd D:\GameStudio\06_CsMod
powershell -ExecutionPolicy Bypass -File tools\detect-game.ps1
dotnet build src\Mod.csproj -c Debug
```

从 Steam 启动游戏，`BepInEx\LogOutput.log` 里应该出现：

```
[Info  :Crown Siege Toolkit] [P0] Harmony injected
[Info  :Crown Siege Toolkit] Crown Siege Toolkit ready. features=4/4
```

---

## 模块

| 模块 | 状态 | 说明 |
|---|---|---|
| `diagnostics` | ✅ | 环境与输入通道诊断 |
| `menu` | 🔍 定位中 | 主菜单注入「联机」入口，探针已就位 |
| `manualcontrol` | 🦴 骨架 | 输入通道通，未绑单位 |
| `net` | ⛔ 接口 | **故意不实现**，见 D-007 |

每个模块都能在 `BepInEx/config/crownsiege.toolkit.cfg` 里单独关掉。

---

## 硬规矩

1. 不提交游戏 dll、游戏原始资源、`local.props`
2. 不在 `main` 直推（`probe/*` 除外）
3. 不改别人 `assets/`，改之前 `git lfs lock`
4. 不写假联机（D-007）

---

## 目录

```
docs/       安装、架构、决策、协作、研究
src/        Core / Diagnostics / Menu / Control / Net
assets/     我们自己的美术与音频（LFS）
data/       数据补丁
tools/      detect-game.ps1、build-doc.py
```
